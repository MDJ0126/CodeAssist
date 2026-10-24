using CodeAssist.Core;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace CodeAssist.Extension
{
    internal static class CompilerActions
    {
        private sealed class ActionItem {internal JObject Action;public override string ToString()=>(string)Action["title"];}
        internal static async Task<bool> TryAsync(AsyncPackage package,EnvDTE80.DTE2 dte,bool quickFix=false,bool toolWindow=false)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var file=dte.ActiveDocument?.FullName;if(string.IsNullOrEmpty(file))return false;
            var root=Path.GetDirectoryName(file);
            while(root!=null&&!File.Exists(Path.Combine(root,"compile_commands.json"))&&!File.Exists(Path.Combine(root,"compile_flags.txt")))root=Path.GetDirectoryName(root);
            if(root==null&&!string.IsNullOrEmpty(StartupUi.Preferences.CompilationDatabaseDirectory))root=Path.GetDirectoryName(file);
            if(root==null)return false;
            var components=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if(manager==null||components==null||manager.GetActiveView(1,null,out var adapter)<0)return false;
            var view=components.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(adapter);
            if(view==null||view.IsClosed||!view.TextBuffer.ContentType.IsOfType("C/C++"))return false;
            var snapshot=view.TextSnapshot;var span=view.Selection.StreamSelectionSpan.SnapshotSpan;
            var first=snapshot.GetLineFromPosition(span.Start);var last=snapshot.GetLineFromPosition(span.End);
            var range=new JObject {["start"]=new JObject {["line"]=first.LineNumber,["character"]=span.Start.Position-first.Start.Position},["end"]=new JObject {["line"]=last.LineNumber,["character"]=span.End.Position-last.Start.Position}};
            var open=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(EnvDTE.Document document in dte.Documents)
            {try{if(new[]{".c",".cc",".cpp",".cxx",".h",".hh",".hpp",".hxx",".inl",".ipp"}.Contains(Path.GetExtension(document.FullName).ToLowerInvariant())&&document.Object("TextDocument") is EnvDTE.TextDocument text)open[document.FullName]=text.StartPoint.CreateEditPoint().GetText(text.EndPoint);}catch(COMException){}}
            var result=await CompilerSemanticService.Instance.QueryAsync("textDocument/codeAction",root,dte.FullName,file,snapshot.GetText(),first.LineNumber+1,span.Start.Position-first.Start.Position+1,package.DisposalToken,new JObject {["range"]=range,["context"]=new JObject {["diagnostics"]=new JArray(),["only"]=new JArray(quickFix?"quickfix":"refactor")}},open);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if(result==null)return false;
            var actions=(result as JArray)?.OfType<JObject>().Where(a=>a["disabled"]==null&&(a["edit"]!=null||a["data"]!=null||CodeAssist.Semantics.ClangdClient.SupportsPreviewCommand(a["command"]))).Select(a=>new ActionItem {Action=a}).ToArray();
            if(actions==null||actions.Length==0)return false;
            var list=new ListBox {ItemsSource=actions,SelectedIndex=0,Margin=new Thickness(14)};
            var rootPanel=new DockPanel();ProductTheme.Apply(rootPanel);rootPanel.Children.Add(list);
            var dialog=new Window {Title="CodeAssist: Compiler Refactorings",Content=rootPanel,Width=650,Height=400,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            new System.Windows.Interop.WindowInteropHelper(dialog).Owner=(IntPtr)dte.MainWindow.HWnd;
            dialog.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape)dialog.DialogResult=false;if(e.Key==System.Windows.Input.Key.Enter&&list.SelectedItem!=null)dialog.DialogResult=true;};
            list.MouseDoubleClick+=(s,e)=>{if(!toolWindow&&list.SelectedItem!=null)dialog.DialogResult=true;};
            async System.Threading.Tasks.Task ApplySelectedAsync()
            {
            if(view.IsClosed||!ReferenceEquals(snapshot,view.TextSnapshot))throw new InvalidOperationException("Document changed while selecting a refactoring.");
            var action=((ActionItem)list.SelectedItem).Action;
            if(action["edit"]==null && CodeAssist.Semantics.ClangdClient.SupportsPreviewCommand(action["command"]))
            {
                var preview=await CompilerSemanticService.Instance.QueryAsync("codeassist/previewCommand",root,dte.FullName,file,snapshot.GetText(),1,1,package.DisposalToken,(JObject)action["command"].DeepClone(),open);
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if(preview==null)throw new InvalidOperationException("Compiler did not provide a supported edit preview.");
                action=(JObject)action.DeepClone();action["edit"]=preview;
            }
            if(action["edit"]==null)
            {
                var resolved=await CompilerSemanticService.Instance.QueryAsync("codeAction/resolve",root,dte.FullName,file,snapshot.GetText(),1,1,package.DisposalToken,(JObject)action.DeepClone(),open);
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if(!(resolved is JObject resolvedAction)||resolvedAction["edit"]==null)throw new InvalidOperationException("Compiler did not provide a supported text edit.");
                action=resolvedAction;
            }
            await WorkspaceEditPreview.ApplyAsync(package,dte,action["edit"],open,(string)action["title"]);
            }
            if(toolWindow)
            {
                var button=ProductTheme.Button("Preview selected fix",()=>{},true);DockPanel.SetDock(button,Dock.Bottom);rootPanel.Children.Insert(0,button);
                button.Click+=(s,e)=>package.JoinableTaskFactory.RunAsync(ApplySelectedAsync).FileAndForget("CodeAssist/QuickFixPreview");
                dialog.Content=null;await UtilityCommands.ShowAsync(package,"Quick Fixes",rootPanel);return true;
            }
            if(dialog.ShowDialog()==true)await ApplySelectedAsync();return true;
        }
    }
}
