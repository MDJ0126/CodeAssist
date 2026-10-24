using CodeAssist.Core;
using CodeAssist.Indexing;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace CodeAssist.Extension
{
    internal sealed class EditorFeatureCommands
    {
        private readonly AsyncPackage package;private readonly ProjectIndexService indexes;
        private EditorFeatureCommands(AsyncPackage package,ProjectIndexService indexes){this.package=package;this.indexes=indexes;}
        internal static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package,ProjectIndexService indexes)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var service=await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;if(service==null)return;
            var owner=new EditorFeatureCommands(package,indexes);
            for(var id=0x171;id<=0x17b;id++)
            {
                var captured=id;service.AddCommand(new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(async()=>{
                    try{await owner.ExecuteAsync(captured);}catch(Exception error){await package.JoinableTaskFactory.SwitchToMainThreadAsync();MessageBox.Show(error.Message,"CodeAssist",MessageBoxButton.OK,MessageBoxImage.Warning);}
                }).FileAndForget("CodeAssist/EditorFeature"),new CommandID(GoToSymbolCommand.CommandSet,id)));
            }
        }
        private async System.Threading.Tasks.Task ExecuteAsync(int id)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;if(dte==null)return;
            var file=dte.ActiveDocument?.FullName;
            if(id==0x172){var path=ChooseNewFile(file);if(path==null)return;WriteNew(path,"");dte.ItemOperations.OpenFile(path,EnvDTE.Constants.vsViewKindTextView);return;}
            if(id==0x173)
            {
                var document=dte.ActiveDocument;var item=document?.ProjectItem;
                if(document==null||item==null)throw new InvalidOperationException(UiText.Get("Select a project file first.",StartupUi.Preferences.UiLanguage));
                if(!document.Saved)throw new InvalidOperationException(UiText.Get("Save the document before renaming it.",StartupUi.Preferences.UiLanguage));
                var name=Prompt(dte,"New file name",Path.GetFileName(file));if(name==null)return;
                if(string.IsNullOrWhiteSpace(name)||Path.GetFileName(name)!=name||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new ArgumentException("Invalid file name");
                item.Name=name;return;
            }
            var components=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if(components==null||manager==null||manager.GetActiveView(1,null,out var adapter)<0)return;
            var view=components.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(adapter);if(view==null||view.IsClosed)return;
            var snapshot=view.TextSnapshot;var selected=view.Selection.StreamSelectionSpan.SnapshotSpan;var caret=view.Caret.Position.BufferPosition.Position;var text=snapshot.GetText();
            var line=snapshot.GetLineFromPosition(caret);var word=IdentifierParser.At(line.GetText(),caret-line.Start.Position);
            if(id==0x17b)
            {
                var links=SourceLinks.Find(line.GetText(),StartupUi.Preferences.SourceLinkRules).Where(candidate=>caret-line.Start.Position>=candidate.Start&&caret-line.Start.Position<=candidate.Start+candidate.Length).ToArray();
                if(links.Length==0)throw new InvalidOperationException(UiText.Get("No source link matches the caret. Configure Navigation / Source Links.",StartupUi.Preferences.UiLanguage));
                var link=links[0];
                if(links.Length>1){var box=new ListBox {ItemsSource=links,SelectedIndex=0};var dialog=new Window {Title="CodeAssist: Source Links",Content=box,Width=750,Height=320,WindowStartupLocation=WindowStartupLocation.CenterOwner};new System.Windows.Interop.WindowInteropHelper(dialog).Owner=(IntPtr)dte.MainWindow.HWnd;box.MouseDoubleClick+=(s,e)=>dialog.DialogResult=true;dialog.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter)dialog.DialogResult=true;if(e.Key==System.Windows.Input.Key.Escape)dialog.DialogResult=false;};if(dialog.ShowDialog()!=true)return;link=box.SelectedItem as SourceLinkMatch;if(link==null)return;}
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link.Uri.AbsoluteUri){UseShellExecute=true});return;
            }
            if(id==0x171)
            {
                var type=word.Length==0?CppExpressionContext.FindEnclosingFunctionOwner(text,caret):new CppReferenceContext(text).ResolveVariableType(word,caret)??indexes.Index.ResolveVariableType(word)??word;
                var entries=indexes.Index.CompleteMembers(type,"",2000).Concat(PersistentSymbolIndex.ReadDocumentSymbols(file??"",text).Where(s=>s.OwnerType==type)).GroupBy(s=>s.Identity).Select(g=>g.First()).Where(s=>File.Exists(s.FilePath)).Select(s=>new NavigationEntry((s.OwnerType.Length>0?s.OwnerType+"::":"")+(s.Signature.Length>0?s.Signature:s.Name),s.FilePath,s.Line,s.Column,s.Signature,s.Kind.ToString())).ToArray();
                var picker=new NavigationPicker("CodeAssist: Go To Member",entries,package.JoinableTaskFactory);new System.Windows.Interop.WindowInteropHelper(picker).Owner=(IntPtr)dte.MainWindow.HWnd;
                if(picker.ShowDialog()==true&&picker.Selected!=null){dte.ItemOperations.OpenFile(picker.Selected.File,EnvDTE.Constants.vsViewKindTextView);(dte.ActiveDocument.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(picker.Selected.Line,picker.Selected.Column,false);}return;
            }
            if(id==0x175){if(selected.Length==0)return;SnippetInsertion.Show(view,file,indexes.ProjectRoot,(IntPtr)dte.MainWindow.HWnd,null,dte.Solution?.FullName,true);return;}
            if(id==0x176)
            {
                if(selected.Length==0)return;
                var title=Prompt(dte,"Snippet title","Selection");if(title==null)return;
                var shortcut=Prompt(dte,"Snippet shortcut","snippet");if(shortcut==null)return;
                var root=!string.IsNullOrEmpty(dte.Solution?.FullName)?Path.GetDirectoryName(dte.Solution.FullName):indexes.ProjectRoot;
                if(string.IsNullOrEmpty(root))throw new InvalidOperationException("Open a solution before saving a project snippet.");
                var path=Path.Combine(root,".codeassist","snippets.tpl");var existing=File.Exists(path)?File.ReadAllText(path):"";
                if(existing.Length>1024*1024)throw new InvalidOperationException("Snippet library exceeds 1 MiB.");
                if(SnippetTemplate.Parse(existing).Any(t=>t.Shortcut==shortcut))throw new InvalidOperationException("Snippet shortcut already exists.");
                var entry=CppGeneration.SnippetFromSelection(title,shortcut,selected.GetText());Directory.CreateDirectory(Path.GetDirectoryName(path));
                var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
                try{File.WriteAllText(temporary,existing+(existing.Length==0?"":"\f\n")+entry);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}return;
            }
            if(id==0x177)
            {
                var source=selected.Length>0?selected.GetText():line.GetText();var uri=CppGeneration.HyperlinkAt(source,selected.Length>0?0:caret-line.Start.Position);
                if(uri==null)throw new InvalidOperationException(UiText.Get("Place the caret inside an http or https link.",StartupUi.Preferences.UiLanguage));
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});return;
            }
            if(!view.TextBuffer.ContentType.IsOfType("C/C++"))return;
            EditorTextChange change=null;
            if(id==0x174)
            {
                if(selected.Length==0)return;var path=ChooseNewFile(file);if(path==null)return;
                if(!ReferenceEquals(snapshot,view.TextSnapshot))throw new InvalidOperationException("Document changed while choosing the target file.");
                var contents=selected.GetText();WriteNew(path,contents);
                try{Apply(view,snapshot,new EditorTextChange(selected.Start,selected.Length,"",selected.Start,0),components,"Move Selection to New File");}
                catch{if(File.Exists(path)&&File.ReadAllText(path)==contents)File.Delete(path);throw;}
                dte.ItemOperations.OpenFile(path,EnvDTE.Constants.vsViewKindTextView);return;
            }
            if(id==0x178)
            {
                var signatureLine=snapshot.GetLineFromPosition(selected.Length>0?selected.Start.Position:caret);
                var signature=selected.Length>0?selected.GetText():signatureLine.GetText();var indent=new string(signatureLine.GetText().TakeWhile(c=>c==' '||c=='\t').ToArray());
                var comment=CppGeneration.DocumentMethod(signature,text.Contains("\r\n")?"\r\n":"\n",indent);
                if(comment==null)throw new InvalidOperationException(UiText.Get("Select a method declaration or its signature line.",StartupUi.Preferences.UiLanguage));
                change=new EditorTextChange(signatureLine.Start,0,comment,signatureLine.Start.Position+comment.Length,0);
            }
            if(id==0x179)
            {
                var header=Prompt(dte,"Header name (use <...> for system headers)","");if(header==null)return;header=header.Trim();var system=header.StartsWith("<",StringComparison.Ordinal)&&header.EndsWith(">",StringComparison.Ordinal);
                change=CppGeneration.Include(text,system?header.Substring(1,header.Length-2):header,system);
            }
            if(id==0x17a)
            {
                var name=Prompt(dte,"Qualified type name (class or struct)",word);if(name==null)return;var structure=name.StartsWith("struct ",StringComparison.Ordinal);name=name.StartsWith("class ",StringComparison.Ordinal)?name.Substring(6):structure?name.Substring(7):name;
                var declaration=CppGeneration.ForwardDeclaration(name.Trim(),structure)+(text.Contains("\r\n")?"\r\n":"\n");change=new EditorTextChange(line.Start,0,declaration,line.Start.Position+declaration.Length,0);
            }
            if(change!=null)Apply(view,snapshot,change,components,"Code Generation");
        }
        internal static void Apply(IWpfTextView view,ITextSnapshot expected,EditorTextChange change,IComponentModel components,string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(view.IsClosed||!ReferenceEquals(expected,view.TextSnapshot))throw new InvalidOperationException("Document changed before the edit.");
            components.GetService<ITextUndoHistoryRegistry>().TryGetHistory(view.TextBuffer,out var history);var operations=components.GetService<IEditorOperationsFactoryService>().GetEditorOperations(view);
            using(var transaction=history?.CreateTransaction("CodeAssist "+name))using(var edit=view.TextBuffer.CreateEdit())
            {
                if(transaction!=null)operations.AddBeforeTextBufferChangePrimitive();
                if(!edit.Replace(change.Start,change.Length,change.Text))throw new InvalidOperationException("The selected range is read-only.");
                var updated=edit.Apply();if(edit.Canceled)throw new InvalidOperationException("Editor rejected the edit.");
                view.Selection.Select(new SnapshotSpan(updated,change.SelectionStart,change.SelectionLength),false);view.Caret.MoveTo(new SnapshotPoint(updated,change.SelectionStart+change.SelectionLength));
                if(transaction!=null){operations.AddAfterTextBufferChangePrimitive();transaction.Complete();}
            }
        }
        private static string ChooseNewFile(string source)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dialog=new Microsoft.Win32.SaveFileDialog {Title=UiText.Get("Create file",StartupUi.Preferences.UiLanguage),InitialDirectory=string.IsNullOrEmpty(source)?null:Path.GetDirectoryName(source),FileName="NewFile.cpp",Filter="C/C++ files|*.cpp;*.h;*.hpp;*.c|All files|*.*",OverwritePrompt=false};
            return dialog.ShowDialog()==true?dialog.FileName:null;
        }
        private static void WriteNew(string path,string contents)
        {using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(contents);}
        internal static string Prompt(EnvDTE80.DTE2 dte,string label,string initial)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var panel=new StackPanel {Margin=new Thickness(16)};ProductTheme.Apply(panel);panel.Children.Add(ProductTheme.Text(label));var input=new TextBox {Text=initial,Margin=new Thickness(0,0,0,16)};panel.Children.Add(input);
            var button=ProductTheme.Button("저장",()=>{},true);button.IsDefault=true;panel.Children.Add(button);
            var dialog=new Window {Title="CodeAssist",Content=panel,Width=560,Height=190,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};new System.Windows.Interop.WindowInteropHelper(dialog).Owner=(IntPtr)dte.MainWindow.HWnd;
            button.Click+=(s,e)=>dialog.DialogResult=true;dialog.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape)dialog.DialogResult=false;};dialog.Loaded+=(s,e)=>{input.Focus();input.SelectAll();};return dialog.ShowDialog()==true?input.Text:null;
        }
    }
}
