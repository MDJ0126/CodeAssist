using CodeAssist.Core;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace CodeAssist.Extension
{
    internal static class PluginCommands
    {
        internal static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var commands=await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            commands?.AddCommand(new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(async()=>{try{await ExecuteAsync(package);}catch(Exception error){await package.JoinableTaskFactory.SwitchToMainThreadAsync();MessageBox.Show(error.Message,"CodeAssist Plugins");}}).FileAndForget("CodeAssist/Plugins"),new CommandID(GoToSymbolCommand.CommandSet,0x1b0)));
        }
        private static async System.Threading.Tasks.Task ExecuteAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var model=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if(model==null||manager==null||dte==null||manager.GetActiveView(1,null,out var adapter)<0)return;var view=model.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(adapter);if(view==null||view.IsClosed)return;
            var snapshot=view.TextSnapshot;var selection=view.Selection.StreamSelectionSpan.SnapshotSpan;var caret=view.Caret.Position.BufferPosition.Position;
            var context=new PluginDocument(dte.ActiveDocument?.FullName,snapshot.GetText(),caret,selection.Start,selection.Length);var providers=model.GetExtensions<ICodeAssistPlugin>().ToArray();
            var actions=await System.Threading.Tasks.Task.Run(()=>providers.SelectMany(provider=>provider.GetActions(context)??Enumerable.Empty<PluginAction>()).Take(1000).ToArray());
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();if(actions.Length==0){MessageBox.Show("No installed VSIX exports CodeAssist.Core.ICodeAssistPlugin actions for this document.","CodeAssist Plugins");return;}
            var list=new ListBox {ItemsSource=actions,SelectedIndex=0};var dialog=new Window {Title="CodeAssist Plugins",Content=list,Width=650,Height=400,WindowStartupLocation=WindowStartupLocation.CenterOwner};new System.Windows.Interop.WindowInteropHelper(dialog).Owner=(IntPtr)dte.MainWindow.HWnd;
            list.MouseDoubleClick+=(s,e)=>dialog.DialogResult=true;dialog.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter)dialog.DialogResult=true;if(e.Key==System.Windows.Input.Key.Escape)dialog.DialogResult=false;};if(dialog.ShowDialog()!=true||!(list.SelectedItem is PluginAction action))return;
            var after=await System.Threading.Tasks.Task.Run(()=>PluginEdits.Apply(context.Text,action.Edits()));await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var tabs=new TabControl();tabs.Items.Add(new TabItem {Header="Before",Content=new TextBox {Text=context.Text,IsReadOnly=true,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});tabs.Items.Add(new TabItem {Header="After",Content=new TextBox {Text=after,IsReadOnly=true,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});tabs.SelectedIndex=1;
            var panel=new DockPanel();ProductTheme.Apply(panel);var apply=ProductTheme.Button("Apply changes",()=>{},true);DockPanel.SetDock(apply,Dock.Bottom);panel.Children.Add(apply);panel.Children.Add(tabs);var preview=new Window {Title=action.Title,Content=panel,Width=950,Height=650,WindowStartupLocation=WindowStartupLocation.CenterOwner};new System.Windows.Interop.WindowInteropHelper(preview).Owner=(IntPtr)dte.MainWindow.HWnd;apply.Click+=(s,e)=>preview.DialogResult=true;
            if(preview.ShowDialog()==true)EditorFeatureCommands.Apply(view,snapshot,new EditorTextChange(0,snapshot.Length,after,Math.Min(caret,after.Length),0),model,action.Title);
        }
    }
}
