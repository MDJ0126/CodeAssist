using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeAssist.Core;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
namespace CodeAssist.Extension
{
    internal sealed class ToolbarCommands
    {
        private static ToolbarCommands current;
        private readonly AsyncPackage package;
        private readonly EnvDTE80.DTE2 dte;
        private readonly ReversibleOverrides<EnvDTE.Breakpoint,bool> skipped=new ReversibleOverrides<EnvDTE.Breakpoint,bool>();
        private readonly ReversibleOverrides<EnvDTE80.Breakpoint2,string> threads=new ReversibleOverrides<EnvDTE80.Breakpoint2,string>();
        private string spellCommand;
        private EnvDTE.DebuggerEvents debuggerEvents;
        private ToolbarCommands(AsyncPackage package,EnvDTE80.DTE2 dte){this.package=package;this.dte=dte;}
        internal static async Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var service=await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            if(dte==null||service==null)throw new InvalidOperationException("Toolbar services are unavailable.");
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var owner=current=new ToolbarCommands(package,dte);
            foreach(var candidate in new[]{"Edit.ToggleTextSpellChecker","Edit.ToggleSpellChecker","Edit.Advanced.ToggleSpellChecker"})
            { try {if(dte.Commands.Item(candidate)!=null){owner.spellCommand=candidate;break;}}catch(ArgumentException){}catch(COMException){} }
            owner.debuggerEvents=dte.Events.DebuggerEvents;
            owner.debuggerEvents.OnEnterDesignMode+=owner.OnDesignMode;
            owner.debuggerEvents.OnEnterRunMode+=owner.OnRunMode;
            WelcomeDocumentWindow.RecordEvent("ToolbarSpellCommand="+(owner.spellCommand??"Unavailable"));
            for(var id=0x150;id<=0x154;id++)
            {
                var captured=id;
                var command=new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(()=>owner.ExecuteAsync(captured)).FileAndForget("CodeAssist/Toolbar"),new CommandID(GoToSymbolCommand.CommandSet,id));
                command.BeforeQueryStatus+=(s,e)=>owner.Query(command,captured);
                service.AddCommand(command);
            }
        }
        private void Query(OleMenuCommand command,int id)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try {
                command.Checked=id==0x153?skipped.Active:id==0x154&&threads.Active;
                command.Enabled=id==0x152?spellCommand!=null && dte.Commands.Item(spellCommand).IsAvailable:
                    id==0x154?threads.Active||dte.Debugger.CurrentMode==EnvDTE.dbgDebugMode.dbgBreakMode&&dte.Debugger.CurrentThread!=null&&dte.Debugger.Breakpoints.Count>0:
                    id==0x153?skipped.Active||dte.Debugger.Breakpoints.Count>0:dte.ActiveDocument?.Selection is EnvDTE.TextSelection;
            } catch(COMException){command.Enabled=false;}
        }
        private async Task ExecuteAsync(int id)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            try {
                if(id==0x152){if(spellCommand!=null&&dte.Commands.Item(spellCommand).IsAvailable)dte.ExecuteCommand(spellCommand);return;}
                if(id==0x153)
                {
                    if(skipped.Active)skipped.Restore(ReadEnabled,WriteEnabled);
                    else skipped.Apply(dte.Debugger.Breakpoints.Cast<EnvDTE.Breakpoint>().ToArray(),ReadEnabled,WriteEnabled,_=>false);
                    WelcomeDocumentWindow.RecordEvent("SkipBreakpoints="+skipped.Active);return;
                }
                if(id==0x154)
                {
                    if(threads.Active)threads.Restore(ReadFilter,WriteFilter);
                    else {
                        if(dte.Debugger.CurrentMode!=EnvDTE.dbgDebugMode.dbgBreakMode||dte.Debugger.CurrentThread==null)return;
                        var thread=dte.Debugger.CurrentThread.ID.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        threads.Apply(dte.Debugger.Breakpoints.Cast<EnvDTE.Breakpoint>().OfType<EnvDTE80.Breakpoint2>().ToArray(),ReadFilter,WriteFilter,
                            filter=>string.IsNullOrWhiteSpace(filter)?"ThreadId = "+thread:"("+filter+") & (ThreadId = "+thread+")");
                    }
                    WelcomeDocumentWindow.RecordEvent("ThreadBreakpoints="+threads.Active);return;
                }
                var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                var components=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if(manager==null||manager.GetActiveView(1,null,out var adapter)<0)return;
                var view=components?.GetService<IVsEditorAdaptersFactoryService>()?.GetWpfTextView(adapter);
                if(view==null||view.IsClosed)return;
                var snapshot=view.TextSnapshot;var caret=view.Caret.Position.BufferPosition.Position;
                var target=ContextReferenceNavigation.Move(snapshot.GetText(),caret,id==0x151);
                if(target==null)return;
                var span=new SnapshotSpan(snapshot,target.Start,target.Length);
                view.Selection.Select(span,false);view.Caret.MoveTo(span.Start);view.ViewScroller.EnsureSpanVisible(span);
            }
            catch(Exception error) when(error is COMException||error is ArgumentException||error is AggregateException)
            {WelcomeDocumentWindow.RecordFailure("ToolbarCommand "+id.ToString("X"),error);VsShellUtilities.ShowMessageBox(package,error.Message,"CodeAssist",OLEMSGICON.OLEMSGICON_WARNING,OLEMSGBUTTON.OLEMSGBUTTON_OK,OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);}
        }
        private static bool ReadEnabled(EnvDTE.Breakpoint b){ThreadHelper.ThrowIfNotOnUIThread();return b.Enabled;}
        private static void WriteEnabled(EnvDTE.Breakpoint b,bool value){ThreadHelper.ThrowIfNotOnUIThread();b.Enabled=value;}
        private static string ReadFilter(EnvDTE80.Breakpoint2 b){ThreadHelper.ThrowIfNotOnUIThread();return b.FilterBy;}
        private static void WriteFilter(EnvDTE80.Breakpoint2 b,string value){ThreadHelper.ThrowIfNotOnUIThread();b.FilterBy=value;}
        private void OnDesignMode(EnvDTE.dbgEventReason reason)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try{threads.Restore(ReadFilter,WriteFilter);}catch(AggregateException error){WelcomeDocumentWindow.RecordFailure("RestoreThreadFilters",error);}
        }
        private void OnRunMode(EnvDTE.dbgEventReason reason)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(!skipped.Active)return;
            try{skipped.Apply(dte.Debugger.Breakpoints.Cast<EnvDTE.Breakpoint>().ToArray(),ReadEnabled,WriteEnabled,_=>false);}
            catch(Exception error) when(error is COMException||error is AggregateException){WelcomeDocumentWindow.RecordFailure("SkipNewBreakpoints",error);}
        }
        internal static void Shutdown()
        {
            ThreadHelper.ThrowIfNotOnUIThread();if(current==null)return;
            if(current.debuggerEvents!=null){current.debuggerEvents.OnEnterDesignMode-=current.OnDesignMode;current.debuggerEvents.OnEnterRunMode-=current.OnRunMode;}
            try{current.threads.Restore(ReadFilter,WriteFilter);}
            catch(Exception error) when(error is COMException||error is AggregateException){WelcomeDocumentWindow.RecordFailure("RestoreThreadOverrides",error);}
            try{current.skipped.Restore(ReadEnabled,WriteEnabled);}
            catch(Exception error) when(error is COMException||error is AggregateException){WelcomeDocumentWindow.RecordFailure("RestoreBreakpointOverrides",error);}
            current=null;
        }
    }
}