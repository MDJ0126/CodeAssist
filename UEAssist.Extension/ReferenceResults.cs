using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace UEAssist.Extension
{
    [Guid("e4c882bf-d127-4272-b93c-febd6a79f503")]
    public sealed class ReferenceResultsToolWindow : ToolWindowPane
    {
        public ReferenceResultsToolWindow() : base(null) { Caption = "UEAssist References"; Content = new ReferencePane(); }
    }
    internal sealed class ReferencePane : UserControl
    {
        private readonly ListBox list = new ListBox();
        private readonly TextBlock title = new TextBlock { Margin = new System.Windows.Thickness(5) };
        private AsyncPackage package;
        internal ReferencePane()
        {
            var panel = new DockPanel(); DockPanel.SetDock(title, Dock.Top); panel.Children.Add(title); panel.Children.Add(list); Content = panel;
            list.MouseDoubleClick += (s, e) =>
            {
                if (package == null || !(list.SelectedItem is NavigationEntry entry)) return;
                package.JoinableTaskFactory.RunAsync(async () =>
                {
                    await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var dte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                    if (dte == null) return;
                    dte.ItemOperations.OpenFile(entry.File, EnvDTE.Constants.vsViewKindTextView);
                    (dte.ActiveDocument.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(entry.Line, entry.Column, false);
                }).FileAndForget("UEAssist/ReferenceLocation");
            };
        }
        internal void Bind(AsyncPackage package, string description, NavigationEntry[] entries)
        {
            this.package = package; title.Text = description + " — " + entries.Length + " results"; list.ItemsSource = entries.ToArray();
        }
    }
    internal static class ReferenceResults
    {
        private static string description = "References";
        private static NavigationEntry[] last = Array.Empty<NavigationEntry>();
        private static int clones;
        internal static async Task PublishAsync(AsyncPackage package, string symbol, NavigationEntry[] entries, bool compiler)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            description = symbol + (compiler ? " — 컴파일러 참조" : " — 참조 후보"); last = entries.ToArray(); await ShowAsync(package, false);
        }
        internal static async Task ShowAsync(AsyncPackage package, bool clone)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(); var rows = last.ToArray(); var label = description;
            var pane = await package.ShowToolWindowAsync(typeof(ReferenceResultsToolWindow), clone ? Interlocked.Increment(ref clones) : 0, true, package.DisposalToken);
            if (pane?.Content is ReferencePane content) content.Bind(package, label, rows);
        }
    }
}
