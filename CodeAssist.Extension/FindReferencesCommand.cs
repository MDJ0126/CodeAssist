using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using CodeAssist.Core;

namespace CodeAssist.Extension
{
    internal sealed class FindReferencesCommand
    {
        public const int CommandId = 0x0102;
        private static readonly Guid OutputPaneGuid = new Guid("5725d377-6839-49a5-b9e2-67694bb4594f");
        private readonly AsyncPackage package;
        private readonly IntelliSenseSquiggleController controller;
        private readonly ProjectIndexService indexService;
        private CancellationTokenSource pendingSearch;

        private FindReferencesCommand(AsyncPackage package, IntelliSenseSquiggleController controller, ProjectIndexService indexService, OleMenuCommandService commandService)
        {
            this.package = package;
            this.controller = controller;
            this.indexService = indexService;
            commandService.AddCommand(new MenuCommand(Execute, new CommandID(GoToSymbolCommand.CommandSet, CommandId)));
        }

        public static async Task InitializeAsync(AsyncPackage package, IntelliSenseSquiggleController controller, ProjectIndexService indexService)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null) _ = new FindReferencesCommand(package, controller, indexService, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            pendingSearch?.Cancel(); pendingSearch?.Dispose();
            pendingSearch = CancellationTokenSource.CreateLinkedTokenSource(package.DisposalToken);
            var token = pendingSearch.Token;
            package.JoinableTaskFactory.RunAsync(async () =>
            { try { await ExecuteAsync(token); } catch (OperationCanceledException) { } }).FileAndForget("CodeAssist/FindReferences");
        }

        private async Task ExecuteAsync(CancellationToken cancellation)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var selection = dte?.ActiveDocument?.Selection as EnvDTE.TextSelection;
            if (dte == null || selection == null)
            {
                ExecuteFallback(dte);
                return;
            }

            var editPoint = selection.ActivePoint.CreateEditPoint();
            editPoint.StartOfLine();
            var symbol = IdentifierParser.At(editPoint.GetText(editPoint.LineLength), selection.ActivePoint.LineCharOffset - 1);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                ExecuteFallback(dte);
                return;
            }

            controller?.Refresh();
            indexService?.Initialize(controller?.UnrealProjectPath);
            var activeFile = dte.ActiveDocument.FullName;
            var activeLine = selection.ActivePoint.Line;
            var activeColumn = selection.ActivePoint.LineCharOffset;
            var document = dte.ActiveDocument.Object("TextDocument") as EnvDTE.TextDocument;
            var activeText = document?.StartPoint.CreateEditPoint().GetText(document.EndPoint);
            var semanticRoot = !string.IsNullOrEmpty(controller?.UnrealProjectPath) ? controller.UnrealProjectPath :
                string.IsNullOrEmpty(dte.Solution?.FullName) ? null : Path.GetDirectoryName(dte.Solution.FullName);
            var compilerResults = await CompilerSemanticService.Instance.FindAsync("textDocument/references", semanticRoot, dte.FullName,
                activeFile, activeText, activeLine, activeColumn, cancellation);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (compilerResults != null)
            {
                await ReferenceResults.PublishAsync(package, symbol, compilerResults, true);
                var semanticOutput = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
                if (semanticOutput == null) return;
                var semanticGuid = OutputPaneGuid;
                semanticOutput.CreatePane(ref semanticGuid, "CodeAssist References", 1, 1); semanticOutput.GetPane(ref semanticGuid, out var semanticPane);
                semanticPane?.Clear(); semanticPane?.OutputStringThreadSafe($"CodeAssist: '{symbol}' compiler references {compilerResults.Length}\r\n");
                foreach (var item in compilerResults) semanticPane?.OutputStringThreadSafe($"{item.File}({item.Line},{item.Column}): {symbol}\r\n");
                semanticPane?.Activate(); return;
            }
            var results = await Task.Run(() => indexService?.Index.FindReferences(symbol, activeFile, activeLine, activeColumn, activeText, cancellation), cancellation);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            cancellation.ThrowIfCancellationRequested();
            if (results == null || results.Count == 0)
            {
                ExecuteFallback(dte);
                return;
            }

            await ReferenceResults.PublishAsync(package, symbol, results.Select(item =>
                new NavigationEntry(symbol, item.FilePath, item.Line, item.Column)).ToArray(), false);
            var output = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (output == null) return;
            var paneGuid = OutputPaneGuid;
            output.CreatePane(ref paneGuid, "CodeAssist References", 1, 1);
            output.GetPane(ref paneGuid, out var pane);
            pane?.Clear();
            pane?.OutputStringThreadSafe($"CodeAssist: '{symbol}' 참조 후보 {results.Count}개 (어휘·지역 블록 기준)\r\n");
            foreach (var result in results)
            {
                pane?.OutputStringThreadSafe($"{result.FilePath}({result.Line},{result.Column}): {symbol}\r\n");
            }
            pane?.Activate();
        }

        private static void ExecuteFallback(EnvDTE80.DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try { dte?.ExecuteCommand("Edit.FindAllReferences"); }
            catch (Exception) { }
        }
    }
}
