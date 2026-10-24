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
using CodeAssist.Core;
using CodeAssist.Indexing;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;

namespace CodeAssist.Extension
{
    internal sealed class NavigationCommands
    {
        private readonly AsyncPackage package;
        private readonly ProjectIndexService indexes;
        private readonly IntelliSenseSquiggleController controller;
        private NavigationCommands(AsyncPackage package, ProjectIndexService indexes, IntelliSenseSquiggleController controller)
        { this.package = package; this.indexes = indexes; this.controller = controller; }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package, ProjectIndexService indexes, IntelliSenseSquiggleController controller)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var service = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (service == null || indexes == null) return;
            var commands = new NavigationCommands(package, indexes, controller);
            for (var id = 0x0103; id <= 0x011e; id++)
            {
                var command = id;
                service.AddCommand(new OleMenuCommand((s, e) => package.JoinableTaskFactory.RunAsync(() => commands.ExecuteAsync(command))
                    .FileAndForget("CodeAssist/Navigation"), new CommandID(GoToSymbolCommand.CommandSet, id)));
            }
        }

        private async System.Threading.Tasks.Task ExecuteAsync(int command)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if(command==0x0116&&!StartupUi.Preferences.ClipboardMenuEnabled) {
                var clipboardDte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                clipboardDte?.ExecuteCommand("Edit.CycleClipboardRing");return;
            }
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
                        if (command == 0x011e)
                        {
                            var snapshot = view.TextSnapshot;
                            var selected = view.Selection.StreamSelectionSpan.SnapshotSpan;
                            if (selected.Length == 0) return;
                            var ascending=new RadioButton {Content=UiText.Get("Ascending",StartupUi.Preferences.UiLanguage),IsChecked=true,GroupName="SortDirection",Margin=new Thickness(0,0,14,7)};
                            var descending=new RadioButton {Content=UiText.Get("Descending",StartupUi.Preferences.UiLanguage),GroupName="SortDirection"};
                            var sensitive=new RadioButton {Content=UiText.Get("Case-sensitive",StartupUi.Preferences.UiLanguage),IsChecked=true,GroupName="SortCase",Margin=new Thickness(0,0,0,7)};
                            var ignoreCase=new RadioButton {Content=UiText.Get("Case-insensitive",StartupUi.Preferences.UiLanguage),GroupName="SortCase"};
                            var panel=new DockPanel {Margin=new Thickness(10)};ProductTheme.Apply(panel);
                            var dialog=new Window {Title=UiText.Get("CodeAssist: Sort Selected Lines",StartupUi.Preferences.UiLanguage),Content=panel,Width=280,Height=167,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner};ProductTheme.Apply(dialog);
                            var buttons=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(buttons,Dock.Bottom);panel.Children.Add(buttons);
                            var sort=ProductTheme.Button("OK",()=>dialog.DialogResult=true,true);sort.IsDefault=true;sort.Width=88;sort.Height=27;sort.Padding=new Thickness(4);sort.Margin=new Thickness(0,0,8,0);buttons.Children.Add(sort);
                            var cancel=ProductTheme.Button("Cancel",()=>dialog.DialogResult=false);cancel.IsCancel=true;cancel.Width=88;cancel.Height=27;cancel.Padding=new Thickness(4);cancel.Margin=new Thickness(0);buttons.Children.Add(cancel);
                            var columns=new Grid();columns.ColumnDefinitions.Add(new ColumnDefinition());columns.ColumnDefinitions.Add(new ColumnDefinition());panel.Children.Add(columns);
                            var direction=new StackPanel();direction.Children.Add(ascending);direction.Children.Add(descending);columns.Children.Add(direction);
                            var casing=new StackPanel();casing.Children.Add(sensitive);casing.Children.Add(ignoreCase);Grid.SetColumn(casing,1);columns.Children.Add(casing);
                            var sortDte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                            if (sortDte != null) new System.Windows.Interop.WindowInteropHelper(dialog).Owner = (IntPtr)sortDte.MainWindow.HWnd;
                            sort.Click += (s, e) => dialog.DialogResult = true;
                            dialog.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) dialog.DialogResult = false; };
                            if (dialog.ShowDialog() != true || view.IsClosed || !ReferenceEquals(snapshot, view.TextSnapshot)) return;
                            var change = SelectedLineSort.Create(snapshot.GetText(), selected.Start.Position, selected.Length, descending.IsChecked == true, ignoreCase.IsChecked == true);
                            using (var edit = view.TextBuffer.CreateEdit())
                            {
                                if (!edit.Replace(new Span(change.Start, change.Length), change.Text)) return;
                                var updated = edit.Apply();
                                view.Selection.Select(new SnapshotSpan(updated, change.Start, change.Text.Length), false);
                                view.Caret.MoveTo(new SnapshotPoint(updated, change.Start + change.Text.Length));
                            }
                        }
                        else if (command == 0x011d)
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
                            string snippetProject = null;
                            try { snippetProject = snippetDte.ActiveDocument?.ProjectItem?.ContainingProject?.FullName; }
                            catch (System.Runtime.InteropServices.COMException) { }
                            SnippetInsertion.Show(view, snippetDte.ActiveDocument?.FullName, controller?.UnrealProjectPath,
                                (IntPtr)snippetDte.MainWindow.HWnd, snippetProject, snippetDte.Solution?.FullName);
                        }
                        else if (command == 0x0116)
                        {
                            var clips = EditorClipboardHistory.History.Items();
                            var choice=new System.Threading.Tasks.TaskCompletionSource<int>();
                            var menu=new ContextMenu {PlacementTarget=view.VisualElement,Placement=System.Windows.Controls.Primitives.PlacementMode.RelativePoint,
                                HorizontalOffset=Math.Max(0,view.Caret.Left-view.ViewportLeft),VerticalOffset=Math.Max(0,view.Caret.Bottom-view.ViewportTop),MinWidth=200,MaxWidth=480};
                            menu.SetResourceReference(Control.BackgroundProperty,EnvironmentColors.ToolWindowBackgroundBrushKey);
                            menu.SetResourceReference(Control.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);
                            menu.Tag=UiText.Get("Clipboard",StartupUi.Preferences.UiLanguage);
                            menu.Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ContextMenu'><StackPanel KeyboardNavigation.DirectionalNavigation='Cycle'><Border HorizontalAlignment='Left' Padding='12,2' BorderThickness='1' BorderBrush='#888888' Background='{TemplateBinding Background}'><TextBlock Text='{TemplateBinding Tag}' Foreground='{TemplateBinding Foreground}'/></Border><Border Background='{TemplateBinding Background}' BorderBrush='#888888' BorderThickness='1' Padding='2'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Cycle'/></Border></StackPanel></ControlTemplate>");
                            var entryTemplate=(ControlTemplate)System.Windows.Markup.XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='MenuItem'><Border x:Name='Row' MinHeight='24' BorderThickness='1' BorderBrush='Transparent' Background='Transparent' Padding='34,0,5,0'><ContentPresenter ContentSource='Header' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Row' Property='Background' Value='{Binding Background,RelativeSource={RelativeSource TemplatedParent}}'/><Setter TargetName='Row' Property='BorderBrush' Value='#888888'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
                            for(var clipIndex=0;clipIndex<clips.Length;clipIndex++) {
                                var index=clipIndex;var value=clips[index];
                                var label=(index+1)+": "+(value.Length>200?value.Substring(0,200)+"…":value).Replace("\r","").Replace("\n"," ↵ ");
                                var entry=new MenuItem {Header=new TextBlock {Text=label,TextTrimming=TextTrimming.CharacterEllipsis},Template=entryTemplate};
                                entry.SetResourceReference(Control.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);
                                ProductTheme.BrushFor(entry,Control.BackgroundProperty,"MenuSelection");
                                entry.Click+=(sender,args)=>{choice.TrySetResult(index);menu.IsOpen=false;};menu.Items.Add(entry);
                            }
                            menu.Opened+=(sender,args)=>{if(menu.Items.Count>0)(menu.Items[0] as MenuItem)?.Focus();};
                            menu.Closed+=(sender,args)=>choice.TrySetResult(-1);
                            menu.IsOpen=true;
                            var selectedClip=await choice.Task;
                            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                            if(selectedClip>=0&&!view.IsClosed)
                            {
                                var clip=clips[selectedClip];
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
                            if (!StartupUi.Preferences.SmartSelectEnabled) return;
                            var selected = view.Selection.StreamSelectionSpan.SnapshotSpan;
                            var history = view.Properties.GetOrCreateSingletonProperty(() => new SelectionHistory());
                            var value = view.TextSnapshot.GetText();
                            var range = command == 0x0113 || command == 0x0115 ?
                                history.Shrink(value, selected.Start.Position, selected.Length) :
                                history.Extend(value, selected.Start.Position, selected.Length, command == 0x0114,new SmartSelectionOptions {
                                    SmallIncrements=StartupUi.Preferences.SmartSelectSmallIncrements,StartWithWord=StartupUi.Preferences.SmartSelectStartWithWord,
                                    SplitCase=StartupUi.Preferences.SmartSelectSplitCase,SplitUnderscores=StartupUi.Preferences.SmartSelectSplitUnderscores,
                                    TrimWhitespace=StartupUi.Preferences.SmartSelectTrimWhitespace});
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
            if(command==0x010b)
            {
                try{if(await CompilerActions.TryAsync(package,dte))return;}
                catch(Exception error){await package.JoinableTaskFactory.SwitchToMainThreadAsync();System.Windows.MessageBox.Show(error.Message,"CodeAssist");return;}
            }
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
                var scopeManager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                var scopeComponents=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                if(scopeManager==null||scopeComponents==null||scopeManager.GetActiveView(1,null,out var scopeAdapter)<0)return;
                var scopeView=scopeComponents.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(scopeAdapter);
                if(scopeView==null||scopeView.IsClosed)return;
                var scopeSnapshot=scopeView.TextSnapshot;
                var target=CppScopeNavigation.Move(scopeSnapshot.GetText(),scopeView.Caret.Position.BufferPosition.Position,command==0x010d);
                if(target>=0&&target<=scopeSnapshot.Length){scopeView.Selection.Clear();scopeView.Caret.MoveTo(new SnapshotPoint(scopeSnapshot,target));scopeView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(scopeSnapshot,target,0));}
                return;
            }
            if(command==0x0106) {
                var methodManager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                var methodComponents=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if(methodManager!=null&&methodComponents!=null&&methodManager.GetActiveView(1,null,out var methodAdapter)>=0&&ContextBarMargin.TryOpenMethodList(methodComponents.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(methodAdapter)))return;
            }
            controller?.Refresh();
            var projectMarker=controller?.UnrealProjectPath;
            if(string.IsNullOrWhiteSpace(projectMarker))projectMarker=dte.Solution?.FullName;
            if(!string.IsNullOrWhiteSpace(projectMarker))indexes.Initialize(projectMarker);
            else indexes.InitializeFromDocument(file);
            await indexes.WhenReadyAsync();
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if(!string.IsNullOrEmpty(indexes.LastError)){System.Windows.MessageBox.Show(indexes.LastError,"CodeAssist: Indexing");return;}
            if (command == 0x0110)
            {
                var files = indexes.Index.SourceFiles().Concat(file != null && text != null ? new[] { file } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var activeFile = file; var activeText = text;
                var tags = await System.Threading.Tasks.Task.Run(() => files.SelectMany(path =>
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
                var tagPicker = new NavigationPicker("CodeAssist: Hashtags", tags, package.JoinableTaskFactory);
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
                var includePicker = new NavigationPicker("CodeAssist: Include Files", includes, package.JoinableTaskFactory);
                new System.Windows.Interop.WindowInteropHelper(includePicker).Owner = (IntPtr)dte.MainWindow.HWnd;
                if (includePicker.ShowDialog() == true) Open(dte, includePicker.Selected);
                return;
            }
            var symbol = selection == null ? "" : IdentifierParser.At(selection.ActivePoint.CreateEditPoint().GetLines(selection.ActivePoint.Line, selection.ActivePoint.Line + 1), selection.ActivePoint.LineCharOffset - 1);
            NavigationEntry[] entries;
            if (command == 0x0103 || command == 0x0105)
            {
                entries = await System.Threading.Tasks.Task.Run(() => indexes.Index.SourceFiles().Select(path => new NavigationEntry(Path.GetFileName(path), path, 1, 1)).ToArray());
                if (command == 0x0105)
                {
                    entries = entries.Where(entry => !string.Equals(entry.File, file, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(Path.GetFileNameWithoutExtension(entry.File), Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (entries.Length == 1) { Open(dte, entries[0]); return; }
                }
            }
            else if (command == 0x0106 && file != null && text != null)
            {
                entries = await System.Threading.Tasks.Task.Run(() => BuildEntries(PersistentSymbolIndex.ReadDocumentSymbols(file, text)
                    .Where(item => item.Kind == SymbolKind.Function)));
            }
            else
            {
                var owner = text == null || selection == null ? null : CppExpressionContext.FindEnclosingFunctionOwner(text, selection.ActivePoint.AbsoluteCharOffset - 1);
                entries = await System.Threading.Tasks.Task.Run(() => BuildEntries((command == 0x0107 ? indexes.Index.RelatedSymbols(symbol, owner) :
                    indexes.Index.SearchSymbols("", limit: int.MaxValue))
                    .GroupBy(item=>item.Kind+"|"+item.OwnerType+"|"+item.Name+"|"+item.Signature+"|"+item.ValueType)
                    .Select(group=>group.OrderBy(item=>!string.IsNullOrEmpty(indexes.ProjectRoot)&&item.FilePath.StartsWith(indexes.ProjectRoot.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)?0:1).ThenBy(item=>IsImplementation(item.FilePath)?0:1).First())));
            }
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var title = command == 0x0103 ? "CodeAssist: 파일 찾기" : command == 0x0105 ? "CodeAssist: 대응 파일" : command == 0x0106 ? "CodeAssist: 현재 파일 메서드" : command == 0x0107 ? "CodeAssist: 관련 심볼 후보" : "CodeAssist: 심볼 찾기";
            var picker = new NavigationPicker(title, entries, package.JoinableTaskFactory, indexes.ProjectRoot, command == 0x0104);
            new System.Windows.Interop.WindowInteropHelper(picker).Owner = (IntPtr)dte.MainWindow.HWnd;
            if (picker.ShowDialog() == true && picker.Selected != null) { Open(dte, picker.Selected); if(picker.FindReferencesRequested) StartupUi.Execute(0x0102); }
        }
        private static bool IsImplementation(string path) => new[]{".cpp",".cxx",".cc",".c"}.Contains(Path.GetExtension(path).ToLowerInvariant());
        private static NavigationEntry[] BuildEntries(IEnumerable<IndexedSymbol> symbols)
        {
            var linesByFile = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            return symbols.Select(item =>
            {
                var fallback = item.ValueType + " " + (item.Signature.Length > 0 ? item.Signature : item.Name);
                if (!linesByFile.TryGetValue(item.FilePath, out var lines))
                {
                    try { lines = File.ReadAllLines(item.FilePath); }
                    catch (IOException) { lines = Array.Empty<string>(); }
                    catch (UnauthorizedAccessException) { lines = Array.Empty<string>(); }
                    linesByFile[item.FilePath] = lines;
                }
                string nextCodeLine = null;
                for (var next = item.Line; next >= 0 && next < lines.Length; next++)
                    if (!string.IsNullOrWhiteSpace(CppCodeMask.Create(lines[next]))) { nextCodeLine = lines[next]; break; }
                var preview = item.Line > 0 && item.Line <= lines.Length
                    ? SymbolDefinitionDisplay.FromLine(lines[item.Line - 1], fallback, nextCodeLine) : fallback.Trim();
                return new NavigationEntry((item.OwnerType.Length > 0 ? item.OwnerType + "::" : "") +
                    item.Name, item.FilePath, item.Line, item.Column, preview, item.Kind.ToString());
            }).ToArray();
        }
        private static void Open(EnvDTE80.DTE2 dte, NavigationEntry entry)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!File.Exists(entry.File)) return;
            dte.ItemOperations.OpenFile(entry.File, EnvDTE.Constants.vsViewKindTextView);
            (dte.ActiveDocument?.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(Math.Max(1, entry.Line), Math.Max(1, entry.Column), false);
        }
        private void ShowUnavailable(string command)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var message = "현재 편집기에서 Visual Studio 명령을 사용할 수 없습니다: " + command;
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte != null) dte.StatusBar.Text = message;
            ActivityLog.LogWarning("CodeAssist Navigation", message);
        }
    }
    internal sealed class NavigationEntry
    {
        public NavigationEntry(string name, string file, int line, int column, string definition = null, string kind = "Unknown") { Name=name; File=file; Line=line; Column=column; Definition=definition ?? file; Kind=kind; }
        public string Name { get; } public string File { get; } public int Line { get; } public int Column { get; }
        public string Definition { get; } public string Kind { get; }
        public Microsoft.VisualStudio.Imaging.Interop.ImageMoniker Icon => CompletionIcons.For(Enum.TryParse<SymbolKind>(Kind,out var kind)?kind:SymbolKind.Unknown);
        public override string ToString() => Name + "    " + File + (Line > 1 ? " (" + Line + ")" : "");
    }
    internal sealed class NavigationPicker : Window
    {
        private readonly ListView list = new ListView();
        public bool FindReferencesRequested { get; private set; }
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
        private static bool Subsequence(string query,string candidate)
        {
            int cursor=0;foreach(var value in candidate) if(cursor<query.Length&&char.ToUpperInvariant(query[cursor])==char.ToUpperInvariant(value))cursor++;
            return cursor==query.Length;
        }
        private sealed class SymbolInkConverter : IMultiValueConverter
        {
            public object Convert(object[] values,Type targetType,object parameter,System.Globalization.CultureInfo culture)
            {
                if(!StartupUi.Preferences.ColoringEnabled||!StartupUi.Preferences.ColorViews)return RecommendedColors.DefaultTextBrush();
                return values.Length>0&&Enum.TryParse<SymbolKind>(values[0] as string,out var kind)
                    ? (object)RecommendedColors.SymbolBrush(kind)??RecommendedColors.DefaultTextBrush() : RecommendedColors.DefaultTextBrush();
            }
            public object[] ConvertBack(object value,Type[] types,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();
        }
        public NavigationPicker(string title, NavigationEntry[] entries, Microsoft.VisualStudio.Threading.JoinableTaskFactory tasks, string solutionRoot=null, bool symbols=false)
        {
            title=UiText.Get(title,StartupUi.Preferences.UiLanguage);
            this.entries = entries; Title=title; Width=886; Height=396; MinWidth=640; MinHeight=300; WindowStartupLocation=WindowStartupLocation.CenterOwner;
            ProductTheme.Apply(this); FontSize=12;
            var panel = new DockPanel { Margin=new Thickness(10) }; ProductTheme.Apply(panel); Content=panel;
            var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            var search = new TextBox { Height=26, Padding=new Thickness(5,2,5,2), Margin=new Thickness(0,0,0,6) };top.Children.Add(search);
            System.Windows.Automation.AutomationProperties.SetName(search,UiText.Get("Search",StartupUi.Preferences.UiLanguage));
            var toggles=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,8)};top.Children.Add(toggles);
            var solution=new CheckBox {Content=UiText.Get("Solution only",StartupUi.Preferences.UiLanguage),IsChecked=symbols&&!string.IsNullOrWhiteSpace(solutionRoot),Margin=new Thickness(0,0,12,0),Visibility=symbols?Visibility.Visible:Visibility.Collapsed};toggles.Children.Add(solution);
            var fuzzy=new CheckBox {Content=UiText.Get("Fuzzy search",StartupUi.Preferences.UiLanguage),Margin=new Thickness(0,0,12,0)};toggles.Children.Add(fuzzy);
            var types=new CheckBox {Content=UiText.Get("Classes/structs/namespaces",StartupUi.Preferences.UiLanguage),Margin=new Thickness(0,0,12,0),Visibility=symbols?Visibility.Visible:Visibility.Collapsed};toggles.Children.Add(types);
            var footer=new DockPanel {Margin=new Thickness(0,7,0,0)};DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
            var buttons=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(buttons,Dock.Right);footer.Children.Add(buttons);
            var go=ProductTheme.Button("Goto",()=>{if(Selected!=null)DialogResult=true;});go.IsDefault=true;buttons.Children.Add(go);
            if(symbols) buttons.Children.Add(ProductTheme.Button("Find References",()=>{if(Selected!=null){FindReferencesRequested=true;DialogResult=true;}}));
            var cancel=ProductTheme.Button("Cancel",()=>DialogResult=false);cancel.IsCancel=true;buttons.Children.Add(cancel);
            foreach (Button button in buttons.Children) { button.Height = 28; button.Padding = new Thickness(0); button.Margin = new Thickness(6,0,0,0); button.Width = button == go ? 88 : button == cancel ? 94 : 104; }
            var brand=new StackPanel {Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};
            var brandFile=Path.Combine(Path.GetDirectoryName(typeof(NavigationPicker).Assembly.Location),"Resources","CodeAssist16.png");
            if(File.Exists(brandFile))brand.Children.Add(new Image {Source=new System.Windows.Media.Imaging.BitmapImage(new Uri(brandFile)),Width=16,Height=16,Margin=new Thickness(0,0,5,0)});
            brand.Children.Add(ProductTheme.Text("CodeAssist",12));footer.Children.Add(brand);
            var headerStyle = new Style(typeof(GridViewColumnHeader), (Style)panel.Resources[typeof(GridViewColumnHeader)]);
            headerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Left));
            headerStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(8,2,4,2)));
            var columns=new GridView {ColumnHeaderContainerStyle=headerStyle};list.View=columns;list.HorizontalContentAlignment=HorizontalAlignment.Stretch;
            var template=new DataTemplate(typeof(NavigationEntry));var label=new FrameworkElementFactory(typeof(SymbolNameTextBlock));label.SetBinding(SymbolNameTextBlock.NameTextProperty,new Binding("Name"));label.SetBinding(SymbolNameTextBlock.KindTextProperty,new Binding("Kind"));var icon=new FrameworkElementFactory(typeof(Microsoft.VisualStudio.Imaging.CrispImage));icon.SetValue(FrameworkElement.WidthProperty,16.0);icon.SetValue(FrameworkElement.HeightProperty,16.0);icon.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,5,0));icon.SetBinding(Microsoft.VisualStudio.Imaging.CrispImage.MonikerProperty,new Binding("Icon"));icon.SetValue(DockPanel.DockProperty,Dock.Left);var symbolCell=new FrameworkElementFactory(typeof(DockPanel));symbolCell.AppendChild(icon);symbolCell.AppendChild(label);template.VisualTree=symbolCell;
            columns.Columns.Add(new GridViewColumn {Header=UiText.Get(symbols||entries.Any(entry=>entry.Kind!="Unknown")?"Symbol":"File",StartupUi.Preferences.UiLanguage),Width=290,CellTemplate=template});
            var definitionTemplate=new DataTemplate(typeof(NavigationEntry));var definitionLabel=new FrameworkElementFactory(typeof(SymbolNameTextBlock));definitionLabel.SetBinding(SymbolNameTextBlock.NameTextProperty,new Binding("Definition"));definitionLabel.SetBinding(SymbolNameTextBlock.KindTextProperty,new Binding("Kind"));definitionLabel.SetValue(SymbolNameTextBlock.NormalizeScopesProperty,false);definitionTemplate.VisualTree=definitionLabel;
            columns.Columns.Add(new GridViewColumn {Header=UiText.Get(symbols||entries.Any(entry=>entry.Kind!="Unknown")?"Definition":"Path",StartupUi.Preferences.UiLanguage),Width=510,CellTemplate=definitionTemplate});
            var rows=new Style(typeof(ListViewItem));rows.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(3,2,3,2)));rows.Setters.Add(new Setter(FrameworkElement.ToolTipProperty,new Binding("File")));
            rows.Setters.Add(new Setter(Control.BackgroundProperty,Brushes.Transparent));
            rows.Setters.Add(new Setter(Control.ForegroundProperty,new System.Windows.DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));
            var rowTemplate=new ControlTemplate(typeof(ListViewItem));var border=new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty,new Binding("Background"){RelativeSource=RelativeSource.TemplatedParent});
            var presenter=new FrameworkElementFactory(typeof(GridViewRowPresenter));presenter.SetBinding(GridViewRowPresenter.ContentProperty,new Binding("Content"){RelativeSource=RelativeSource.TemplatedParent});
            presenter.SetBinding(GridViewRowPresenter.ColumnsProperty,new Binding("View.Columns"){RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(ListView),1)});
            presenter.SetBinding(FrameworkElement.MarginProperty,new Binding("Padding"){RelativeSource=RelativeSource.TemplatedParent});border.AppendChild(presenter);rowTemplate.VisualTree=border;rows.Setters.Add(new Setter(Control.TemplateProperty,rowTemplate));
            var selected=new Trigger {Property=ListViewItem.IsSelectedProperty,Value=true};selected.Setters.Add(new Setter(Control.BackgroundProperty,new Binding("Background"){RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(ListView),1),Converter=ProductTheme.Palette,ConverterParameter="Selection"}));rows.Triggers.Add(selected);
            list.ItemContainerStyle=rows;panel.Children.Add(list);
            async System.Threading.Tasks.Task Filter()
            {
                await tasks.SwitchToMainThreadAsync();
                var current=++generation;var query=search.Text;var onlySolution=solution.IsChecked==true;var onlyTypes=types.IsChecked==true;var useFuzzy=fuzzy.IsChecked==true && query.Length>0 && !query.Any(char.IsWhiteSpace) && !query.StartsWith("-",StringComparison.Ordinal);
                list.IsEnabled=false;var filter=new NavigationQuery(query);
                var matches=await System.Threading.Tasks.Task.Run(()=>entries.Where(item=>(!onlySolution||item.File.StartsWith(solutionRoot.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))&&(!onlyTypes||item.Kind=="Type")&&(filter.Matches(item.Name,item.File)||(symbols&&filter.Matches(item.Name.Replace("::","."),item.File))||(useFuzzy&&Subsequence(query,item.Name)))).OrderBy(item=>item.Name.StartsWith(query,StringComparison.OrdinalIgnoreCase)?0:1).ThenBy(item=>symbols?item.Name:string.Empty,StringComparer.OrdinalIgnoreCase).ToArray());
                await tasks.SwitchToMainThreadAsync();if(current!=generation)return;
                requestedLine=filter.Line;list.ItemsSource=matches.Take(200).ToArray();list.SelectedIndex=list.Items.Count>0?0:-1;list.IsEnabled=true;Title=title+" ["+matches.Length+"]";
            }
            search.TextChanged+=(s,e)=>Filter().FileAndForget("CodeAssist/NavigationFilter");
            fuzzy.Click+=(s,e)=>Filter().FileAndForget("CodeAssist/NavigationFilter");
            solution.Click+=(s,e)=>Filter().FileAndForget("CodeAssist/NavigationFilter");types.Click+=(s,e)=>Filter().FileAndForget("CodeAssist/NavigationFilter");
            Loaded+=(s,e)=>Filter().FileAndForget("CodeAssist/NavigationFilter");
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
