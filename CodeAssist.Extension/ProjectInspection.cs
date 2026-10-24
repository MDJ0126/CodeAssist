using CodeAssist.Core;
using CodeAssist.Indexing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace CodeAssist.Extension
{
    internal sealed class ProjectInspection : IDisposable
    {
        private readonly AsyncPackage package;
        private readonly ProjectIndexService index;
        private readonly ErrorListProvider errors;
        private CancellationTokenSource run;
        internal ProjectInspection(AsyncPackage package, ProjectIndexService index)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package=package;this.index=index;errors=new ErrorListProvider(package);
        }
        internal void Start()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            run?.Cancel();run?.Dispose();run=CancellationTokenSource.CreateLinkedTokenSource(package.DisposalToken);
            var token=run.Token;
            package.JoinableTaskFactory.RunAsync(async()=> {
                try
                {
                    await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                    var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                    if(dte == null) return;
                    var root=dte.Solution?.IsOpen==true ? Path.GetDirectoryName(dte.Solution.FullName) : index?.ProjectRoot;
                    if(string.IsNullOrEmpty(root)) return;
                    var findings=await System.Threading.Tasks.Task.Run(()=>ProjectSyntaxInspection.Scan(root,token),token);
                    await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                    errors.Tasks.Clear();
                    foreach(var finding in findings.Where(f=>StartupUi.Preferences.IncludeProjectPath(f.File)).Take(Math.Max(1,StartupUi.Preferences.InspectionResultLimit)))
                    {
                        var task=new ErrorTask { Document=finding.File,Line=finding.Line-1,Column=finding.Column-1,Text="CodeAssist: "+finding.Message,Category=TaskCategory.CodeSense,ErrorCategory=TaskErrorCategory.Warning };
                        task.Navigate+=(s,e)=> {
                            ThreadHelper.ThrowIfNotOnUIThread();
                            dte?.ItemOperations.OpenFile(finding.File,EnvDTE.Constants.vsViewKindTextView);
                            (dte?.ActiveDocument?.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(finding.Line,finding.Column,false);
                        };
                        errors.Tasks.Add(task);
                    }
                    if(StartupUi.Preferences.InspectionShowQuestions)errors.Show();
                    var status=await package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                    status?.SetText("CodeAssist 기본 구문 검사 완료: "+findings.Count+"개 항목");
                }
                catch(OperationCanceledException) { }
                catch(Exception ex) { ActivityLog.LogError("CodeAssist Inspection",ex.ToString()); }
            }).FileAndForget("CodeAssist/ProjectInspection");
        }
        internal void Stop()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            run?.Cancel();errors.Tasks.Clear();
        }
        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            run?.Cancel();run?.Dispose();errors.Dispose();
        }
    }
}