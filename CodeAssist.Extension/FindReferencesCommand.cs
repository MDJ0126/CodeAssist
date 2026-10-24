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
            commandService.AddCommand(new OleMenuCommand(Execute, new CommandID(GoToSymbolCommand.CommandSet, CommandId)));
        }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package, IntelliSenseSquiggleController controller, ProjectIndexService indexService)
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

        private async System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancellation)
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
            var searchAnchor = selection.ActivePoint.CreateEditPoint();
            var document = dte.ActiveDocument.Object("TextDocument") as EnvDTE.TextDocument;
            var activeText = document?.StartPoint.CreateEditPoint().GetText(document.EndPoint);
            var semanticRoot = !string.IsNullOrEmpty(controller?.UnrealProjectPath) ? controller.UnrealProjectPath :
                string.IsNullOrEmpty(dte.Solution?.FullName) ? null : Path.GetDirectoryName(dte.Solution.FullName);
            var queryContext=new ReferenceQueryContext(dte.Solution?.FullName,indexService?.ProjectRoot);
            void ValidateQueryContext()
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if(!queryContext.Matches(dte.Solution?.FullName,indexService?.ProjectRoot))
                    throw new OperationCanceledException(UiText.Get("Search project changed; find references again",StartupUi.Preferences.UiLanguage));
            }
            void ValidateInitialQuery()
            {
                cancellation.ThrowIfCancellationRequested();ValidateQueryContext();
            }
            async Task<NavigationEntry[]> RefreshQueryAsync(bool compiler)
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                ValidateQueryContext();
                var open=dte.Documents.Cast<EnvDTE.Document>().FirstOrDefault(item=>string.Equals(item.FullName,activeFile,StringComparison.OrdinalIgnoreCase));
                var current=open?.Object("TextDocument") as EnvDTE.TextDocument;
                var text=current==null?File.ReadAllText(activeFile):current.StartPoint.CreateEditPoint().GetText(current.EndPoint);
                var queryLine=searchAnchor.Line;var queryColumn=searchAnchor.LineCharOffset;
                var lines=text.Replace("\r\n","\n").Split('\n');
                if(queryLine<1||queryLine>lines.Length||IdentifierParser.At(lines[queryLine-1],queryColumn-1)!=symbol)
                    throw new InvalidOperationException(UiText.Get("Search location changed; find references again",StartupUi.Preferences.UiLanguage));
                if(compiler)
                {
                    var compilerRows=await CompilerSemanticService.Instance.FindAsync("textDocument/references",semanticRoot,dte.FullName,activeFile,text,queryLine,queryColumn,package.DisposalToken);
                    await package.JoinableTaskFactory.SwitchToMainThreadAsync();ValidateQueryContext();return compilerRows;
                }
                var found=await System.Threading.Tasks.Task.Run(()=>indexService?.Index.FindReferences(symbol,activeFile,queryLine,queryColumn,text,package.DisposalToken),package.DisposalToken);
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();ValidateQueryContext();
                return found?.Select(item=>new NavigationEntry(symbol,item.FilePath,item.Line,item.Column)).ToArray();
            }
            var compilerResults = await CompilerSemanticService.Instance.FindAsync("textDocument/references", semanticRoot, dte.FullName,
                activeFile, activeText, activeLine, activeColumn, cancellation);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            cancellation.ThrowIfCancellationRequested();ValidateQueryContext();
            if (compilerResults != null)
            {
                await ReferenceResults.PublishAsync(package, symbol, compilerResults, true,()=>RefreshQueryAsync(true),ValidateInitialQuery);
                var semanticOutput = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
                ValidateInitialQuery();
                if (semanticOutput == null) return;
                var semanticGuid = OutputPaneGuid;
                semanticOutput.CreatePane(ref semanticGuid, ReferenceResultText.WindowTitle(StartupUi.Preferences.UiLanguage), 1, 1); semanticOutput.GetPane(ref semanticGuid, out var semanticPane);
                semanticPane?.Clear(); semanticPane?.OutputStringThreadSafe(ReferenceResultText.Summary(symbol,compilerResults.Length,true,StartupUi.Preferences.UiLanguage)+"\r\n");
                foreach (var item in compilerResults) semanticPane?.OutputStringThreadSafe($"{item.File}({item.Line},{item.Column}): {symbol}\r\n");
                semanticPane?.Activate(); return;
            }
            var results = await System.Threading.Tasks.Task.Run(() => indexService?.Index.FindReferences(symbol, activeFile, activeLine, activeColumn, activeText, cancellation), cancellation);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            cancellation.ThrowIfCancellationRequested();
            ValidateQueryContext();
            if (results == null || results.Count == 0)
            {
                ExecuteFallback(dte);
                return;
            }

            await ReferenceResults.PublishAsync(package, symbol, results.Select(item =>
                new NavigationEntry(symbol, item.FilePath, item.Line, item.Column)).ToArray(), false,()=>RefreshQueryAsync(false),ValidateInitialQuery);
            var output = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            ValidateInitialQuery();
            if (output == null) return;
            var paneGuid = OutputPaneGuid;
            output.CreatePane(ref paneGuid, ReferenceResultText.WindowTitle(StartupUi.Preferences.UiLanguage), 1, 1);
            output.GetPane(ref paneGuid, out var pane);
            pane?.Clear();
            pane?.OutputStringThreadSafe(ReferenceResultText.Summary(symbol,results.Count,false,StartupUi.Preferences.UiLanguage)+"\r\n");
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
