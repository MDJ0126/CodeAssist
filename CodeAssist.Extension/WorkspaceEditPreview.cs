using CodeAssist.Semantics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace CodeAssist.Extension
{
    internal static class WorkspaceEditPreview
    {
        private sealed class Plan {internal string File,Before,After;internal TextReplacement[] Changes;}
        internal static async System.Threading.Tasks.Task ApplyAsync(AsyncPackage package,EnvDTE80.DTE2 dte,JToken edit,IDictionary<string,string> open,string title)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var plans=new List<Plan>();
            foreach(var pair in WorkspaceEdits.Files(edit))
            {
                var before=open.TryGetValue(pair.Key,out var value)?value:File.ReadAllText(pair.Key);
                plans.Add(new Plan {File=pair.Key,Before=before,After=WorkspaceEdits.Apply(before,pair.Value),Changes=WorkspaceEdits.Plan(before,pair.Value)});
            }
            if(plans.Count==0)return;
            var tabs=new TabControl();
            foreach(var plan in plans)
            {
                var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition());
                var before=Text(plan.Before);var after=Text(plan.After);Grid.SetColumn(after,1);grid.Children.Add(before);grid.Children.Add(after);
                tabs.Items.Add(new TabItem {Header=Path.GetFileName(plan.File),ToolTip=plan.File,Content=grid});
            }
            var root=new DockPanel();ProductTheme.Apply(root);
            var button=ProductTheme.Button("Apply changes",()=>{},true);button.IsDefault=true;DockPanel.SetDock(button,Dock.Bottom);root.Children.Add(button);root.Children.Add(tabs);
            var dialog=new Window {Title="CodeAssist: "+title,Content=root,Width=1100,Height=650,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            new System.Windows.Interop.WindowInteropHelper(dialog).Owner=(IntPtr)dte.MainWindow.HWnd;
            button.Click+=(s,e)=>dialog.DialogResult=true;dialog.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape)dialog.DialogResult=false;};
            if(dialog.ShowDialog()!=true)return;
            var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            var components=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var adapters=components?.GetService<IVsEditorAdaptersFactoryService>();
            if(manager==null||adapters==null||dte.UndoContext.IsOpen)throw new InvalidOperationException("Editor is busy. Retry after the current edit finishes.");
            var edits=new List<ITextEdit>();bool undo=false;
            try
            {
                foreach(var plan in plans)
                {
                    dte.ItemOperations.OpenFile(plan.File,EnvDTE.Constants.vsViewKindTextView);
                    if(manager.GetActiveView(1,null,out var adapter)<0)throw new InvalidOperationException("Cannot open "+plan.File);
                    var view=adapters.GetWpfTextView(adapter);
                    if(view==null||view.IsClosed||view.TextSnapshot.GetText()!=plan.Before)throw new InvalidOperationException("Document changed since the preview: "+plan.File);
                    var bufferEdit=view.TextBuffer.CreateEdit();edits.Add(bufferEdit);
                    foreach(var change in plan.Changes)if(!bufferEdit.Replace(change.Start,change.Length,change.Text))throw new InvalidOperationException("Read-only range: "+plan.File);
                }
                dte.UndoContext.Open("CodeAssist "+title,false);undo=true;
                foreach(var change in edits){change.Apply();if(change.Canceled)throw new InvalidOperationException("Editor rejected a workspace change.");}
            }
            catch {if(undo)dte.UndoContext.SetAborted();throw;}
            finally {foreach(var change in edits)change.Dispose();if(undo)dte.UndoContext.Close();}
        }
        private static TextBox Text(string value)=>new TextBox {Text=value,IsReadOnly=true,AcceptsReturn=true,FontFamily=new System.Windows.Media.FontFamily("Consolas"),HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
