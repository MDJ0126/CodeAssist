using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeAssist.ParityRunner
{
    [PackageRegistration(UseManagedResourcesOnly=true,AllowsBackgroundLoading=true,RegisterUsing=RegistrationMethod.CodeBase)]
    [ProvideBindingPath]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists,PackageAutoLoadFlags.BackgroundLoad)]
    [Guid("CE561D06-F3DA-4FC3-827C-66396BC61BD1")]
    public sealed class BootstrapPackage : AsyncPackage
    {
        private Microsoft.VisualStudio.Threading.JoinableTask captureMonitor;
        protected override async Task InitializeAsync(CancellationToken cancellationToken,IProgress<ServiceProgressData> progress)
        {
            var root=Environment.GetEnvironmentVariable("CODEASSIST_PARITY_ROOT");
            var role=Environment.GetEnvironmentVariable("CODEASSIST_PARITY_ROLE");
            if(string.IsNullOrEmpty(root)||(role!="VA"&&role!="CA"))return;
            root=Path.GetFullPath(root);
            if(!File.Exists(Path.Combine(root,".codeassist-parity-owned")))return;
            try
            {
                captureMonitor = JoinableTaskFactory.RunAsync(async () => {
                    while (!DisposalToken.IsCancellationRequested) {
                        try { await BackgroundCapture.Serve(this,root,role); }
                        catch (Exception error) { try { File.AppendAllText(Path.Combine(root,role+".capture-service.log"),DateTimeOffset.Now.ToString("o")+" "+error+Environment.NewLine); } catch {} }
                        if (DisposalToken.IsCancellationRequested) break;
                        await Task.Delay(500);
                    }
                });
                File.WriteAllText(Path.Combine(root,role+".capture-service-build.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new{assembly=GetType().Assembly.Location,mvid=GetType().Assembly.ManifestModule.ModuleVersionId,pid=System.Diagnostics.Process.GetCurrentProcess().Id,at=DateTimeOffset.Now.ToString("o")}));
                File.WriteAllText(Path.Combine(root,role+".bootstrap.txt"),"Package loaded "+DateTimeOffset.Now.ToString("o"));
                var service=await GetServiceAsync(typeof(EnvDTE.DTE));
                await Task.Delay(1500,cancellationToken);
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var dte=service as EnvDTE80.DTE2;
                var fixture=Path.Combine(root,role,"Bootstrap.cpp");
                if(dte==null||!File.Exists(fixture))throw new InvalidOperationException("Missing DTE or owned bootstrap");
                dte.ItemOperations.OpenFile(fixture,EnvDTE.Constants.vsViewKindTextView);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(root,role+".bootstrap-error.txt"),error.ToString());}
        }
    }
}
