using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeAssist.Core;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
namespace CodeAssist.Extension
{
    // Opt-in development verification. Never edits an existing user document.
    internal static class EditorIntegrationChecks
    {
        private static bool running;
        internal static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var service=await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var command=new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(()=>RunAsync(package)).FileAndForget("CodeAssist/EditorChecks"),new CommandID(GoToSymbolCommand.CommandSet,0x160));
            command.BeforeQueryStatus+=(s,e)=>{command.Visible=Environment.GetEnvironmentVariable("CODEASSIST_EDITOR_CHECKS")=="1";command.Enabled=!running;};
            command.Visible=Environment.GetEnvironmentVariable("CODEASSIST_EDITOR_CHECKS")=="1";
            service?.AddCommand(command);
        }
        private static async System.Threading.Tasks.Task RunAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if(running||Environment.GetEnvironmentVariable("CODEASSIST_EDITOR_CHECKS")!="1")return;
            running=true;
            var cases=new JArray(); var p=StartupUi.Preferences;
            var oldPair=p.PairCompletionEnabled;var oldSurround=p.SurroundSelectionEnabled;var oldParen=p.SurroundParentheses;var oldSlash=p.SurroundSlash;var oldComment=p.ExtendCommentsEnabled;var oldSpace=p.CommentSpaceEnabled;var oldPlacement=p.ClosingBracePlacement;
            EnvDTE.Document ownedDocument=null;
            var folder=Path.Combine(Path.GetTempPath(),"CodeAssistEditorChecks",Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);var file=Path.Combine(folder,"EditorChecks.cpp");
            var report=new JObject { ["version"]=ExtensionPackage.ProductVersion,["mvid"]=typeof(EditorIntegrationChecks).Module.ModuleVersionId.ToString(),["fixture"]=file,["cases"]=cases,["scope"]="Actual VS editor buffer, command chain, Undo history. Not VA parity." };
            try
            {
                var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                if(dte==null||dte.UndoContext.IsOpen)throw new InvalidOperationException("IDE is not ready for an isolated editor test");
                File.WriteAllText(file,"void EditorChecks() {}\r\n");
                ownedDocument=dte.ItemOperations.OpenFile(file,EnvDTE.Constants.vsViewKindTextView).Document;
                await System.Threading.Tasks.Task.Yield();
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                var components=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                if(components==null)throw new InvalidOperationException("Editor components are unavailable");
                var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                if(manager==null||manager.GetActiveView(1,null,out var adapter)<0)throw new InvalidOperationException("No test view");
                var view=components?.GetService<IVsEditorAdaptersFactoryService>()?.GetWpfTextView(adapter);
                if(view==null||!(adapter is Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget target))throw new InvalidOperationException("No test command target");
                var documents=components.GetService<ITextDocumentFactoryService>();
                if(!documents.TryGetTextDocument(view.TextBuffer,out var document)||!string.Equals(document.FilePath,file,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Refusing to edit an unrelated document");
                var histories=components.GetService<ITextUndoHistoryRegistry>();
                if(!histories.TryGetHistory(view.TextBuffer,out var history))throw new InvalidOperationException("No editor Undo history");
                p.PairCompletionEnabled=true;p.SurroundSelectionEnabled=true;p.SurroundParentheses=true;p.SurroundSlash=true;p.ExtendCommentsEnabled=true;p.CommentSpaceEnabled=true;
                view.Options.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId,true);view.Options.SetOptionValue(DefaultOptions.IndentSizeOptionId,4);
                void Reset(string text,int start,int length=0)
                {
                    if(view.IsClosed)throw new InvalidOperationException("Test view closed");
                    view.TextBuffer.Replace(new Span(0,view.TextSnapshot.Length),text);
                    view.Selection.Select(new SnapshotSpan(view.TextSnapshot,start,length),false);view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot,start+length));
                }
                void Key(VSConstants.VSStd2KCmdID id,char? character=null)
                {
                    var group=VSConstants.VSStd2K;var input=IntPtr.Zero;
                    try { if(character.HasValue){input=Marshal.AllocCoTaskMem(32);Marshal.GetNativeVariantForObject((ushort)character.Value,input);}
                        ErrorHandler.ThrowOnFailure(target.Exec(ref group,(uint)id,0,input,IntPtr.Zero)); }
                    finally { if(input!=IntPtr.Zero)Marshal.FreeCoTaskMem(input); }
                }
                void Check(string name,Func<bool> condition)
                {
                    bool passed=false;string error=null;try{passed=condition();}catch(Exception ex){error=ex.ToString();}
                    cases.Add(new JObject {["name"]=name,["passed"]=passed,["text"]=view.TextSnapshot.GetText(),["caret"]=view.Caret.Position.BufferPosition.Position,["selectionStart"]=view.Selection.StreamSelectionSpan.SnapshotSpan.Start.Position,["selectionLength"]=view.Selection.StreamSelectionSpan.SnapshotSpan.Length,["error"]=error});
                }
                Reset("",0);Key(VSConstants.VSStd2KCmdID.TYPECHAR,'(');
                Check("Pair insert and caret",()=>view.TextSnapshot.GetText()=="()"&&view.Caret.Position.BufferPosition.Position==1);
                Key(VSConstants.VSStd2KCmdID.BACKSPACE);Check("Empty pair delete",()=>view.TextSnapshot.Length==0);
                history.Undo(1);Check("Pair deletion Undo restores caret",()=>view.TextSnapshot.GetText()=="()"&&view.Caret.Position.BufferPosition.Position==1);
                history.Redo(1);Check("Pair deletion Redo",()=>view.TextSnapshot.Length==0&&view.Caret.Position.BufferPosition.Position==0);
                Reset("int value;",4,5);Key(VSConstants.VSStd2KCmdID.TYPECHAR,'(');
                Check("Surround selected word",()=>view.TextSnapshot.GetText()=="int (value);"&&view.Selection.StreamSelectionSpan.SnapshotSpan.Length==7);
                history.Undo(1);Check("Surround Undo restores selection",()=>view.TextSnapshot.GetText()=="int value;"&&view.Selection.StreamSelectionSpan.SnapshotSpan.Start.Position==4&&view.Selection.StreamSelectionSpan.SnapshotSpan.Length==5);
                history.Redo(1);Check("Surround Redo restores selection",()=>view.TextSnapshot.GetText()=="int (value);"&&view.Selection.StreamSelectionSpan.SnapshotSpan.Length==7);
                Reset("    int value;",0,14);Key(VSConstants.VSStd2KCmdID.TYPECHAR,'/');
                Check("Slash comments whole selected line",()=>view.TextSnapshot.GetText()=="    // int value;");
                history.Undo(1);Check("Comment Undo restores selection",()=>view.TextSnapshot.GetText()=="    int value;"&&view.Selection.StreamSelectionSpan.SnapshotSpan.Length==14);
                Reset("// note",7);Key(VSConstants.VSStd2KCmdID.RETURN);
                Check("Comment continuation",()=>view.TextSnapshot.GetText().Replace("\r\n","\n")=="// note\n// ");
                history.Undo(1);Check("Comment continuation one Undo",()=>view.TextSnapshot.GetText()=="// note"&&view.Caret.Position.BufferPosition.Position==7);
                foreach(var placement in new[]{ClosingBracePlacement.SameLine,ClosingBracePlacement.NextLine,ClosingBracePlacement.BlankLine})
                {
                    p.ClosingBracePlacement=placement;Reset("",0);Key(VSConstants.VSStd2KCmdID.TYPECHAR,'{');
                    var expected=placement==ClosingBracePlacement.SameLine?"{}":placement==ClosingBracePlacement.NextLine?"{\n}":"{\n    \n}";
                    Check("Brace placement "+placement,()=>view.TextSnapshot.GetText().Replace("\r\n","\n")==expected);
                }
            }
            catch(Exception error){report["error"]=error.ToString();}
            finally
            {
                p.PairCompletionEnabled=oldPair;p.SurroundSelectionEnabled=oldSurround;p.SurroundParentheses=oldParen;p.SurroundSlash=oldSlash;p.ExtendCommentsEnabled=oldComment;p.CommentSpaceEnabled=oldSpace;p.ClosingBracePlacement=oldPlacement;
                try{ownedDocument?.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);}catch(Exception error){report["cleanupError"]=error.Message;}
                report["completedUtc"]=DateTime.UtcNow.ToString("O");
                var output=Path.Combine(Path.GetDirectoryName(StartupUi.StatePath),"editor-integration-checks.json");File.WriteAllText(output,report.ToString());
                WelcomeDocumentWindow.RecordEvent("EditorIntegrationChecks="+output);running=false;
            }
        }
    }
}
