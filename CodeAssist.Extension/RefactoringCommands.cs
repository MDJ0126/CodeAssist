using CodeAssist.Core;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace CodeAssist.Extension
{
    internal static class RefactoringCommands
    {
        internal static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var service=await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            if(service==null)return;
            for(var id=0x180;id<=0x18c;id++){var command=id;service.AddCommand(new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(async()=>{try{await ExecuteAsync(package,command);}catch(Exception error){await package.JoinableTaskFactory.SwitchToMainThreadAsync();MessageBox.Show(error.Message,"CodeAssist",MessageBoxButton.OK,MessageBoxImage.Warning);}}).FileAndForget("CodeAssist/Refactoring"),new CommandID(GoToSymbolCommand.CommandSet,command)));}
        }
        private static async System.Threading.Tasks.Task ExecuteAsync(AsyncPackage package,int id)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var model=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if(dte==null||model==null||manager==null||manager.GetActiveView(1,null,out var adapter)<0)return;
            var view=model.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(adapter);if(view==null||view.IsClosed||!view.TextBuffer.ContentType.IsOfType("C/C++"))return;
            var snapshot=view.TextSnapshot;var caret=view.Caret.Position.BufferPosition;var selected=view.Selection.StreamSelectionSpan.SnapshotSpan;
            var line=caret.GetContainingLine();var source=selected.Length==0?line.GetText():selected.GetText();var nl=snapshot.GetText().Contains("\r\n")?"\r\n":"\n";
            var start=selected.Length==0?line.Start.Position:selected.Start.Position;var length=selected.Length==0?line.Length:selected.Length;
            string result=null;var title="CodeAssist Refactoring";
            string Ask(string label,string initial="")=>EditorFeatureCommands.Prompt(dte,label,initial);
            switch(id)
            {
                case 0x18b:
                    result=CppDocumentTransforms.ConvertLocalPointer(snapshot.GetText(),start,length);start=0;length=snapshot.Length;title="Convert Local Pointer / Instance";break;
                case 0x18c:
                    if(selected.Length==0)throw new ArgumentException("Select statements to extract.");var signature=Ask("New free-function signature (use reference parameters for outputs)","void Extracted()");if(signature==null)return;
                    result=CppDocumentTransforms.ExtractGlobalMethod(snapshot.GetText(),start,length,signature);start=0;length=snapshot.Length;title="Extract Method";break;
                case 0x180:result=CppRefactoring.SimplifyInstance(source);title="Simplify Instance Declaration";break;
                case 0x181:
                    var property=Ask("Accessor name suffix","Value");if(property==null)return;result=CppRefactoring.EncapsulateField(source,property,nl);title="Encapsulate Field (declaration and accessors)";break;
                case 0x182:
                    var member=Ask("Member declaration","void NewMethod();");if(member==null)return;
                    if(!member.TrimEnd().EndsWith(";",StringComparison.Ordinal))throw new ArgumentException("A member declaration must end with a semicolon.");
                    var memberEdit=CppClassTransforms.InsertMember(snapshot.GetText(),caret,member);result=memberEdit.Text;start=memberEdit.Start;length=0;title="Add Member to Class";break;
                case 0x183:
                    var name=Ask("New member name","NewMember");if(name==null)return;var similar=CppRefactoring.SimilarMember(source,name);if(similar!=null)result=source+nl+similar;title="Add Similar Member";break;
                case 0x184:
                    var ns=Ask("Namespace","std");if(ns==null)return;var directive=CppRefactoring.UsingNamespace(ns);if(directive!=null)result=directive+nl;start=line.Start;length=0;title="Add Using Directive at Current Line";break;
                case 0x185:result=CppRefactoring.ToggleBraces(source);title="Add / Remove Braces";break;
                case 0x186:
                    var declaration=CppRefactoring.CreateDeclaration(source);if(declaration!=null)result=declaration+nl;
                    var declarationOwner=CppClassTransforms.MethodOwner(source);if(declaration!=null&&declarationOwner!=null){var declarationEdit=CppClassTransforms.InsertMember(snapshot.GetText(),caret,declaration,declarationOwner);result=declarationEdit.Text;start=declarationEdit.Start;}else start=selected.Length==0?line.Start.Position:selected.Start.Position;length=0;title="Create Declaration";break;
                case 0x187:case 0x188:
                    var owner=Ask("Qualified owner type (empty for free function)",CppExpressionContext.FindEnclosingFunctionOwner(snapshot.GetText(),caret));if(owner==null)return;
                    result=id==0x187?CppRefactoring.CreateImplementation(source,owner,nl,false,StartupUi.Preferences.MethodBodyTemplate):CppRefactoring.CreateImplementations(source,owner,nl,false,StartupUi.Preferences.MethodBodyTemplate);
                    if(result!=null){if(new[]{".h",".hh",".hpp",".hxx",".inl",".ipp"}.Contains(Path.GetExtension(dte.ActiveDocument.FullName).ToLowerInvariant()))result=CppClassTransforms.HeaderSafeImplementation(result);result=nl+result;}start=snapshot.Length;length=0;title="Generate Method Implementation(s) at End of File";break;
                case 0x189:
                    var virtuals=Ask("Virtual declarations to implement in the derived class","virtual void Update() = 0;");if(virtuals==null)return;
                    result=CppRefactoring.CreateImplementations(virtuals,"",nl,true,StartupUi.Preferences.MethodBodyTemplate);if(result!=null){var virtualEdit=CppClassTransforms.InsertMember(snapshot.GetText(),caret,result);result=virtualEdit.Text;start=virtualEdit.Start;}length=0;title="Implement Virtual Methods in Class";break;
                case 0x18a:
                    var enumName=Ask("Qualified scoped enum name","State");if(enumName==null)return;
                    var definition=Ask("Enum declaration","enum class State { Ready, Running, Done };");if(definition==null)return;
                    result=CppRefactoring.MissingCases(source,definition,enumName,nl);title="Add Missing Case Statements";break;
            }
            if(result==null)throw new InvalidOperationException(UiText.Get("The selected syntax is unsupported. Select the complete declaration or expression.",StartupUi.Preferences.UiLanguage));
            var after=snapshot.GetText().Remove(start,length).Insert(start,result);
            var tabs=new TabControl();tabs.Items.Add(new TabItem {Header=UiText.Get("Before",StartupUi.Preferences.UiLanguage),Content=Text(snapshot.GetText())});tabs.Items.Add(new TabItem {Header=UiText.Get("After",StartupUi.Preferences.UiLanguage),Content=Text(after)});tabs.SelectedIndex=1;
            var panel=new DockPanel();ProductTheme.Apply(panel);var apply=ProductTheme.Button("Apply changes",()=>{},true);DockPanel.SetDock(apply,Dock.Bottom);panel.Children.Add(apply);panel.Children.Add(tabs);
            var dialog=new Window {Title=title,Content=panel,Width=950,Height=650,WindowStartupLocation=WindowStartupLocation.CenterOwner};new System.Windows.Interop.WindowInteropHelper(dialog).Owner=(IntPtr)dte.MainWindow.HWnd;
            apply.Click+=(s,e)=>dialog.DialogResult=true;dialog.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape)dialog.DialogResult=false;};
            if(dialog.ShowDialog()==true)EditorFeatureCommands.Apply(view,snapshot,new EditorTextChange(start,length,result,start,result.Length),model,title);
        }
        private static TextBox Text(string text)=>new TextBox {Text=text,IsReadOnly=true,AcceptsReturn=true,AcceptsTab=true,FontFamily=new System.Windows.Media.FontFamily("Consolas"),HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
