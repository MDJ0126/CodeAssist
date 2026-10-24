using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using CodeAssist.Core;

namespace CodeAssist.Extension
{
    internal static class EditorInputTrace { internal static readonly bool Enabled=Environment.GetEnvironmentVariable("CODEASSIST_TRACE_EDITOR")=="1"; }

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("C/C++")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class CompletionCommandFilterProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService Adapters = null;

        [Import]
        internal ICompletionBroker CompletionBroker = null;

        [Import]
        internal ITextUndoHistoryRegistry UndoHistories = null;

        [Import] internal ProjectIndexService Indexes=null;
        [Import] internal IEditorOperationsFactoryService EditorOperations = null;

        public void VsTextViewCreated(Microsoft.VisualStudio.TextManager.Interop.IVsTextView textViewAdapter)
        {
            var view = Adapters.GetWpfTextView(textViewAdapter);
            if (view == null) return;
            var filter = new CompletionCommandFilter(view, CompletionBroker, UndoHistories, EditorOperations.GetEditorOperations(view),Indexes);
            var attached=textViewAdapter.AddCommandFilter(filter, out var next);
            if(EditorInputTrace.Enabled)WelcomeDocumentWindow.RecordEvent("EditorFilterAttached="+attached+" content="+view.TextBuffer.ContentType.TypeName);
            filter.Next = next;
        }
    }

    internal sealed class CompletionCommandFilter : IOleCommandTarget
    {
        private readonly ProjectIndexService indexes;
        private long shiftStarted;private int shiftCaret=-1;private bool repeatingKey;
        private readonly IWpfTextView view;
        private readonly ICompletionBroker broker;
        private readonly ITextUndoHistoryRegistry undoHistories;
        private readonly IEditorOperations editorOperations;
        private readonly SynchronizationContext uiContext;
        private bool liveCompletionQueued;
        private sealed class OwnedPair { internal ITrackingPoint Open,Close;internal char Opening,Closing; }
        private readonly List<OwnedPair> pairs=new List<OwnedPair>();
        private int tracedInputs;

        public CompletionCommandFilter(IWpfTextView view, ICompletionBroker broker, ITextUndoHistoryRegistry undoHistories, IEditorOperations editorOperations, ProjectIndexService indexes=null)
        {
            this.view = view;this.indexes=indexes;
            view.VisualElement.PreviewKeyDown+=OnKeyDown;view.VisualElement.PreviewKeyUp+=OnKeyUp;view.Closed+=OnClosed;
            this.broker = broker;
            this.undoHistories = undoHistories;
            this.editorOperations = editorOperations;
            uiContext = SynchronizationContext.Current;
        }

        public IOleCommandTarget Next { get; set; }

        public int QueryStatus(ref Guid commandGroup, uint commandCount, OLECMD[] commands, IntPtr commandText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (view.IsClosed) return Next.QueryStatus(ref commandGroup, commandCount, commands, commandText);
            if (commandGroup == VSConstants.GUID_VSStandardCommandSet97 && WordHighlights.For(view).Active &&
                commands.Take((int)commandCount).All(command => command.cmdID == (uint)VSConstants.VSStd97CmdID.FindNext || command.cmdID == (uint)VSConstants.VSStd97CmdID.FindPrev))
            {
                for (var i = 0; i < commandCount; i++) commands[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                return VSConstants.S_OK;
            }
            return Next.QueryStatus(ref commandGroup, commandCount, commands, commandText);
        }

        public int Exec(ref Guid commandGroup, uint commandId, uint commandOptions, IntPtr input, IntPtr output)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (view.IsClosed) return Next.Exec(ref commandGroup, commandId, commandOptions, input, output);
            if(commandGroup==VSConstants.VSStd2K&&commandId==(uint)VSConstants.VSStd2KCmdID.BACKSPACE&&TryRemoveEmptyPair())return VSConstants.S_OK;
            if (commandGroup == VSConstants.GUID_VSStandardCommandSet97 &&
                (commandId == (uint)VSConstants.VSStd97CmdID.FindNext || commandId == (uint)VSConstants.VSStd97CmdID.FindPrev) &&
                WordHighlights.For(view).Navigate(commandId == (uint)VSConstants.VSStd97CmdID.FindNext)) return VSConstants.S_OK;
            if (commandGroup == VSConstants.VSStd2K && commandId == (uint)VSConstants.VSStd2KCmdID.CANCEL && WordHighlights.For(view).Active)
                WordHighlights.For(view).Clear();
            string copiedText = null;
            if (!view.IsClosed && commandGroup == VSConstants.GUID_VSStandardCommandSet97 &&
                (commandId == (uint)VSConstants.VSStd97CmdID.Copy || commandId == (uint)VSConstants.VSStd97CmdID.Cut) &&
                !view.Selection.IsEmpty && view.Selection.SelectedSpans.Count == 1 && view.Selection.SelectedSpans[0].Length <= 1024 * 1024)
                copiedText = view.Selection.SelectedSpans[0].GetText();
            if(commandGroup==VSConstants.VSStd2K&&commandId==(uint)VSConstants.VSStd2KCmdID.TAB&&!broker.IsCompletionActive(view)&&SnippetInsertion.TryShortcut(view))return VSConstants.S_OK;
            if (IsCommitCommand(commandGroup, commandId) && TryCommitCodeAssistSelection())
            {
                return VSConstants.S_OK;
            }

            var hasTypedCharacter = TryGetTypedCharacter(commandGroup, commandId, input, out var typedCharacter);
            if(EditorInputTrace.Enabled && tracedInputs++<24)WelcomeDocumentWindow.RecordEvent("EditorInput group="+commandGroup+" id="+commandId+" typed="+(hasTypedCharacter?(int)typedCharacter:-1)+" selection="+view.Selection.StreamSelectionSpan.SnapshotSpan.Length+" surround="+StartupUi.Preferences.SurroundSelectionEnabled);
            if(hasTypedCharacter&&view.Selection.IsEmpty&&TryAutomaticInput(typedCharacter))return VSConstants.S_OK;
            if (hasTypedCharacter && TrySurroundSelection(typedCharacter)) return VSConstants.S_OK;
            if(hasTypedCharacter && TrySkipClosing(typedCharacter))return VSConstants.S_OK;
            if(hasTypedCharacter && StartupUi.Preferences.PairCompletionEnabled && CppPairing.Opening(typedCharacter))
            {
                var selected=view.Selection.StreamSelectionSpan.SnapshotSpan;
                var text=view.TextSnapshot.GetText();
                if(text.Length<=2*1024*1024&&CppPairing.CanOpen(text.Remove(selected.Start,selected.Length),selected.Start,typedCharacter))
                    return TypeOpening(ref commandGroup,commandId,commandOptions,input,output,typedCharacter);
            }
            if (commandGroup==VSConstants.VSStd2K && commandId==(uint)VSConstants.VSStd2KCmdID.RETURN &&
                StartupUi.Preferences.ExtendCommentsEnabled && view.Selection.IsEmpty && !broker.IsCompletionActive(view))
            {
                var prefix=CppEditing.CommentContinuation(view.TextSnapshot.GetText(),view.Caret.Position.BufferPosition.Position);
                if(prefix!=null)return ContinueComment(ref commandGroup,commandId,commandOptions,input,output,prefix);
            }
            var pasteSpan=StartupUi.Preferences.FormatAfterPasteEnabled && commandGroup==VSConstants.GUID_VSStandardCommandSet97 && commandId==(uint)VSConstants.VSStd97CmdID.Paste && view.Selection.SelectedSpans.Count==1
                ?view.Selection.StreamSelectionSpan.SnapshotSpan.Snapshot.CreateTrackingSpan(view.Selection.StreamSelectionSpan.SnapshotSpan.Span,SpanTrackingMode.EdgeInclusive):null;
            var isDeletion = IsDeletion(commandGroup, commandId);
            var isExplicitCompletion = IsExplicitCompletion(commandGroup, commandId);
            // Handle our explicit list before VC++ can expand the word on its own.
            // Otherwise COMPLETEWORD edits the buffer, then our fallback opens a
            // second list for the already expanded name outside the commit Undo.
            if (isExplicitCompletion && StartupUi.Preferences.CompletionEnabled)
            {
                if (!broker.IsCompletionActive(view)) broker.TriggerCompletion(view);
                var ownSet = broker.GetSessions(view).Select(session => session.SelectedCompletionSet)
                    .FirstOrDefault(set => string.Equals(set?.Moniker, "CodeAssistPreview", StringComparison.Ordinal));
                if (ownSet != null)
                {
                    if (commandId == (uint)VSConstants.VSStd2KCmdID.COMPLETEWORD && ownSet.Completions.Count == 1 && ownSet.ApplicableTo.GetSpan(view.TextSnapshot).Length > 0)
                        TryCommitCodeAssistSelection();
                    return VSConstants.S_OK;
                }
            }
            var shouldTrigger = hasTypedCharacter && ShouldTrigger(typedCharacter);
            var isNumericPunctuation = hasTypedCharacter && IsNumericLiteralPunctuation(typedCharacter);
            if (isNumericPunctuation)
            {
                // A native C++ completion session treats '.' as a commit character.
                // Close every session before "50." can commit an unrelated symbol.
                DismissAllCompletionSessions();
            }
            // A standalone preview must never consume punctuation or whitespace as a
            // completion commit. Dismiss it before Visual Studio processes the key.
            if (hasTypedCharacter && ShouldDismissPreviewBeforeInput(typedCharacter))
            {
                DismissPreviewSessions();
            }
            var result = Next.Exec(ref commandGroup, commandId, commandOptions, input, output);
            if(ErrorHandler.Succeeded(result)&&StartupUi.Preferences.DoubleKeyRepeat&&repeatingKey&&System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None&&commandGroup==VSConstants.VSStd2K&&
                ((hasTypedCharacter&&(char.IsLetterOrDigit(typedCharacter)||typedCharacter==' '))||commandId==(uint)VSConstants.VSStd2KCmdID.BACKSPACE||commandId==(uint)VSConstants.VSStd2KCmdID.DELETE||commandId==(uint)VSConstants.VSStd2KCmdID.LEFT||commandId==(uint)VSConstants.VSStd2KCmdID.RIGHT||commandId==(uint)VSConstants.VSStd2KCmdID.UP||commandId==(uint)VSConstants.VSStd2KCmdID.DOWN))
                Next.Exec(ref commandGroup,commandId,commandOptions,input,output);
            if(ErrorHandler.Succeeded(result) && pasteSpan!=null) FormatPastedLines(pasteSpan);
            if (ErrorHandler.Succeeded(result) && copiedText != null) EditorClipboardHistory.History.Record(copiedText);
            if (ErrorHandler.Succeeded(result) && (hasTypedCharacter || isDeletion))
            {
                RefreshCodeAssistSessions();
            }
            if (ErrorHandler.Succeeded(result) && hasTypedCharacter && !isNumericPunctuation && ShouldInvokeLiveCompletion(typedCharacter))
            {
                // Refresh on the next UI turn without repeatedly invoking the native
                // SHOWMEMBERLIST command. The latter displays VC++'s loading popup on
                // top of the CodeAssist preview while IntelliSense is still indexing.
                QueueCompletionRefresh();
                return result;
            }
            if (ErrorHandler.Succeeded(result) && shouldTrigger && !broker.IsCompletionActive(view))
            {
                broker.TriggerCompletion(view);
                PreferIntelliSenseWhenReady();
            }
            else if (ErrorHandler.Succeeded(result) && hasTypedCharacter)
            {
                PreferIntelliSenseWhenReady();
            }
            if (ErrorHandler.Succeeded(result) && isDeletion && !broker.IsCompletionActive(view) && HasCompletionContextAtCaret())
            {
                broker.TriggerCompletion(view);
                PreferIntelliSenseWhenReady();
            }
            if (isExplicitCompletion && !broker.IsCompletionActive(view))
            {
                broker.TriggerCompletion(view);
                PreferIntelliSenseWhenReady();
                if (ErrorHandler.Failed(result)) result = VSConstants.S_OK;
            }
            return result;
        }

        private bool ApplyAutomatic(EditorTextChange change,string name)
        {
            if(change==null)return false;DismissPreviewSessions();undoHistories.TryGetHistory(view.TextBuffer,out var history);
            using(var transaction=BeginEditTransaction(history,name))using(var edit=view.TextBuffer.CreateEdit())
            {if(!edit.Replace(change.Start,change.Length,change.Text)){transaction?.Cancel();return false;}var updated=edit.Apply();if(edit.Canceled){transaction?.Cancel();return false;}view.Caret.MoveTo(new SnapshotPoint(updated,change.SelectionStart));if(change.SelectionLength>0)view.Selection.Select(new SnapshotSpan(updated,change.SelectionStart,change.SelectionLength),false);CompleteEditTransaction(transaction);return true;}
        }
        private bool TryAutomaticInput(char key)
        {
            var preferences=StartupUi.Preferences;var snapshot=view.TextSnapshot;if(snapshot.Length>512*1024)return false;
            if(key!='.'&&(!preferences.RepairTypeCase||char.IsLetterOrDigit(key)||key=='_'))return false;
            var text=snapshot.GetText();var caret=view.Caret.Position.BufferPosition.Position;var word=CppAutomaticEdits.WordBefore(text,caret);
            if(key=='.'&&preferences.ConvertDotToArrow&&word.Length>0)
            {
                var type=new CppReferenceContext(text).ResolveVariableType(word,caret)??indexes?.Index.ResolveVariableType(word);
                var conversion=CppAutomaticEdits.DotToArrow(text,caret,type,CppAutomaticEdits.HasArrowOperator(text,type),preferences.ConvertOverloadedArrow);
                if(conversion==null)return false;
                // Preserve the typed dot as a separate Undo step, matching VA's conversion.
                if(!ApplyAutomatic(new EditorTextChange(caret,0,".",caret+1,0),"CodeAssist Type Dot"))return false;
                ApplyAutomatic(new EditorTextChange(caret,1,conversion.Text,conversion.SelectionStart,conversion.SelectionLength),"CodeAssist Dot to Arrow");
                return true;
            }
            if(preferences.RepairTypeCase&&word.Length>0&&new CppReferenceContext(text).ResolveVariableType(word,caret)==null)ApplyAutomatic(CppAutomaticEdits.RepairCase(text,caret,CppAutomaticEdits.FindDeclaredTypeCasing(text,word)??indexes?.Index.FindCorrectTypeCasing(word)),"CodeAssist Repair Type Case");
            return false;
        }
        private void OnKeyDown(object sender,System.Windows.Input.KeyEventArgs e)
        {
            repeatingKey=e.IsRepeat;
            if(e.Key!=System.Windows.Input.Key.LeftShift&&e.Key!=System.Windows.Input.Key.RightShift){shiftCaret=-1;return;}
            if(!StartupUi.Preferences.InsertUnderscoreAfterM||!view.Selection.IsEmpty||view.IsClosed)return;
            var caret=view.Caret.Position.BufferPosition;var line=caret.GetContainingLine();var before=line.GetText().Substring(0,caret.Position-line.Start.Position);
            if(System.Text.RegularExpressions.Regex.IsMatch(before,@"(?:^|\W)m$")){shiftCaret=caret;shiftStarted=System.Diagnostics.Stopwatch.GetTimestamp();}
        }
        private void OnKeyUp(object sender,System.Windows.Input.KeyEventArgs e)
        {
            repeatingKey=false;
            if(e.Key!=System.Windows.Input.Key.LeftShift&&e.Key!=System.Windows.Input.Key.RightShift)return;
            var expected=shiftCaret;shiftCaret=-1;
            if(expected<0||view.IsClosed||!StartupUi.Preferences.InsertUnderscoreAfterM||!view.Selection.IsEmpty||view.Caret.Position.BufferPosition.Position!=expected)return;
            if((System.Diagnostics.Stopwatch.GetTimestamp()-shiftStarted)/(double)System.Diagnostics.Stopwatch.Frequency>.4)return;
            if(CppAutomaticEdits.WordBefore(view.TextSnapshot.GetText(),expected)=="m")ApplyAutomatic(new EditorTextChange(expected,0,"_",expected+1,0),"CodeAssist Member Prefix");
        }
        private void OnClosed(object sender,EventArgs e){view.VisualElement.PreviewKeyDown-=OnKeyDown;view.VisualElement.PreviewKeyUp-=OnKeyUp;view.Closed-=OnClosed;}
        private bool TrySkipClosing(char key)
        {
            if(view.Selection.IsEmpty && broker.GetSessions(view).All(s=>s.Properties.ContainsProperty(CodeAssistCompletionSessionState.Marker)))
            {
                var snapshot=view.TextSnapshot;var caret=view.Caret.Position.BufferPosition.Position;
                for(var i=pairs.Count-1;i>=0;i--)
                {
                    var pair=pairs[i];var open=pair.Open.GetPosition(snapshot);var close=pair.Close.GetPosition(snapshot);
                    if(open>=close||close>=snapshot.Length||snapshot[open]!=pair.Opening||snapshot[close]!=pair.Closing){pairs.RemoveAt(i);continue;}
                    if(key==pair.Closing&&close==caret){DismissPreviewSessions();view.Caret.MoveTo(new SnapshotPoint(snapshot,caret+1));pairs.RemoveAt(i);return true;}
                }
            }
            return false;
        }
        private ITextUndoTransaction BeginEditTransaction(ITextUndoHistory history,string name)
        {
            if(history==null)return null;
            var transaction=history.CreateTransaction(name);
            try { editorOperations.AddBeforeTextBufferChangePrimitive(); return transaction; }
            catch { transaction.Cancel(); transaction.Dispose(); throw; }
        }
        private void CompleteEditTransaction(ITextUndoTransaction transaction)
        {
            if(transaction==null)return;
            editorOperations.AddAfterTextBufferChangePrimitive(); transaction.Complete();
        }
        private bool TryRemoveEmptyPair()
        {
            if(!view.Selection.IsEmpty)return false;
            var snapshot=view.TextSnapshot;var caret=view.Caret.Position.BufferPosition.Position;
            for(var i=pairs.Count-1;i>=0;i--)
            {
                var pair=pairs[i];var open=pair.Open.GetPosition(snapshot);var close=pair.Close.GetPosition(snapshot);
                if(open>=close||close>=snapshot.Length||snapshot[open]!=pair.Opening||snapshot[close]!=pair.Closing){pairs.RemoveAt(i);continue;}
                if(close!=caret||open+1!=caret)continue;
                undoHistories.TryGetHistory(view.TextBuffer,out var history);
                using(var transaction=BeginEditTransaction(history,"CodeAssist Remove Pair"))
                using(var edit=view.TextBuffer.CreateEdit())
                {
                    if(!edit.Delete(new Span(open,2))){transaction?.Cancel();return false;}
                    var updated=edit.Apply();if(edit.Canceled){transaction?.Cancel();return false;}
                    view.Caret.MoveTo(new SnapshotPoint(updated,open));pairs.RemoveAt(i);CompleteEditTransaction(transaction);return true;
                }
            }
            return false;
        }
        private int TypeOpening(ref Guid group,uint id,uint options,IntPtr input,IntPtr output,char key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DismissPreviewSessions();undoHistories.TryGetHistory(view.TextBuffer,out var history);
            using(var transaction=BeginEditTransaction(history,"CodeAssist Insert Pair"))
            {
                var before=view.TextSnapshot;var selected=view.Selection.StreamSelectionSpan.SnapshotSpan;
                var result=Next.Exec(ref group,id,options,input,output);
                if(ErrorHandler.Failed(result)||view.IsClosed){transaction?.Cancel();return result;}
                var caret=view.Caret.Position.BufferPosition.Position;var snapshot=view.TextSnapshot;
                var indent=(!view.Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId))?"\t":new string(' ',Math.Max(1,Math.Min(16,view.Options.GetOptionValue(DefaultOptions.IndentSizeOptionId))));
                var contents=snapshot.GetText();
                var freshNativePair=CppPairing.IsFreshPair(before.GetText(),selected.Start,selected.Length,contents,caret,key);
                var planned=CppPairing.AfterOpening(freshNativePair?contents.Remove(caret,1):contents,caret,key,StartupUi.Preferences.ClosingBracePlacement,indent);
                if(EditorInputTrace.Enabled)WelcomeDocumentWindow.RecordEvent("PairInsertion owned="+(planned!=null)+" freshNative="+freshNativePair+" caret="+caret);
                if(planned!=null)
                {
                    using(var edit=view.TextBuffer.CreateEdit())
                    {
                        if(edit.Replace(new Span(caret,freshNativePair?1:0),planned.Text))
                        {
                            var updated=edit.Apply();
                            if(!edit.Canceled){RecordPair(updated,caret-1,caret+planned.ClosingOffset,key,CppPairing.Closing(key));view.Caret.MoveTo(new SnapshotPoint(updated,caret+planned.CaretOffset));}
                        }
                    }
                }
                CompleteEditTransaction(transaction);return result;
            }
        }
        private void RecordPair(ITextSnapshot snapshot,int open,int close,char opening,char closing)
        {
            if(pairs.Count>=128)pairs.RemoveAt(0);
            pairs.Add(new OwnedPair {Open=snapshot.CreateTrackingPoint(open,PointTrackingMode.Positive),Close=snapshot.CreateTrackingPoint(close,PointTrackingMode.Positive),Opening=opening,Closing=closing});
        }
        private void AddMethodCall(Completion completion)
        {
            if(!StartupUi.Preferences.PairCompletionEnabled||!(completion is ColoredCompletion colored)||colored.Kind!=SymbolKind.Function)return;
            var snapshot=view.TextSnapshot;var caret=view.Caret.Position.BufferPosition.Position;
            if(!CppPairing.CanAddMethodCall(snapshot.GetText(),caret,completion.InsertionText))return;
            var inserted=new string(' ',StartupUi.Preferences.MethodParenthesesSpaces)+"()";
            undoHistories.TryGetHistory(view.TextBuffer,out var history);
            using(var transaction=BeginEditTransaction(history,"CodeAssist Method Parentheses"))
            using(var edit=view.TextBuffer.CreateEdit())
            {
                if(!edit.Insert(caret,inserted)){transaction?.Cancel();return;}
                var updated=edit.Apply();if(edit.Canceled){transaction?.Cancel();return;}
                var open=caret+inserted.Length-2;RecordPair(updated,open,open+1,'(',')');
                var empty=System.Text.RegularExpressions.Regex.IsMatch(completion.Description??"",@"\(\s*(?:void\s*)?\)");
                view.Caret.MoveTo(new SnapshotPoint(updated,open+(empty?2:1)));CompleteEditTransaction(transaction);
            }
        }

        private bool TrySurroundSelection(char key)
        {
            var p=StartupUi.Preferences;
            if(!p.SurroundSelectionEnabled || view.Selection.IsEmpty || view.Selection.SelectedSpans.Count!=1)return false;
            if(p.SurroundWithSnippets&&SnippetInsertion.TrySurroundCharacter(view,key))return true;
            if(key=='#'&&p.SurroundHash){var span=view.Selection.StreamSelectionSpan.SnapshotSpan;var directiveChange=CppEditing.SurroundDirective(view.TextSnapshot.GetText(),span.Start,span.Length);if(!ApplyAutomatic(directiveChange,"CodeAssist Conditional Directive"))return false;return true;}
            if(!(key=='/'&&p.SurroundSlash || key=='*'&&p.SurroundAsterisk || key=='('&&p.SurroundParentheses || key=='{'&&p.SurroundBraces))return false;
            var selected=view.Selection.StreamSelectionSpan.SnapshotSpan;
            var change=CppEditing.Surround(view.TextSnapshot.GetText(),selected.Start,selected.Length,key,p.CommentSpaceEnabled);
            if(change==null)return false;
            undoHistories.TryGetHistory(view.TextBuffer,out var history);
            using(var transaction=BeginEditTransaction(history,"CodeAssist Surround Selection"))
            using(var edit=view.TextBuffer.CreateEdit())
            {
                if(!edit.Replace(new Span(change.Start,change.Length),change.Text)){transaction?.Cancel();return false;}
                var updated=edit.Apply();if(edit.Canceled){transaction?.Cancel();return false;}
                view.Selection.Select(new SnapshotSpan(updated,change.SelectionStart,change.SelectionLength),false);
                view.Caret.MoveTo(new SnapshotPoint(updated,change.SelectionStart+change.SelectionLength));
                if(key=='('||key=='{') {
                    var end=updated.CreateTrackingPoint(change.SelectionStart+change.SelectionLength,PointTrackingMode.Positive);
                    var group=VSConstants.VSStd2K;Next.Exec(ref group,(uint)VSConstants.VSStd2KCmdID.FORMATSELECTION,0,IntPtr.Zero,IntPtr.Zero);
                    view.Selection.Clear();view.Caret.MoveTo(end.GetPoint(view.TextSnapshot));
                }
                CompleteEditTransaction(transaction);DismissAllCompletionSessions();return true;
            }
        }
        private int ContinueComment(ref Guid group,uint id,uint options,IntPtr input,IntPtr output,string prefix)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            undoHistories.TryGetHistory(view.TextBuffer,out var history);
            using(var transaction=BeginEditTransaction(history,"CodeAssist Extend Comment"))
            {
                var result=Next.Exec(ref group,id,options,input,output);
                if(ErrorHandler.Failed(result)||view.IsClosed){transaction?.Cancel();return result;}
                var caret=view.Caret.Position.BufferPosition;var line=caret.GetContainingLine();
                var before=caret.Snapshot.GetText(line.Start,caret.Position-line.Start);
                // Preserve the IDE's own continuation when it already inserted a marker.
                if(string.IsNullOrWhiteSpace(before))
                {
                    using(var edit=view.TextBuffer.CreateEdit())
                    {
                        if(edit.Replace(new Span(line.Start,before.Length),prefix))
                        {var changed=edit.Apply();if(!edit.Canceled)view.Caret.MoveTo(new SnapshotPoint(changed,line.Start.Position+prefix.Length));}
                    }
                }
                CompleteEditTransaction(transaction);return result;
            }
        }
        private void FormatPastedLines(ITrackingSpan inserted)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(view.IsClosed)return;
            var snapshot=view.TextSnapshot;var span=inserted.GetSpan(snapshot);
            if(span.Length==0||span.GetText().IndexOf('\n')<0)return;
            var selection=view.Selection.StreamSelectionSpan.SnapshotSpan.Snapshot.CreateTrackingSpan(view.Selection.StreamSelectionSpan.SnapshotSpan.Span,SpanTrackingMode.EdgeInclusive);
            var caret=snapshot.CreateTrackingPoint(view.Caret.Position.BufferPosition.Position,PointTrackingMode.Positive);
            var reversed=view.Selection.IsReversed;
            try
            {
                view.Selection.Select(span,false);var group=VSConstants.VSStd2K;
                Next.Exec(ref group,(uint)VSConstants.VSStd2KCmdID.FORMATSELECTION,0,IntPtr.Zero,IntPtr.Zero);
            }
            finally
            {
                if(!view.IsClosed){view.Selection.Select(selection.GetSpan(view.TextSnapshot),reversed);view.Caret.MoveTo(caret.GetPoint(view.TextSnapshot));}
            }
        }

        private void RefreshCodeAssistSessions()
        {
            foreach (var session in broker.GetSessions(view).ToArray())
            {
                if (!session.Properties.ContainsProperty(CodeAssistCompletionSessionState.Marker)) continue;
                // Preserve the 0.0.13 live-refresh behavior without entering the
                // native VCCompletionSet that caused the later devenv crash.
                foreach (var set in session.CompletionSets.Where(item =>
                    string.Equals(item.Moniker, "CodeAssistPreview", StringComparison.Ordinal)).ToArray())
                {
                    set.Filter();
                }
            }
        }

        private bool TryCommitCodeAssistSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var session in broker.GetSessions(view).ToArray())
            {
                var set = session.SelectedCompletionSet;
                var selected = set?.SelectionStatus?.Completion;
                var isCodeAssist = string.Equals(set?.Moniker, "CodeAssistPreview", StringComparison.Ordinal) ||
                                 selected is Completion4 completion && string.Equals(completion.Suffix, "CodeAssist", StringComparison.Ordinal);
                if (!isCodeAssist || selected == null) continue;
                var identity = IdentityOf(selected);
                var candidates = set.Completions.ToArray();
                var previousIndex = Array.IndexOf(candidates, selected);
                var index = CompletionSelection.Validate(identity, previousIndex < 0 ? candidates.Length : previousIndex,
                    candidates.Select(IdentityOf).ToArray());
                if (index < 0) { session.Dismiss(); return false; }
                set.SelectionStatus = new CompletionSelectionStatus(candidates[index], true, true);
                if(candidates[index] is SnippetCompletion snippet){var span=set.ApplicableTo.GetSpan(view.TextSnapshot);session.Dismiss();SnippetInsertion.CommitSuggestion(view,snippet.Shortcut,span);return true;}
                undoHistories.TryGetHistory(view.TextBuffer, out var history);
                using (var transaction = BeginEditTransaction(history, "CodeAssist Complete Word"))
                {
                    session.Commit();
                    AddMethodCall(candidates[index]);
                    CompleteEditTransaction(transaction);
                }
                return true;
            }
            return false;
        }

        private static bool IsCommitCommand(Guid commandGroup, uint commandId)
        {
            return commandGroup == VSConstants.VSStd2K &&
                   ((StartupUi.Preferences.CompletionCommitWithTab && commandId == (uint)VSConstants.VSStd2KCmdID.TAB) ||
                    (StartupUi.Preferences.CompletionCommitWithEnter && commandId == (uint)VSConstants.VSStd2KCmdID.RETURN));
        }

        private static string IdentityOf(Completion completion) => completion == null ? null : completion.InsertionText + "\0" + completion.Description;

        private static bool IsExplicitCompletion(Guid commandGroup, uint commandId)
        {
            return commandGroup == VSConstants.VSStd2K &&
                   (commandId == (uint)VSConstants.VSStd2KCmdID.COMPLETEWORD ||
                    commandId == (uint)VSConstants.VSStd2KCmdID.SHOWMEMBERLIST ||
                    commandId == (uint)VSConstants.VSStd2KCmdID.AUTOCOMPLETE);
        }

        private static bool IsDeletion(Guid commandGroup, uint commandId)
        {
            return commandGroup == VSConstants.VSStd2K &&
                   (commandId == (uint)VSConstants.VSStd2KCmdID.BACKSPACE ||
                    commandId == (uint)VSConstants.VSStd2KCmdID.DELETE);
        }

        private bool HasCompletionContextAtCaret()
        {
            var point = view.Caret.Position.BufferPosition;
            if (point.Position == 0) return false;
            var previous = point.Snapshot[point.Position - 1];
            return char.IsLetterOrDigit(previous) || previous == '_';
        }

        private static bool TryGetTypedCharacter(Guid commandGroup, uint commandId, IntPtr input, out char character)
        {
            character = default(char);
            if (commandGroup != VSConstants.VSStd2K || commandId != (uint)VSConstants.VSStd2KCmdID.TYPECHAR || input == IntPtr.Zero)
            {
                return false;
            }

            var value = Marshal.GetObjectForNativeVariant(input);
            if (!(value is ushort characterCode)) return false;
            character = (char)characterCode;
            return true;
        }

        private static bool ShouldTrigger(char character)
        {
            return char.IsLetter(character) || character == '_';
        }

        private bool ShouldInvokeLiveCompletion(char character)
        {
            if (char.IsDigit(character))
            {
                return HasIdentifierCompletionContextAtCaret();
            }
            if (char.IsLetter(character) || character == '_' || character == '.' || character == '>')
            {
                return true;
            }

            if (character != ':') return false;
            var caret = view.Caret.Position.BufferPosition;
            return caret.Position >= 2 && caret.Snapshot[caret.Position - 2] == ':';
        }

        private bool HasIdentifierCompletionContextAtCaret()
        {
            var caret = view.Caret.Position.BufferPosition;
            var position = caret.Position;
            while (position > 0)
            {
                var character = caret.Snapshot[position - 1];
                if (!char.IsLetterOrDigit(character) && character != '_') break;
                position--;
            }
            return position < caret.Position &&
                   (char.IsLetter(caret.Snapshot[position]) || caret.Snapshot[position] == '_');
        }

        private bool IsNumericLiteralPunctuation(char character)
        {
            if (character != '.') return false;
            var caret = view.Caret.Position.BufferPosition;
            if (caret.Position == 0) return false;
            return char.IsDigit(caret.Snapshot[caret.Position - 1]);
        }

        private void QueueCompletionRefresh()
        {
            if (liveCompletionQueued || view.IsClosed || uiContext == null) return;
            liveCompletionQueued = true;
#pragma warning disable VSTHRD001 // Captured from IVsTextViewCreated on the VS UI thread.
            uiContext.Post(delegate
            {
                liveCompletionQueued = false;
                if (view.IsClosed) return;
                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    InvokeCompletionRefresh();
                }
                catch (Exception)
                {
                    // Completion must never terminate devenv.exe.
                }
            }, null);
