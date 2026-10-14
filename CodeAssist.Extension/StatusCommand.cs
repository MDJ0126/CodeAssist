using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace CodeAssist.Extension
{
    internal sealed class StatusCommand
    {
        public const int CommandId = 0x0101;
        private readonly AsyncPackage package;
        private readonly IntelliSenseSquiggleController controller;
        private readonly ProjectIndexService indexService;

        private StatusCommand(AsyncPackage package, IntelliSenseSquiggleController controller, ProjectIndexService indexService, OleMenuCommandService commandService)
        {
            this.package = package;
            this.controller = controller;
            this.indexService = indexService;
            var commandId = new CommandID(GoToSymbolCommand.CommandSet, CommandId);
            commandService.AddCommand(new MenuCommand(Execute, commandId));
        }

        public static async Task InitializeAsync(AsyncPackage package, IntelliSenseSquiggleController controller, ProjectIndexService indexService)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null && controller != null)
            {
                _ = new StatusCommand(package, controller, indexService, commandService);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VsShellUtilities.ShowMessageBox(
                package,
                controller.CreateStatusText() + Environment.NewLine + Environment.NewLine +
                $"심볼 인덱스: {indexService?.Index.Count ?? 0:N0}개" + Environment.NewLine +
                $"인덱싱: {(indexService?.IsBuilding == true ? "진행 중" : "완료")}" + Environment.NewLine +
                $"컴파일 구성: {indexService?.CompilationModel?.Targets.Count ?? 0}개" + Environment.NewLine +
                $"최근 분석: {indexService?.LastBuild?.ElapsedMilliseconds ?? 0}ms / {(indexService?.LastBuild?.UsedWorker == true ? "별도 작업자" : "프로세스 내부")}" + Environment.NewLine +
                (indexService?.LastBuild?.WorkerFailure ?? "") + Environment.NewLine +
                "컴파일러 의미 분석: " + (CompilerSemanticService.Instance.LastError ?? "오류 기록 없음; 구성 파일이 있거나 명시 활성화한 경우 사용"),
                "CodeAssist Status",
                Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
