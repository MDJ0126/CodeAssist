using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CodeAssist.Core;

namespace CodeAssist.Extension
{
    internal static class SnippetInsertion
    {
        private static Dictionary<string, string> environment;
        internal static void CaptureEnvironment()
        {
            if (environment != null) return;
            environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
                environment[(string)entry.Key] = (string)entry.Value;
        }
        internal static readonly SnippetTemplate[] BuiltIns =
        {
            new SnippetTemplate("Conditional directive", "#", "#ifdef $Condition=_DEBUG$\n$selected$\n#endif // $Condition$$end$"),
            new SnippetTemplate("Include", "include", "#include \"$Header$\"$end$"),
            new SnippetTemplate("If block", "if", "if ($Condition$)\n{\n    $selected$$end$\n}"),
            new SnippetTemplate("For loop", "for", "for (int $Index$ = 0; $Index$ < $Count$; ++$Index$)\n{\n    $selected$$end$\n}"),
            new SnippetTemplate("Unreal property", "uproperty", "UPROPERTY($Specifiers$)\n$Type$ $Name$;$end$"),
            new SnippetTemplate("Unreal function", "ufunction", "UFUNCTION($Specifiers$)\n$ReturnType$ $Name$($Arguments$);$end$"),
            new SnippetTemplate("Unreal struct", "ustruct", "USTRUCT($Specifiers$)\nstruct $Name$\n{\n    GENERATED_BODY()\n\n    $selected$$end$\n};"),
            new SnippetTemplate("Header guard", "guard", "#ifndef $FILE_BASE$_H\n#define $FILE_BASE$_H\n\n$selected$$end$\n\n#endif")
        };
        private static readonly Dictionary<string,Tuple<DateTime,SnippetTemplate[]>> cachedLibraries=new Dictionary<string,Tuple<DateTime,SnippetTemplate[]>>(StringComparer.OrdinalIgnoreCase);
        internal static SnippetTemplate[] Suggestions(string root,string prefix)
        {
            var templates=BuiltIns;
            if(!string.IsNullOrEmpty(root))
            {
                var path=Path.Combine(root,".codeassist","snippets.tpl");
                try {var info=new FileInfo(path);if(info.Exists&&info.Length<=1024*1024){if(!cachedLibraries.TryGetValue(path,out var cached)||cached.Item1!=info.LastWriteTimeUtc){cached=Tuple.Create(info.LastWriteTimeUtc,SnippetTemplate.Parse(File.ReadAllText(path)).ToArray());if(cachedLibraries.Count>=32)cachedLibraries.Clear();cachedLibraries[path]=cached;}templates=templates.Concat(cached.Item2).ToArray();}}
                catch(IOException){}catch(UnauthorizedAccessException){}
            }
            return templates.Where(t=>!string.IsNullOrEmpty(prefix)&&t.Shortcut.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)).GroupBy(t=>t.Shortcut,StringComparer.Ordinal).Select(g=>g.Last()).Take(30).ToArray();
        }
        internal static void SurroundDirective(IWpfTextView view)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte=Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var file=dte?.ActiveDocument?.FullName;var solution=dte?.Solution?.FullName;
            Show(view,file,string.IsNullOrEmpty(solution)?null:Path.GetDirectoryName(solution),dte==null?IntPtr.Zero:(IntPtr)dte.MainWindow.HWnd,null,solution,true,"#");
        }
        internal static bool TrySurroundCharacter(IWpfTextView view,char key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(view.Selection.IsEmpty||char.IsControl(key)||char.IsWhiteSpace(key))return false;
            var dte=Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var solution=dte?.Solution?.FullName;var root=string.IsNullOrEmpty(solution)?null:Path.GetDirectoryName(solution);
            var path=root==null?null:Path.Combine(root,".codeassist","snippets.tpl");
            SnippetTemplate[] templates;
            try {
                if(path==null||!File.Exists(path)||new FileInfo(path).Length>1024*1024)return false;
                templates=SnippetTemplate.Parse(File.ReadAllText(path)).Where(t=>t.CanSurround(key)).ToArray();
            } catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}
            if(templates.Length==0)return false;
            void Commit(SnippetTemplate template) => Show(view,dte?.ActiveDocument?.FullName,root,dte==null?IntPtr.Zero:(IntPtr)dte.MainWindow.HWnd,null,solution,true,key.ToString(),null,new[]{template});
            if(templates.Length==1){Commit(templates[0]);return true;}
            var snapshot=view.TextSnapshot;var selected=view.Selection.StreamSelectionSpan.SnapshotSpan;
            var menu=new ContextMenu {PlacementTarget=view.VisualElement,Placement=System.Windows.Controls.Primitives.PlacementMode.RelativePoint,HorizontalOffset=view.Caret.Left-view.ViewportLeft,VerticalOffset=view.Caret.Bottom-view.ViewportTop};
            ProductTheme.Apply(menu);bool escape=false;
            foreach(var template in templates){var candidate=template;var item=new MenuItem {Header=(string.IsNullOrEmpty(template.Title)?template.Body:template.Title).Replace("&","_")};item.Click+=(s,e)=>Commit(candidate);menu.Items.Add(item);}
            menu.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape){escape=true;e.Handled=true;menu.IsOpen=false;}};
            menu.Closed+=(s,e)=> {
                if(!escape)return;
                view.VisualElement.Dispatcher.BeginInvoke(new Action(()=> {
                    if(view.IsClosed||!ReferenceEquals(snapshot,view.TextSnapshot))return;
                    var model=Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
                    var histories=model?.GetService<Microsoft.VisualStudio.Text.Operations.ITextUndoHistoryRegistry>();Microsoft.VisualStudio.Text.Operations.ITextUndoHistory history=null;histories?.TryGetHistory(view.TextBuffer,out history);
                    using(var transaction=history?.CreateTransaction("CodeAssist Surround Character")) {
                        var updated=view.TextBuffer.Replace(selected.Span,key.ToString());view.Selection.Clear();view.Caret.MoveTo(new SnapshotPoint(updated,selected.Start.Position+1));transaction?.Complete();
                    }
                }));
            };
            menu.IsOpen=true;return true;
        }
        internal static void CommitSuggestion(IWpfTextView view,string shortcut,SnapshotSpan span)
        {
            ThreadHelper.ThrowIfNotOnUIThread();var dte=Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var solution=dte?.Solution?.FullName;var root=string.IsNullOrEmpty(solution)?null:Path.GetDirectoryName(solution);
            Show(view,dte?.ActiveDocument?.FullName,root,dte==null?IntPtr.Zero:(IntPtr)dte.MainWindow.HWnd,null,solution,false,shortcut,span);
        }
        internal static bool TryShortcut(IWpfTextView view)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(!StartupUi.Preferences.SnippetShortcutsEnabled||!view.Selection.IsEmpty)return false;
            var point=view.Caret.Position.BufferPosition;var line=point.GetContainingLine();var before=line.GetText().Substring(0,point.Position-line.Start.Position);
            var match=System.Text.RegularExpressions.Regex.Match(before,@"[A-Za-z_][A-Za-z_0-9]*$");if(!match.Success)return false;
            var file=view.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument),out ITextDocument document)?document.FilePath:null;
            var dte=Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var solution=dte?.Solution?.FullName;var root=string.IsNullOrEmpty(solution)?null:Path.GetDirectoryName(solution);
            var templates=new List<SnippetTemplate>(BuiltIns);
            try{var path=root==null?null:Path.Combine(root,".codeassist","snippets.tpl");if(path!=null&&File.Exists(path)&&new FileInfo(path).Length<=1024*1024)templates.AddRange(SnippetTemplate.Parse(File.ReadAllText(path)));}catch(IOException){}catch(UnauthorizedAccessException){}
            if(!templates.Any(t=>t.Shortcut==match.Value))return false;
            Show(view,file,root,dte==null?IntPtr.Zero:(IntPtr)dte.MainWindow.HWnd,null,solution,false,match.Value,new SnapshotSpan(point.Snapshot,line.Start.Position+match.Index,match.Length));return true;
        }
        internal static void Show(IWpfTextView view, string file, string projectRoot, IntPtr owner, string projectFile = null, string solutionFile = null, bool surroundOnly = false, string shortcut = null, SnapshotSpan? replacement = null, SnippetTemplate[] selectionTemplates = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var snapshot = view.TextSnapshot;
            var selection = replacement ?? view.Selection.StreamSelectionSpan.SnapshotSpan;
            var templates = new List<SnippetTemplate>(BuiltIns);
            if(string.IsNullOrEmpty(projectRoot)&&!string.IsNullOrEmpty(solutionFile))projectRoot=Path.GetDirectoryName(solutionFile);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                if (!Directory.Exists(projectRoot)) projectRoot = Path.GetDirectoryName(projectRoot);
                var path = Path.Combine(projectRoot, ".codeassist", "snippets.tpl");
                try { if (File.Exists(path)) templates.AddRange(SnippetTemplate.Parse(File.ReadAllText(path))); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            if(selectionTemplates!=null)templates=selectionTemplates.ToList();
            else if(shortcut!=null)templates=templates.Where(t=>string.Equals(t.Shortcut,shortcut,StringComparison.Ordinal)).ToList();
            if(surroundOnly)templates=templates.Where(t=>t.Variables().Contains("selected")).ToList();
            if(templates.Count==0)return;
            CaptureEnvironment();
            templates = templates.Select(template => template.WithEnvironment(name => environment.TryGetValue(name, out var value) ? value : "")).ToList();
            var reserved = SnippetReservedValues.Create(file, shortcut==null||surroundOnly?selection.GetText():"", DateTime.Now, System.Globalization.CultureInfo.CurrentCulture);
            SnippetReservedValues.AddWorkspace(reserved, projectFile, solutionFile, Guid.NewGuid(), System.Globalization.CultureInfo.CurrentCulture);
            var clipboardCaptured = false;
            var panel = new DockPanel { Margin = new Thickness(12) };
            var choice = new ComboBox { ItemsSource = templates, Margin = new Thickness(0, 0, 0, 10) };
            var fields = new StackPanel();
            var preview = new TextBox { IsReadOnly = true, AcceptsReturn = true, FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            var insert = ProductTheme.Button("삽입",()=>{},true);insert.IsDefault=true;insert.Margin=new Thickness(0,10,0,0);insert.Padding=new Thickness(15,5,15,5);
            DockPanel.SetDock(choice, Dock.Top); panel.Children.Add(choice);
            var fieldsHost = new ScrollViewer { Content = fields, MaxHeight = 250, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            DockPanel.SetDock(fieldsHost, Dock.Top); panel.Children.Add(fieldsHost);
            DockPanel.SetDock(insert, Dock.Bottom); panel.Children.Add(insert); panel.Children.Add(preview);
            ProductTheme.Apply(panel);
            var dialog = new Window { Title = "CodeAssist: Snippets", Content = panel, Width = 650, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            new System.Windows.Interop.WindowInteropHelper(dialog).Owner = owner;
            var inputs = new Dictionary<string, TextBox>(StringComparer.Ordinal);
            Func<SnippetExpansion> expand = () =>
            {
                var values = new Dictionary<string, string>(reserved, StringComparer.Ordinal);
                foreach (var pair in inputs) values[pair.Key] = pair.Value.Text;
                var template = (SnippetTemplate)choice.SelectedItem;
                var newline = snapshot.GetText().Contains("\r\n") ? "\r\n" : snapshot.GetText().Contains("\n") ? "\n" : view.Options.GetOptionValue(DefaultOptions.NewLineCharacterOptionId);
                return new SnippetTemplate(template.Title, template.Shortcut, template.Body.Replace("\r\n", "\n").Replace("\n", newline)).Expand(values);
            };
            choice.SelectionChanged += (s, e) =>
            {
                inputs.Clear(); fields.Children.Clear();
                if (((SnippetTemplate)choice.SelectedItem).Variables().Contains("clipboard") && !clipboardCaptured)
                {
                    try { reserved["clipboard"] = Clipboard.ContainsText() ? Clipboard.GetText() : ""; clipboardCaptured = true; }
                    catch (System.Runtime.InteropServices.ExternalException) { reserved["clipboard"] = ""; }
                }
                var inputFields = ((SnippetTemplate)choice.SelectedItem).Inputs(reserved);
                insert.IsEnabled = inputFields.Length <= 8;
                if (inputFields.Length > 8) fields.Children.Add(new TextBlock { Text = "입력 자리표시자는 최대 8개까지 지원합니다." });
                foreach (var field in inputFields)

                {
                    var name = field.Name;
                    fields.Children.Add(new TextBlock { Text = name });
                    var input = new TextBox { Text = field.DefaultValue ?? "", Margin = new Thickness(0, 2, 0, 7) }; inputs[name] = input; fields.Children.Add(input);
                    input.TextChanged += (sender, args) => preview.Text = expand().Text;
                }
                preview.Text = expand().Text;
            };
            insert.Click += (s, e) => dialog.DialogResult = true;
            dialog.PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) dialog.DialogResult = false; };
            choice.SelectedIndex = 0;
            if ((shortcut==null || inputs.Count>0) && dialog.ShowDialog() != true || view.IsClosed) return;
            if (!ReferenceEquals(snapshot, view.TextSnapshot))
            {
                MessageBox.Show("파일 내용이 변경되었습니다. 삽입할 위치를 다시 선택해 주세요.", "CodeAssist"); return;
            }
            var result = expand();
            var model=Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
            var histories=model?.GetService<Microsoft.VisualStudio.Text.Operations.ITextUndoHistoryRegistry>();
            Microsoft.VisualStudio.Text.Operations.ITextUndoHistory history=null;histories?.TryGetHistory(view.TextBuffer,out history);
            var operations=model?.GetService<Microsoft.VisualStudio.Text.Operations.IEditorOperationsFactoryService>()?.GetEditorOperations(view);
            using(var undo=history?.CreateTransaction("CodeAssist Snippet"))
            using (var edit = view.TextBuffer.CreateEdit())
            {
                if(undo!=null)operations?.AddBeforeTextBufferChangePrimitive();
                if (!edit.Replace(selection.Span, result.Text)){undo?.Cancel();return;}
                var updated = edit.Apply();
                if(edit.Canceled){undo?.Cancel();return;}
                view.Selection.Clear(); view.Caret.MoveTo(new SnapshotPoint(updated, selection.Start.Position + result.Caret));
                if(undo!=null)operations?.AddAfterTextBufferChangePrimitive();undo?.Complete();
            }
        }
    }
}