#pragma warning restore VSTHRD001
        }

        private void InvokeCompletionRefresh()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(EditorInputTrace.Enabled)WelcomeDocumentWindow.RecordEvent("CompletionRefresh caret="+view.Caret.Position.BufferPosition.Position+" active="+broker.GetSessions(view).Count);
            var CodeAssistSessions = broker.GetSessions(view)
                .Where(session => session.Properties.ContainsProperty(CodeAssistCompletionSessionState.Marker))
                .ToArray();
            if (CodeAssistSessions.Length > 0)
            {
                var previous = CodeAssistSessions.Select(session => session.SelectedCompletionSet?.SelectionStatus)
                    .FirstOrDefault(status => status?.IsSelected == true);
                var identity = IdentityOf(previous?.Completion);
                // Filtering can only remove items from the list created for the
                // first character. Recreate our standalone safe session so a more
                // specific prefix can bring GetWorld/GetPawn and similar symbols in.
                foreach (var session in CodeAssistSessions) session.Dismiss();
                broker.TriggerCompletion(view);
                if (identity != null)
                {
                    foreach (var session in broker.GetSessions(view))
                    foreach (var set in session.CompletionSets.Where(item => item.Moniker == "CodeAssistPreview"))
                    {
                        var items = set.Completions.ToArray();
                        var index = CompletionSelection.Validate(identity, items.Length, items.Select(IdentityOf).ToArray());
                        if (index >= 0) set.SelectionStatus = new CompletionSelectionStatus(items[index], true, true);
                    }
                }
                return;
            }

            if (!broker.IsCompletionActive(view))
            {
                broker.TriggerCompletion(view);
            }
            else
            {
                RefreshCodeAssistSessions();
            }
        }

        private static bool ShouldDismissPreviewBeforeInput(char character)
        {
            return !char.IsLetterOrDigit(character) && character != '_';
        }

        private void DismissPreviewSessions()
        {
            foreach (var session in broker.GetSessions(view).ToArray())
            {
                if (session.CompletionSets.Count > 0 && session.CompletionSets.All(set => string.Equals(set.Moniker, "CodeAssistPreview", StringComparison.Ordinal)))
                {
                    session.Dismiss();
                }
            }
        }

        private void DismissAllCompletionSessions()
        {
            foreach (var session in broker.GetSessions(view).ToArray())
            {
                session.Dismiss();
            }
        }

        private void PreferIntelliSenseWhenReady()
        {
            foreach (var session in broker.GetSessions(view).ToArray())
            {
                var hasPreview = session.CompletionSets.Any(set => string.Equals(set.Moniker, "CodeAssistPreview", StringComparison.Ordinal));
                var hasIntelliSense = session.CompletionSets.Any(set => !string.Equals(set.Moniker, "CodeAssistPreview", StringComparison.Ordinal) && set.Completions.Count > 0);
                if (!hasPreview || !hasIntelliSense) continue;
                session.Dismiss();
                break;
            }
        }
    }
}
