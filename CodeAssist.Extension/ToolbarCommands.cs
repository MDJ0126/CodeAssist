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
        private string spellCommand;private int filteredSteps;private DateTime lastFilteredStep;
        private EnvDTE.DebuggerEvents debuggerEvents;
        private ToolbarCommands(AsyncPackage package,EnvDTE80.DTE2 dte){this.package=package;this.dte=dte;}
        internal static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var service=await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            if(dte==null||service==null)throw new InvalidOperationException("Toolbar services are unavailable.");
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var owner=current=new ToolbarCommands(package,dte);
            StartupUi.SettingsApplied+=RefreshLabels;
            foreach(var menuId in new[]{0x1050,0x1052}) {var main=menuId==0x1052;var menu=new OleMenuCommand((s,e)=>{},new CommandID(GoToSymbolCommand.CommandSet,menuId));menu.BeforeQueryStatus+=(s,e)=>menu.Visible=DisplayQuickInfo.IntegratedMenuVisible(StartupUi.Preferences.IntegratedMainMenu,main);service.AddCommand(menu);}

            foreach(var candidate in new[]{"Edit.ToggleTextSpellChecker","Edit.ToggleSpellChecker","Edit.Advanced.ToggleSpellChecker"})
            { try {if(dte.Commands.Item(candidate)!=null){owner.spellCommand=candidate;break;}}catch(ArgumentException){}catch(COMException){} }
            owner.debuggerEvents=dte.Events.DebuggerEvents;
            owner.debuggerEvents.OnEnterDesignMode+=owner.OnDesignMode;
            owner.debuggerEvents.OnEnterRunMode+=owner.OnRunMode;
            owner.debuggerEvents.OnEnterBreakMode+=owner.OnBreakMode;
            WelcomeDocumentWindow.RecordEvent("ToolbarSpellCommand="+(owner.spellCommand??"Unavailable"));
            for(var id=0x150;id<=0x154;id++)
            {
                var captured=id;
                var command=new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(()=>owner.ExecuteAsync(captured)).FileAndForget("CodeAssist/Toolbar"),new CommandID(GoToSymbolCommand.CommandSet,id));
                command.BeforeQueryStatus+=(s,e)=>owner.Query(command,captured);
                service.AddCommand(command);
            }
        }
        internal static async System.Threading.Tasks.Task LocalizeMenusAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var service = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var localized = new Newtonsoft.Json.Linq.JArray();
            foreach (var item in UiCommandLabels.Sources)
            {
                var id = item.Key;
                if (!(service?.FindCommand(new CommandID(GoToSymbolCommand.CommandSet,id)) is OleMenuCommand command)) continue;
                void UpdateLabel(object sender, EventArgs args)
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    command.Text = UiCommandLabels.Get(id,StartupUi.Preferences.UiLanguage);
                }
                command.BeforeQueryStatus += UpdateLabel;
                UpdateLabel(null,EventArgs.Empty);
                localized.Add(new Newtonsoft.Json.Linq.JObject{["id"]=id,["text"]=command.Text});
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(StartupUi.StatePath),"menu-localization.json"),new Newtonsoft.Json.Linq.JObject{["version"]=ExtensionPackage.ProductVersion,["culture"]=System.Globalization.CultureInfo.CurrentUICulture.Name,["language"]=StartupUi.Preferences.UiLanguage,["commands"]=localized}.ToString());
        }
        internal static void RefreshLabels()
        {
            ThreadHelper.ThrowIfNotOnUIThread();if(current==null)return;
            var rows=new Newtonsoft.Json.Linq.JArray();
            try {
                var bars=current.dte.CommandBars as Microsoft.VisualStudio.CommandBars.CommandBars;
                if(bars==null)return;
                var names=new System.Collections.Generic.Dictionary<int,string>{
                    {0x141,"Settings"},{0x103,"OpenFile"},{0x104,"FindSymbol"},{0x102,"FindReferences"},
                    {0x150,"PreviousContext"},{0x151,"NextContext"},{0x117,"Snippet"},{0x152,"SpellCheck"},
                    {0x105,"CorrespondingFile"},{0x153,"SkipBreakpoints"},{0x154,"ThreadBreakpoints"},{0x140,"Welcome"}};
                foreach(Microsoft.VisualStudio.CommandBars.CommandBarControl control in bars["CodeAssist"].Controls)
                {
                    if(!names.TryGetValue(control.Id,out var key)) {
                        key=control.Tag as string;
                        if(!names.Values.Contains(key)) {
                            var caption=control.Caption.Replace("&","");
                            key=names.Values.FirstOrDefault(k=>
                                caption.StartsWith("CodeAssist: "+UiText.Get("Toolbar."+k,StartupUi.Preferences.UiLanguage),StringComparison.Ordinal) ||
                                UiCommandLabels.Sources.Values.Where(source=>source.StartsWith("CodeAssist: "+UiText.Get("Toolbar."+k,"en"),StringComparison.Ordinal))
                                    .Any(source=>caption.StartsWith(source,StringComparison.Ordinal) || caption.StartsWith(UiText.Get(source,StartupUi.Preferences.UiLanguage),StringComparison.Ordinal)));
                        }
                        if(key==null)continue;
                    }
                    control.Tag=key;
                    var label="CodeAssist: "+UiText.Get("Toolbar."+key,StartupUi.Preferences.UiLanguage);
                    control.Caption=label;control.TooltipText=label;
                    rows.Add(new Newtonsoft.Json.Linq.JObject{["id"]=control.Id,["caption"]=control.Caption,["tooltip"]=control.TooltipText});
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(StartupUi.StatePath),"toolbar-localization.json"),new Newtonsoft.Json.Linq.JObject{["version"]=ExtensionPackage.ProductVersion,["language"]=StartupUi.Preferences.UiLanguage,["culture"]=System.Globalization.CultureInfo.CurrentUICulture.Name,["controls"]=rows}.ToString());
            }catch(Exception error){WelcomeDocumentWindow.RecordFailure("ToolbarLocalization",error);}
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
        private async System.Threading.Tasks.Task ExecuteAsync(int id)
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
        private void OnBreakMode(EnvDTE.dbgEventReason reason,ref EnvDTE.dbgExecutionAction action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();if(!StartupUi.Preferences.StepFilterEnabled||reason.ToString().IndexOf("Step",StringComparison.OrdinalIgnoreCase)<0){filteredSteps=0;return;}
            if(DateTime.UtcNow-lastFilteredStep>TimeSpan.FromSeconds(3))filteredSteps=0;
            if(filteredSteps>=20)return;var function=dte.Debugger.CurrentStackFrame?.FunctionName;if(string.IsNullOrEmpty(function))return;
            foreach(var pattern in (StartupUi.Preferences.StepFilterPatterns??"").Split(new[]{';','\r','\n'},StringSplitOptions.RemoveEmptyEntries).Take(100))
            {if(pattern.Length>256)continue;var regex="^"+System.Text.RegularExpressions.Regex.Escape(pattern.Trim()).Replace("\\*",".*").Replace("\\?",".")+"$";if(!System.Text.RegularExpressions.Regex.IsMatch(function,regex,System.Text.RegularExpressions.RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20)))continue;filteredSteps++;lastFilteredStep=DateTime.UtcNow;action=EnvDTE.dbgExecutionAction.dbgExecutionActionStepOut;return;}
        }
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
            StartupUi.SettingsApplied-=RefreshLabels;
            if(current.debuggerEvents!=null){current.debuggerEvents.OnEnterDesignMode-=current.OnDesignMode;current.debuggerEvents.OnEnterRunMode-=current.OnRunMode;current.debuggerEvents.OnEnterBreakMode-=current.OnBreakMode;}
            try{current.threads.Restore(ReadFilter,WriteFilter);}
            catch(Exception error) when(error is COMException||error is AggregateException){WelcomeDocumentWindow.RecordFailure("RestoreThreadOverrides",error);}
            try{current.skipped.Restore(ReadEnabled,WriteEnabled);}
            catch(Exception error) when(error is COMException||error is AggregateException){WelcomeDocumentWindow.RecordFailure("RestoreBreakpointOverrides",error);}
            current=null;
        }
    }
}