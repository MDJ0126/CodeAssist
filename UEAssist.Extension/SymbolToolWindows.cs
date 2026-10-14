using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UEAssist.Core;
using UEAssist.Indexing;

namespace UEAssist.Extension
{
    [Guid("e4c882bf-d127-4272-b93c-febd6a79f501")]
    public sealed class OutlineToolWindow : ToolWindowPane
    {
        public OutlineToolWindow() : base(null) { Caption = "UEAssist Outline"; Content = new SymbolPane(false); }
    }
    [Guid("e4c882bf-d127-4272-b93c-febd6a79f502")]
    public sealed class WorkspaceToolWindow : ToolWindowPane
    {
        public WorkspaceToolWindow() : base(null) { Caption = "UEAssist Workspace"; Content = new SymbolPane(true); }
    }
    internal sealed class SymbolPane : UserControl
    {
        private readonly bool workspace;
        private readonly TreeView tree = new TreeView();
        private readonly TextBox search = new TextBox { Margin = new Thickness(5) };
        private readonly TextBlock status = new TextBlock { Margin = new Thickness(5) };
        private readonly DispatcherTimer debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        private AsyncPackage package;
        private ProjectIndexService index;
        private EnvDTE80.DTE2 dte;
        private EnvDTE.WindowEvents windows;
        private EnvDTE.TextEditorEvents editor;
        private bool attached;
        private int revision;
        private bool refreshing;
        private bool refreshQueued;
        internal SymbolPane(bool workspace)
        {
            this.workspace = workspace;
            var panel = new DockPanel(); DockPanel.SetDock(search, Dock.Top); panel.Children.Add(search);
            DockPanel.SetDock(status, Dock.Bottom); panel.Children.Add(status); panel.Children.Add(tree); Content = panel;
            search.TextChanged += (s, e) => Schedule();
            debounce.Tick += (s, e) => { debounce.Stop(); if (package != null) package.JoinableTaskFactory.RunAsync(RefreshAsync).FileAndForget("UEAssist/SymbolPane"); };
            Loaded += (s, e) => Attach(); Unloaded += (s, e) => Detach();
            tree.MouseDoubleClick += (s, e) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (!(tree.SelectedItem is TreeViewItem item) || !(item.Tag is NavigationEntry entry)) return;
                dte.ItemOperations.OpenFile(entry.File, EnvDTE.Constants.vsViewKindTextView);
                (dte.ActiveDocument.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(entry.Line, entry.Column, false);
            };
        }
        internal void Bind(AsyncPackage package, ProjectIndexService index, EnvDTE80.DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread(); Detach(); this.package = package; this.index = index; this.dte = dte;
            if (IsLoaded) Attach();
        }
        private void Attach()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (attached || dte == null || index == null) return;
            attached = true; windows = dte.Events.WindowEvents; editor = dte.Events.TextEditorEvents;
            windows.WindowActivated += OnWindowActivated; editor.LineChanged += OnLineChanged; index.IndexUpdated += OnIndexUpdated; Schedule();
        }
        private void Detach()
        {
            ThreadHelper.ThrowIfNotOnUIThread(); revision++; debounce.Stop();
            if (!attached) return;
            windows.WindowActivated -= OnWindowActivated; editor.LineChanged -= OnLineChanged; index.IndexUpdated -= OnIndexUpdated;
            windows = null; editor = null; attached = false;
        }
        private void OnWindowActivated(EnvDTE.Window active, EnvDTE.Window previous) => Schedule();
        private void OnLineChanged(EnvDTE.TextPoint start, EnvDTE.TextPoint end, int hint) { if (!workspace) Schedule(); }
        private void OnIndexUpdated(object sender, EventArgs args)
        {
            package.JoinableTaskFactory.RunAsync(async () =>
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken); Schedule();
            }).FileAndForget("UEAssist/SymbolPaneUpdate");
        }
        private void Schedule() { revision++; if (!attached) return; debounce.Stop(); debounce.Start(); }
        private async System.Threading.Tasks.Task RefreshAsync()
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            if (refreshing) { refreshQueued = true; return; }
            refreshing = true;
            try { await RefreshCoreAsync(); }
            finally { refreshing = false; if (refreshQueued) { refreshQueued = false; Schedule(); } }
        }
        private async System.Threading.Tasks.Task RefreshCoreAsync()
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!attached) return;
            var current = revision; var query = search.Text;
            var filter = new NavigationQuery(query);
            string file = null, text = null;
            if (!workspace)
            {
                try
                {
                    file = dte.ActiveDocument?.FullName;
                    var document = dte.ActiveDocument?.Object("TextDocument") as EnvDTE.TextDocument;
                    text = document?.StartPoint.CreateEditPoint().GetText(document.EndPoint);
                }
                catch (COMException) { }
            }
            var symbols = await System.Threading.Tasks.Task.Run(() =>
                (workspace ? index.Index.SearchSymbols("", limit: int.MaxValue) :
                string.IsNullOrEmpty(file) || text == null ? Array.Empty<IndexedSymbol>() : PersistentSymbolIndex.ReadDocumentSymbols(file, text))
                .Where(symbol => !symbol.IsLocal && !string.IsNullOrEmpty(symbol.FilePath) && filter.Matches(symbol.Name, symbol.FilePath))
                .ToArray());
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!attached || current != revision) return;
            tree.Items.Clear();
            foreach (var group in symbols.Take(5000).GroupBy(symbol => workspace ? symbol.FilePath : symbol.OwnerType))
            {
                var parent = new TreeViewItem { Header = string.IsNullOrEmpty(group.Key) ? "Global" : workspace ? System.IO.Path.GetFileName(group.Key) : group.Key, IsExpanded = !workspace };
                foreach (var symbol in group.OrderBy(symbol => symbol.Line))
                    parent.Items.Add(new TreeViewItem { Header = symbol.Name + (string.IsNullOrEmpty(symbol.Signature) ? "" : " — " + symbol.Signature),
                        Tag = new NavigationEntry(symbol.Name, symbol.FilePath, symbol.Line, symbol.Column) });
                tree.Items.Add(parent);
            }
            status.Text = Math.Min(5000, symbols.Length) + " / " + symbols.Length + " symbols";
        }
    }
}
