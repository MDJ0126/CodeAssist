using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UEAssist.Core;
using UEAssist.Indexing;

namespace UEAssist.Extension
{
    internal sealed class NavigationCommands
    {
        private readonly AsyncPackage package;
        private readonly ProjectIndexService indexes;
        private readonly IntelliSenseSquiggleController controller;
        private NavigationCommands(AsyncPackage package, ProjectIndexService indexes, IntelliSenseSquiggleController controller)
        { this.package = package; this.indexes = indexes; this.controller = controller; }

        public static async Task InitializeAsync(AsyncPackage package, ProjectIndexService indexes, IntelliSenseSquiggleController controller)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var service = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (service == null || indexes == null) return;
            var commands = new NavigationCommands(package, indexes, controller);
            for (var id = 0x0103; id <= 0x011d; id++)
            {
                var command = id;
                service.AddCommand(new MenuCommand((s, e) => package.JoinableTaskFactory.RunAsync(() => commands.ExecuteAsync(command))
                    .FileAndForget("UEAssist/Navigation"), new CommandID(GoToSymbolCommand.CommandSet, id)));
            }
        }

        private async Task ExecuteAsync(int command)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (command == 0x011b || command == 0x011c)
            { await ReferenceResults.ShowAsync(package, command == 0x011c); return; }
            if (command == 0x0118 || command == 0x0119)
            {
                var pane = await package.ShowToolWindowAsync(command == 0x0118 ? typeof(OutlineToolWindow) : typeof(WorkspaceToolWindow), 0, true, package.DisposalToken);
                var paneDte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                if (pane?.Content is SymbolPane content && paneDte != null) content.Bind(package, indexes, paneDte);
                return;
            }
            if (command == 0x010f || command >= 0x0112)
            {
                var manager = await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                var components = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                if (manager != null && components != null && manager.GetActiveView(1, null, out var adapter) >= 0)
                {
                    var view = components?.GetService<IVsEditorAdaptersFactoryService>()?.GetWpfTextView(adapter);
                    if (view != null && !view.IsClosed)
                    {
                        if (command == 0x011d)
                        {
                            if (!view.TextBuffer.ContentType.IsOfType("C/C++")) return;
                            if (view.TextBuffer.Properties.TryGetProperty(typeof(LiveDocumentTracker), out LiveDocumentTracker tracker))
                                tracker.Reparse();
                            else if (components.GetService<ITextDocumentFactoryService>()?.TryGetTextDocument(view.TextBuffer, out var document) == true)
                            {
                                tracker = view.TextBuffer.Properties.GetOrCreateSingletonProperty(() =>
                                    new LiveDocumentTracker(view.TextBuffer, document.FilePath, indexes));
                                tracker.Attach(view);
                            }
                        }
                        else if (command == 0x010f) view.ZoomLevel = 100;
                        else if (command == 0x011a) WordHighlights.For(view).Activate();
                        else if (command == 0x0117)
                        {
                            var snippetDte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                            if (snippetDte == null) return;
                            SnippetInsertion.Show(view, snippetDte.ActiveDocument?.FullName, controller?.UnrealProjectPath,
                                (IntPtr)snippetDte.MainWindow.HWnd);
                        }
                        else if (command == 0x0116)
                        {
                            var clips = EditorClipboardHistory.History.Items();
                            var chooser = new ListBox { ItemsSource = clips.Select((value, index) =>
                                (index + 1) + ". " + (value.Length > 200 ? value.Substring(0, 200) + "…" : value).Replace("\r", "").Replace("\n", " ↵ ")).ToArray(), SelectedIndex = clips.Length > 0 ? 0 : -1 };
                            var dialog = new Window { Title = "UEAssist: Multiple Clipboards", Width = 750, Height = 350,
                                Content = chooser, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                            dialog.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) dialog.DialogResult = false;
                                if (e.Key == Key.Enter && chooser.SelectedIndex >= 0) dialog.DialogResult = true; };
                            chooser.MouseDoubleClick += (s, e) => { if (chooser.SelectedIndex >= 0) dialog.DialogResult = true; };
                            var clipboardDte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                            if (clipboardDte != null) new System.Windows.Interop.WindowInteropHelper(dialog).Owner = (IntPtr)clipboardDte.MainWindow.HWnd;
                            if (dialog.ShowDialog() == true && !view.IsClosed)
                            {
                                var clip = clips[chooser.SelectedIndex];
                                try { Clipboard.SetText(clip); }
                                catch (System.Runtime.InteropServices.ExternalException) { return; }
                                using (var edit = view.TextBuffer.CreateEdit())
                                {
                                    if (edit.Replace(view.Selection.StreamSelectionSpan.SnapshotSpan.Span, clip)) edit.Apply();
                                }
                            }
                        }
                        else
                        {
                            var selected = view.Selection.StreamSelectionSpan.SnapshotSpan;
                            var history = view.Properties.GetOrCreateSingletonProperty(() => new SelectionHistory());
                            var value = view.TextSnapshot.GetText();
                            var range = command == 0x0113 || command == 0x0115 ?
                                history.Shrink(value, selected.Start.Position, selected.Length) :
                                history.Extend(value, selected.Start.Position, selected.Length, command == 0x0114);
                            view.Selection.Select(new SnapshotSpan(view.TextSnapshot, range.Start, range.Length), false);
                            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, range.End));
                            view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(view.TextSnapshot, range.Start, range.Length));
                        }
                    }
                }
                return;
            }
            var dte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if (dte == null) return;
            if (command == 0x010a && await CompilerRename.TryAsync(package, dte, controller?.UnrealProjectPath)) return;
            if (command >= 0x0108 && command <= 0x010c)
            {
                var native = new[] { "View.NavigateBackward", "View.NavigateForward", "Refactor.Rename", "View.QuickActions", "View.QuickActions" }[command - 0x0108];
                try
                {
                    if (!dte.Commands.Item(native).IsAvailable) { ShowUnavailable(native); return; }
                    dte.ExecuteCommand(native);
                }
                catch (System.Runtime.InteropServices.COMException) { ShowUnavailable(native); }
                catch (ArgumentException) { ShowUnavailable(native); }
                return;
            }
            var file = dte.ActiveDocument?.FullName;
            var selection = dte.ActiveDocument?.Selection as EnvDTE.TextSelection;
            EnvDTE.TextDocument textDocument = null;
            try { textDocument = dte.ActiveDocument?.Object("TextDocument") as EnvDTE.TextDocument; }
            catch (System.Runtime.InteropServices.COMException) { }
            var text = textDocument?.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            if (command == 0x010d || command == 0x010e)
            {
                if (selection == null || text == null) return;
                var position = selection.ActivePoint.AbsoluteCharOffset - 1;
                var target = CppScopeNavigation.Move(text, position, command == 0x010d);
                if (target >= 0) selection.MoveToAbsoluteOffset(target + 1, false);
                return;
            }
            controller?.Refresh(); indexes.Initialize(controller?.UnrealProjectPath);
            if (command == 0x0110)
            {
                var files = indexes.Index.SourceFiles().Concat(file != null && text != null ? new[] { file } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var activeFile = file; var activeText = text;
                var tags = await Task.Run(() => files.SelectMany(path =>
                {
                    try
                    {
                        var contents = string.Equals(path, activeFile, StringComparison.OrdinalIgnoreCase) && activeText != null ? activeText : File.ReadAllText(path);
                        return CppDocumentNavigation.Hashtags(contents).Select(tag => new NavigationEntry(tag.Name, path, tag.Line, 1)).ToArray();
                    }
                    catch (IOException) { return Array.Empty<NavigationEntry>(); }
                    catch (UnauthorizedAccessException) { return Array.Empty<NavigationEntry>(); }
                }).OrderBy(item => item.Name).ThenBy(item => item.File).ToArray());
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                var tagPicker = new NavigationPicker("UEAssist: Hashtags", tags, package.JoinableTaskFactory);
                new System.Windows.Interop.WindowInteropHelper(tagPicker).Owner = (IntPtr)dte.MainWindow.HWnd;
                if (tagPicker.ShowDialog() == true) Open(dte, tagPicker.Selected);
                return;
            }
            if (command == 0x0111)
            {
                if (text == null || file == null) return;
                var files = indexes.Index.SourceFiles();
                var includes = CppDocumentNavigation.Includes(text).SelectMany(include =>
                {
                    var relative = include.Name.Replace('/', Path.DirectorySeparatorChar);
                    var local = Path.Combine(Path.GetDirectoryName(file), relative);
                    return files.Where(path => path.EndsWith(Path.DirectorySeparatorChar + relative, StringComparison.OrdinalIgnoreCase))
                        .Concat(File.Exists(local) ? new[] { local } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(path => new NavigationEntry(include.Name, path, 1, 1));
                }).ToArray();
                var includePicker = new NavigationPicker("UEAssist: Include Files", includes, package.JoinableTaskFactory);
                new System.Windows.Interop.WindowInteropHelper(includePicker).Owner = (IntPtr)dte.MainWindow.HWnd;
                if (includePicker.ShowDialog() == true) Open(dte, includePicker.Selected);
                return;
            }
            var symbol = selection == null ? "" : IdentifierParser.At(selection.ActivePoint.CreateEditPoint().GetLines(selection.ActivePoint.Line, selection.ActivePoint.Line + 1), selection.ActivePoint.LineCharOffset - 1);
            NavigationEntry[] entries;
            if (command == 0x0103 || command == 0x0105)
            {
                entries = await Task.Run(() => indexes.Index.SourceFiles().Select(path => new NavigationEntry(Path.GetFileName(path), path, 1, 1)).ToArray());
                if (command == 0x0105)
                {
                    entries = entries.Where(entry => !string.Equals(entry.File, file, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(Path.GetFileNameWithoutExtension(entry.File), Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (entries.Length == 1) { Open(dte, entries[0]); return; }
                }
            }
            else if (command == 0x0106 && file != null && text != null)
            {
                entries = await Task.Run(() => PersistentSymbolIndex.ReadDocumentSymbols(file, text)
                    .Where(item => item.Kind == SymbolKind.Function).Select(Entry).ToArray());
            }
            else
            {
                var owner = text == null || selection == null ? null : CppExpressionContext.FindEnclosingFunctionOwner(text, selection.ActivePoint.AbsoluteCharOffset - 1);
                entries = await Task.Run(() => (command == 0x0107 ? indexes.Index.RelatedSymbols(symbol, owner) :
                    indexes.Index.SearchSymbols("", limit: int.MaxValue)).Select(Entry).ToArray());
            }
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var title = command == 0x0103 ? "UEAssist: 파일 찾기" : command == 0x0105 ? "UEAssist: 대응 파일" : command == 0x0106 ? "UEAssist: 현재 파일 메서드" : command == 0x0107 ? "UEAssist: 관련 심볼 후보" : "UEAssist: 심볼 찾기";
            var picker = new NavigationPicker(title, entries, package.JoinableTaskFactory);
            new System.Windows.Interop.WindowInteropHelper(picker).Owner = dte.MainWindow.HWnd;
            if (picker.ShowDialog() == true && picker.Selected != null) Open(dte, picker.Selected);
        }
        private static NavigationEntry Entry(IndexedSymbol item) => new NavigationEntry((item.OwnerType.Length > 0 ? item.OwnerType + "::" : "") +
            (item.Signature.Length > 0 ? item.Signature : item.Name), item.FilePath, item.Line, item.Column);
        private static void Open(EnvDTE80.DTE2 dte, NavigationEntry entry)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!File.Exists(entry.File)) return;
            dte.ItemOperations.OpenFile(entry.File, EnvDTE.Constants.vsViewKindTextView);
            (dte.ActiveDocument?.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(Math.Max(1, entry.Line), Math.Max(1, entry.Column), false);
        }
        private void ShowUnavailable(string command)
        {
            VsShellUtilities.ShowMessageBox(package, "현재 편집기에서 Visual Studio 명령을 사용할 수 없습니다: " + command,
                "UEAssist", Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK, Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
    internal sealed class NavigationEntry
    {
        public NavigationEntry(string name, string file, int line, int column) { Name=name; File=file; Line=line; Column=column; }
        public string Name { get; } public string File { get; } public int Line { get; } public int Column { get; }
        public override string ToString() => Name + "    " + File + (Line > 1 ? " (" + Line + ")" : "");
    }
    internal sealed class NavigationPicker : Window
    {
        private readonly ListBox list = new ListBox();
        private readonly NavigationEntry[] entries;
        private int generation;
        private int? requestedLine;
        public NavigationEntry Selected
        {
            get
            {
                if (!list.IsEnabled) return null;
                var entry = list.SelectedItem as NavigationEntry;
                return entry == null || !requestedLine.HasValue ? entry :
                    new NavigationEntry(entry.Name, entry.File, requestedLine.Value, 1);
            }
        }
        public NavigationPicker(string title, NavigationEntry[] entries, Microsoft.VisualStudio.Threading.JoinableTaskFactory tasks)
        {
            this.entries = entries; Title=title; Width=850; Height=450; WindowStartupLocation=WindowStartupLocation.CenterOwner;
            var panel = new DockPanel { Margin=new Thickness(12) };
            var search = new TextBox { Margin=new Thickness(0,0,0,10) };
            DockPanel.SetDock(search, Dock.Top); panel.Children.Add(search); panel.Children.Add(list); Content=panel;
            search.TextChanged += (s,e) => tasks.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var current = ++generation; var query = search.Text;
                list.IsEnabled = false;
                var filter = new NavigationQuery(query);
                var matches = await Task.Run(() => entries.Where(item => filter.Matches(item.Name, item.File))
                    .OrderBy(item => item.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1).Take(200).ToArray());
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (current != generation) return;
                requestedLine = filter.Line;
                list.ItemsSource=matches; list.SelectedIndex=matches.Length > 0 ? 0 : -1;
                list.IsEnabled = true;
            }).FileAndForget("UEAssist/NavigationFilter");
            Closed += (s,e) => generation++;
            PreviewKeyDown += (s,e) =>
            {
                if (e.Key == Key.Escape) { DialogResult=false; e.Handled=true; }
                if (e.Key == Key.Enter && Selected != null) { DialogResult=true; e.Handled=true; }
                if (e.Key == Key.Down && search.IsKeyboardFocusWithin) { list.SelectedIndex=Math.Min(list.Items.Count-1, list.SelectedIndex+1); e.Handled=true; }
                if (e.Key == Key.Up && search.IsKeyboardFocusWithin) { list.SelectedIndex=Math.Max(0, list.SelectedIndex-1); e.Handled=true; }
            };
            list.MouseDoubleClick += (s,e) => { if (Selected != null) DialogResult=true; };
            list.ItemsSource=entries.Take(200).ToArray(); list.SelectedIndex=list.Items.Count > 0 ? 0 : -1;
            Loaded += (s,e) => search.Focus();
        }
    }
}
