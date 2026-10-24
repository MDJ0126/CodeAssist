using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace CodeAssist.ParityRunner
{
    public sealed class Suite { public string runId; public string role; public string fixtureRoot; public Case[] cases; }
    public sealed class Case
    {
        public string id; public int feature; public string text; public int start; public int length;
        public string action; public string vaCommand; public int caCommand; public int settleMs=500; public string character;
        public string expectedText; public int? expectedCaret; public int? expectedSelectionLength; public bool verifyUndo; public bool setupParen;
        public string page; public string verificationScope; public bool filePicker; public Dictionary<string,bool> preferenceOverrides;
    }

    // Opt-in integration test executable INSIDE VS. Independent of the shipping extension.
    // Only generated, run-owned fixture documents are edited; no keyboard/mouse injection.
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("C/C++")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public sealed class Runner : IWpfTextViewCreationListener
    {
        [Import(typeof(SVsServiceProvider))] internal IServiceProvider Services=null;
        private static bool started;
        private static Microsoft.VisualStudio.Threading.JoinableTask runTask;
        private static readonly JavaScriptSerializer Json=new JavaScriptSerializer {MaxJsonLength=16*1024*1024};
        private static readonly HashSet<string> Commands=new HashSet<string>(StringComparer.Ordinal) {
            "VAssistX.ResetEditorZoom", "VAssistX.SmartSelectExtend", "VAssistX.SmartSelectShrink",
            "VAssistX.ScopeNext", "VAssistX.ScopePrevious"
        };
        private static readonly HashSet<int> CaCommands=new HashSet<int> {0x010f,0x0112,0x0113,0x010d,0x010e};
        public void TextViewCreated(IWpfTextView view)
        {
            var plan=Environment.GetEnvironmentVariable("CODEASSIST_PARITY_PLAN");
            if(started||string.IsNullOrEmpty(plan))return;
            started=true;
            runTask=ThreadHelper.JoinableTaskFactory.RunAsync(async()=>{
                await Task.Delay(1500);
                await RunAsync(plan);
            });
        }
        private static string Hash(string value)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        private static void Write(string file, object value)
        {
            var temporary=file+".tmp";
            File.WriteAllText(temporary,Json.Serialize(value));
            for(var attempt=0;;attempt++)
            {
                try {if(File.Exists(file))File.Replace(temporary,file,null);else File.Move(temporary,file);break;}
                catch(IOException){if(attempt>=59)throw;Thread.Sleep(10);}
            }
        }
        private static object Snapshot(IWpfTextView view, EnvDTE80.DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var span=view.Selection.StreamSelectionSpan.SnapshotSpan;
            return new {text=view.TextSnapshot.GetText(),caret=view.Caret.Position.BufferPosition.Position,
                selectionStart=span.Start.Position,selectionLength=span.Length,selectedText=span.GetText(),zoom=view.ZoomLevel,
                activeFile=dte.ActiveDocument==null?null:dte.ActiveDocument.FullName};
        }
        private async Task RunAsync(string plan)
        {
            // Do not create/delete paths supplied by an unvalidated plan.
            var role=Environment.GetEnvironmentVariable("CODEASSIST_PARITY_ROLE");
            var expectedRoot=Environment.GetEnvironmentVariable("CODEASSIST_PARITY_ROOT");
            if((role!="VA"&&role!="CA")||string.IsNullOrEmpty(expectedRoot))return;
            var root=Path.GetFullPath(expectedRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            plan=Path.GetFullPath(plan);
            if(!plan.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!File.Exists(Path.Combine(root,".codeassist-parity-owned")))return;
            var result=Path.Combine(root,role+".results.json");
            var rows=new List<object>();
            var watch=Stopwatch.StartNew();
            var suiteHash=Hash(File.ReadAllText(plan));
            var suite=Json.Deserialize<Suite>(File.ReadAllText(plan));
            var completedIds=new HashSet<string>(StringComparer.Ordinal);
            if(File.Exists(result))
            {
                var prior=Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(result));
                if(Convert.ToString(prior["planHash"])==suiteHash&&Convert.ToString(prior["role"])==role)
                {
                    foreach(var value in (System.Collections.IEnumerable)prior["cases"])
                    {var row=(Dictionary<string,object>)value;var priorError=row.ContainsKey("error")?Convert.ToString(row["error"]):"";if(priorError.Contains("Refusing to overwrite an existing fixture")||priorError.Contains("Owned document lost focus"))continue;if(!row.ContainsKey("processId"))row["processId"]=prior["processId"];rows.Add(row);completedIds.Add(Convert.ToString(row["id"]));}
                }
            }
            string fatal=null;string executing=null;
            Action<bool> publish=done=>Write(result,new {schema=1,runId=suite.runId,role,processId=Process.GetCurrentProcess().Id,
                planHash=suiteHash,startedAt=DateTimeOffset.Now.Subtract(watch.Elapsed).ToString("o"),elapsedMs=watch.ElapsedMilliseconds,
                completed=done,fatal,executing,cases=rows.ToArray(),scope="VS command-chain test; not physical shortcuts, pixel/UI or whole-product parity"});
            try
            {
                if(suite.role!=role||suite.cases==null||suite.cases.Length>500)throw new InvalidOperationException("Invalid role or suite size");
                var fixtureRoot=Path.GetFullPath(suite.fixtureRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                if(!fixtureRoot.StartsWith(root,StringComparison.OrdinalIgnoreCase)||fixtureRoot==root)throw new InvalidOperationException("Fixture root must be a run-owned child directory");
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte=Services.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                var model=Services.GetService(typeof(SComponentModel)) as IComponentModel;
                var manager=Services.GetService(typeof(SVsTextManager)) as IVsTextManager;
                if(dte==null||model==null||manager==null||dte.UndoContext.IsOpen)throw new InvalidOperationException("IDE services not ready");
                var adapters=model.GetService<IVsEditorAdaptersFactoryService>();
                var documents=model.GetService<ITextDocumentFactoryService>();
                var inventory=new List<object>();var nativeNames=new List<string>();
                foreach(EnvDTE.Command command in dte.Commands)
                {try{nativeNames.Add(command.Name);if(NativeCommandNames.IsVa(command.Name)||command.Guid.Equals("{f9d77864-fac8-457e-bd4f-552715151b6c}",StringComparison.OrdinalIgnoreCase))inventory.Add(new {command.Name,command.Guid,command.ID,command.IsAvailable});}catch{}}
                Write(Path.Combine(root,role+".commands.json"),inventory);
                publish(false);
                foreach(var item in suite.cases)
                {
                    if(completedIds.Contains(item.id))continue;
                    executing=item.id;publish(false);
                    if(File.Exists(Path.Combine(root,"STOP")))throw new OperationCanceledException("STOP requested");
                    var timer=Stopwatch.StartNew();EnvDTE.Document owned=null;object before=null,after=null,undo=null,probe=null;Action restorePreferences=()=>{};string error=null;int? hr=null;string resolvedVaCommand=null;
                    try
                    {
                        restorePreferences=ApplyPreferences(role,item.preferenceOverrides);
                        if(string.IsNullOrEmpty(item.id)||item.id.Any(c=>!char.IsLetterOrDigit(c)&&c!='-')||item.text==null||item.text.Length>128*1024)throw new InvalidOperationException("Invalid owned fixture");
                        var epoch=Path.Combine(fixtureRoot,"epoch-"+Process.GetCurrentProcess().Id);
                        Directory.CreateDirectory(epoch);
                        var file=Path.Combine(epoch,item.id+".cpp");
                        if(File.Exists(file))throw new InvalidOperationException("Refusing to overwrite an existing fixture");
                        File.WriteAllText(file,item.text);
                        owned=dte.ItemOperations.OpenFile(file,EnvDTE.Constants.vsViewKindTextView).Document;
                        await Task.Delay(150);await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        for(var focusAttempt=0;focusAttempt<3;focusAttempt++) {
                            owned.Activate();await Task.Delay(150);await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            if(dte.ActiveDocument!=null&&string.Equals(dte.ActiveDocument.FullName,file,StringComparison.OrdinalIgnoreCase))break;
                        }
                        if(dte.ActiveDocument==null||!string.Equals(dte.ActiveDocument.FullName,file,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Owned document lost focus before test; refusing command");
                        IVsTextView adapter;ErrorHandler.ThrowOnFailure(manager.GetActiveView(1,null,out adapter));
                        var view=adapters.GetWpfTextView(adapter);ITextDocument document;
                        if(view==null||!documents.TryGetTextDocument(view.TextBuffer,out document)||!string.Equals(document.FilePath,file,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Active document is not the owned fixture");
                        if(item.start<0||item.length<0||item.start+item.length>view.TextSnapshot.Length)throw new InvalidOperationException("Invalid selection");
                        view.Selection.Select(new SnapshotSpan(view.TextSnapshot,item.start,item.length),false);
                        view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot,item.start+item.length));
                        view.VisualElement.Focus();
                        view.ZoomLevel=100;
                        if(item.setupParen)
                        {
                            var setupTarget=adapter as Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
                            if(setupTarget==null)throw new InvalidOperationException("No setup command chain");
                            var setupGroup=VSConstants.VSStd2K;var setupInput=Marshal.AllocCoTaskMem(32);
                            try {Marshal.GetNativeVariantForObject((ushort)'(',setupInput);ErrorHandler.ThrowOnFailure(setupTarget.Exec(ref setupGroup,(uint)VSConstants.VSStd2KCmdID.TYPECHAR,0,setupInput,IntPtr.Zero));}
                            finally {Marshal.FreeCoTaskMem(setupInput);}
                            await Task.Delay(100);await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        }
                        if(item.action=="zoom")view.ZoomLevel=140;
                        before=Snapshot(view,dte);
                        await ScreenCheckpoint.Capture(root,role,item.id,"before",(IntPtr)dte.MainWindow.HWnd);await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        if(item.action=="dialog-probe")
                        {
                            if(role=="VA"&&!FullSuiteCommands.AllowedVa.Contains(item.vaCommand))throw new InvalidOperationException("VA probe command not allowlisted");
                            if(role=="CA"&&!FullSuiteCommands.AllowedCa.Contains(item.caCommand))throw new InvalidOperationException("CA probe command not allowlisted");
                            if(role=="VA")resolvedVaCommand=NativeCommandNames.Resolve(item.vaCommand,nativeNames);
                            // A separate MTA client avoids UI Automation calls into its own UI thread.
                            var driver=role=="VA"&&item.vaCommand=="VAssistX.Options" ? InProcessOptionsProbe.Run(root,role,item) : ProbeDialogAsync(root,role,item);
                            try
                            {
                                if(role=="VA")dte.ExecuteCommand(resolvedVaCommand);
                                else {object input=null,output=null;dte.Commands.Raise("{f9d77864-fac8-457e-bd4f-552715151b6c}",item.caCommand,ref input,ref output);}
                            }
                            catch(Exception invokeError){error=invokeError.ToString();}
                            probe=await driver;await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        }
                        else if(item.action=="command"||item.action=="zoom")
                        {
                            if(role=="VA") {if(!Commands.Contains(item.vaCommand))throw new InvalidOperationException("VA command not allowlisted");resolvedVaCommand=NativeCommandNames.Resolve(item.vaCommand,nativeNames);dte.ExecuteCommand(resolvedVaCommand);}
                            else {if(!CaCommands.Contains(item.caCommand))throw new InvalidOperationException("CA command not allowlisted");object input=null,output=null;dte.Commands.Raise("{f9d77864-fac8-457e-bd4f-552715151b6c}",item.caCommand,ref input,ref output);}
                        }
                        else
                        {
                            VSConstants.VSStd2KCmdID key;char? character=null;
                            if(item.action=="paren"){key=VSConstants.VSStd2KCmdID.TYPECHAR;character='(';}
                            else if(item.action=="typechar")
                            {
                                if(string.IsNullOrEmpty(item.character)||item.character.Length!=1||char.IsControl(item.character[0]))throw new InvalidOperationException("Expected one printable character");
                                key=VSConstants.VSStd2KCmdID.TYPECHAR;character=item.character[0];
                            }
                            else if(item.action=="backspace")key=VSConstants.VSStd2KCmdID.BACKSPACE;
                            else if(item.action=="return")key=VSConstants.VSStd2KCmdID.RETURN;
                            else if(item.action=="complete")key=VSConstants.VSStd2KCmdID.COMPLETEWORD;
                            else throw new InvalidOperationException("Unknown test action");
                            var target=adapter as Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
                            if(target==null)throw new InvalidOperationException("No editor command chain");
                            var group=VSConstants.VSStd2K;var input=IntPtr.Zero;
                            try {if(character.HasValue){input=Marshal.AllocCoTaskMem(32);Marshal.GetNativeVariantForObject((ushort)character.Value,input);}hr=target.Exec(ref group,(uint)key,0,input,IntPtr.Zero);ErrorHandler.ThrowOnFailure(hr.Value);}
                            finally {if(input!=IntPtr.Zero)Marshal.FreeCoTaskMem(input);}
                        }
                        await Task.Delay(Math.Max(50,Math.Min(item.settleMs,5000)));await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        after=Snapshot(view,dte);
                        await ScreenCheckpoint.Capture(root,role,item.id,"after",(IntPtr)dte.MainWindow.HWnd);await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        if(item.verifyUndo)
                        {
                            if(dte.ActiveDocument==null||!string.Equals(dte.ActiveDocument.FullName,file,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Refusing Undo in unrelated document");
                            dte.ExecuteCommand("Edit.Undo");
                            await Task.Delay(150);await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            undo=Snapshot(view,dte);
                        }
                    }
                    catch(Exception ex){error=ex.ToString();}
                    finally
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        restorePreferences();
                        try{owned?.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);}catch(Exception ex){error=(error??"")+" Cleanup: "+ex.Message;}
                    }
                    rows.Add(new {id=item.id,feature=item.feature,processId=Process.GetCurrentProcess().Id,inputHash=Hash(item.text??""),action=item.action,verificationScope=item.verificationScope,requestedVaCommand=item.vaCommand,resolvedVaCommand,before,after,undo,probe,error,hresult=hr,elapsedMs=timer.ElapsedMilliseconds});
                    publish(false);
                }
            }
            catch(Exception ex){fatal=ex.ToString();}
            executing=null;publish(true);
        }
        private static Action ApplyPreferences(string role,Dictionary<string,bool> overrides)
        {
            if(overrides==null||overrides.Count==0)return ()=>{};
            if(role!="CA")throw new InvalidOperationException("Preference override is CA-only test support");
            var allowed=new HashSet<string>{"ExtendCommentsEnabled","ConvertDotToArrow","RepairTypeCase","AcronymCompletionEnabled","ShorthandCompletionEnabled","PairCompletionEnabled","SmartSelectTrimWhitespace"};
            var assembly=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="CodeAssist.Extension");
            var target=assembly.GetType("CodeAssist.Extension.StartupUi").GetProperty("Preferences",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var saved=new Dictionary<System.Reflection.PropertyInfo,object>();
            foreach(var entry in overrides) {
                if(!allowed.Contains(entry.Key))throw new InvalidOperationException("Unsupported test preference");
                var property=target.GetType().GetProperty(entry.Key);saved.Add(property,property.GetValue(target));property.SetValue(target,entry.Value);
            }
            return ()=>{foreach(var entry in saved)entry.Key.SetValue(target,entry.Value);};
        }
        private static async Task<object> ProbeDialogAsync(string root,string role,Case item)
        {
            var file=Path.Combine(root,role+"-"+item.id+".dialog.json");
            var exe=Path.Combine(Path.GetDirectoryName(typeof(Runner).Assembly.Location),"CodeAssist.DialogProbeHost.exe");
            var requestedPage=role=="CA"&&item.page=="VA Hashtags"?"CodeAssist Hashtags":item.page;
            var page=Convert.ToBase64String(Encoding.UTF8.GetBytes(requestedPage??""));
            var args=Process.GetCurrentProcess().Id+" \""+page+"\" 5000 "+item.filePicker+" \""+file+"\"";
            using(var process=new Process {StartInfo=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true}})
            {
                process.Start();
                var exit=Task.Run(()=>process.WaitForExit(45000));
                if(!await exit){process.Kill();return new {status="probe-process-timeout"};}
                if(process.ExitCode!=0||!File.Exists(file))return new {status="probe-process-error",error=process.StandardError.ReadToEnd()};
                return Json.DeserializeObject(File.ReadAllText(file));
            }
        }
    }
}
