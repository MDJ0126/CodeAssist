using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using CodeAssist.Core;
using CodeAssist.Indexing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using System.Threading;
using CodeAssist.Semantics;

namespace CodeAssist.Extension
{
    [Export(typeof(ICompletionSourceProvider))]
    [Export(typeof(ITextViewCreationListener))]
    [ContentType("C/C++")]
    [Name("CodeAssist Completion")]
    [Order(After = Priority.Default)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class CodeAssistCompletionSourceProvider : ICompletionSourceProvider, ITextViewCreationListener
    {
        [Import]
        internal ProjectIndexService IndexService = null;

        [Import]
        internal ITextDocumentFactoryService DocumentFactory = null;

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            if (DocumentFactory.TryGetTextDocument(textBuffer, out var document))
            {
                IndexService.InitializeFromDocument(document.FilePath);
            }
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new CodeAssistCompletionSource(textBuffer, IndexService, document?.FilePath));
        }

        public void TextViewCreated(ITextView textView)
        {
            var buffer = textView.TextBuffer;
            if (!DocumentFactory.TryGetTextDocument(buffer, out var document)) return;
            IndexService.InitializeFromDocument(document.FilePath);
            buffer.Properties.GetOrCreateSingletonProperty(() =>
                new LiveDocumentTracker(buffer, document.FilePath, IndexService)).Attach(textView);
        }
    }

    internal sealed class CodeAssistCompletionSource : ICompletionSource
    {
        private static readonly Regex MemberPattern = new Regex(@"(?<receiver>[A-Za-z_]\w*)(?<call>\s*\(\s*\))?\s*(?<operator>->|\.|::)\s*(?<prefix>[A-Za-z_]\w*)?$", RegexOptions.Compiled);
        private static readonly Regex UnrealSpecifierContext = new Regex(@"\b(?:UPROPERTY|UFUNCTION|UDELEGATE|UCLASS|USTRUCT|UENUM|UINTERFACE)\s*\([^)]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex IncludeContext = new Regex(@"^\s*#\s*include\s*[<""](?<prefix>[^>""]*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly ITextBuffer buffer;
        private readonly ProjectIndexService indexService;
        private bool disposed;
        private int tracedQueries;
        private readonly string filePath;
        private ITextSnapshot compilerSnapshot;
        private int compilerPosition = -1;
        private string compilerRoot;
        private IndexedSymbol[] compilerCandidates = Array.Empty<IndexedSymbol>();
        private CancellationTokenSource compilerPending;
        private int contextVersion = -1;
        private CppReferenceContext context;

        public CodeAssistCompletionSource(ITextBuffer buffer, ProjectIndexService indexService, string filePath)
        {
            this.buffer = buffer;
            this.indexService = indexService;
            this.filePath = filePath;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (tracedQueries++ < 12) WelcomeDocumentWindow.RecordEvent("CompletionQuery file="+filePath+" count="+indexService.Index.Count+" root="+indexService.ProjectRoot);
            if (!StartupUi.Preferences.CompletionEnabled) return;
            if (disposed) return;
            var point = session.GetTriggerPoint(buffer.CurrentSnapshot);
            if (!point.HasValue) return;

            var snapshot = point.Value.Snapshot;
            var line = snapshot.GetLineFromPosition(point.Value.Position);
            var beforeCaret = snapshot.GetText(line.Start.Position, point.Value.Position - line.Start.Position);
            var memberMatch = MemberPattern.Match(beforeCaret);
            var includeMatch = IncludeContext.Match(beforeCaret);
            var currentOwner = FindEnclosingOwner(snapshot, point.Value.Position);
            string prefix;
            IReadOnlyList<IndexedSymbol> candidates;
            var hasResolvedMemberContext = false;
            string memberOwner=null;
            if (includeMatch.Success)
            {
                prefix = includeMatch.Groups["prefix"].Value;
                candidates = indexService.CompleteHeaders(prefix, 100);
            }
            else if (UnrealSpecifierContext.IsMatch(beforeCaret))
            {
                prefix = GetIdentifierPrefix(beforeCaret);
                candidates = indexService.Index.CompleteSpecifiers(prefix, 100);
            }
            else if (memberMatch.Success)
            {
                prefix = memberMatch.Groups["prefix"].Value;
                var receiver = memberMatch.Groups["receiver"].Value;
                var typeName = memberMatch.Groups["operator"].Value == "::"
                    ? receiver
                    : receiver == "Super"
                    ? indexService.Index.ResolveBaseType(currentOwner)
                    : memberMatch.Groups["call"].Success
                        ? indexService.Index.ResolveReturnType(currentOwner, receiver)
                        : ResolveLocalVariableType(snapshot, receiver, line.Start.Position + memberMatch.Groups["receiver"].Index + receiver.Length) ?? indexService.Index.ResolveVariableType(currentOwner, receiver);
                if (memberMatch.Groups["operator"].Value == "->")
                    typeName = indexService.Index.ResolveArrowTargetType(typeName);
                if (tracedQueries <= 12) WelcomeDocumentWindow.RecordEvent("CompletionReceiver receiver="+receiver+" op="+memberMatch.Groups["operator"].Value+" type="+typeName);
                hasResolvedMemberContext = !string.IsNullOrWhiteSpace(typeName);
                memberOwner=NormalizeType(typeName);
                candidates = hasResolvedMemberContext
                    ? indexService.CompleteLiveMembers(NormalizeType(typeName), prefix, 100, StartupUi.Preferences.AcronymCompletionEnabled, StartupUi.Preferences.ShorthandCompletionEnabled)
                    : receiver == "Super"
                        ? indexService.Index.Complete(prefix, 100, StartupUi.Preferences.AcronymCompletionEnabled, StartupUi.Preferences.ShorthandCompletionEnabled)
                        : Array.Empty<IndexedSymbol>();
            }
            else
            {
                prefix = GetIdentifierPrefix(beforeCaret);
                candidates = MergeCandidates(
                    indexService.Index.Complete(prefix, 200,StartupUi.Preferences.AcronymCompletionEnabled,StartupUi.Preferences.ShorthandCompletionEnabled),
                    indexService.CompleteLive(prefix, 200, filePath).Where(item => !item.IsLocal ||
                        ResolveLocalVariableType(snapshot, item.Name, point.Value.Position) != null).ToArray());
            }


            if (!includeMatch.Success && !UnrealSpecifierContext.IsMatch(beforeCaret))
            {
                RequestCompilerCompletion(session, snapshot, point.Value.Position, prefix,memberMatch.Success);
                if (ReferenceEquals(compilerSnapshot, snapshot) && compilerPosition == point.Value.Position &&
                    string.Equals(compilerRoot, indexService.ProjectRoot, StringComparison.OrdinalIgnoreCase))
                    candidates = candidates.Concat(compilerCandidates).GroupBy(item => item.Name, StringComparer.Ordinal)
                        .Select(group => group.First()).ToArray();
            }
            // Member declarations are not recent usage: retain the index match order for member lists.
            candidates = (hasResolvedMemberContext ? candidates : RankByRecentUse(candidates, snapshot, point.Value.Position)).Take(100).ToArray();

            if(!StartupUi.Preferences.SuggestionListsEnabled)return;
            var snippetItems=StartupUi.Preferences.SnippetSuggestionsEnabled&&!hasResolvedMemberContext&&!includeMatch.Success?SnippetInsertion.Suggestions(indexService.ProjectRoot,prefix).Select(t=>(Completion)new SnippetCompletion(t)).ToList():new List<Completion>();
            if(StartupUi.Preferences.SmartSuggestionsEnabled&&!hasResolvedMemberContext&&!includeMatch.Success)
                snippetItems.AddRange(SmartSuggestions.At(snapshot.GetText(),point.Value.Position).Where(value=>value.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)).Select(value=>(Completion)new Completion(value,value,"CodeAssist expected-type literal",null,"literal")));
            if (tracedQueries <= 12) WelcomeDocumentWindow.RecordEvent("CompletionCandidates count="+candidates.Count+" snippets="+snippetItems.Count+" sets="+completionSets.Count);
            if (candidates.Count == 0&&snippetItems.Count==0) return;
            var builtInSet = completionSets.FirstOrDefault(set =>
                !string.Equals(set.Moniker, "CodeAssistPreview", StringComparison.Ordinal) &&
                set.Completions != null && set.Completions.Count > 0);
            var start = point.Value.Position - prefix.Length;
            var applicable = snapshot.CreateTrackingSpan(start, prefix.Length, SpanTrackingMode.EdgeInclusive);
            if (builtInSet != null)
            {
                var builtInNames = new HashSet<string>(builtInSet.Completions.Select(item => item.InsertionText), StringComparer.OrdinalIgnoreCase);
                var overlap = candidates.Count(candidate => builtInNames.Contains(candidate.Name));
                if (hasResolvedMemberContext) indexService.ReportIntelliSenseEvidence(overlap, candidates.Count);
                var previewItems = candidates.Where(candidate => !builtInNames.Contains(candidate.Name)).Select(CreateCompletion).ToList();
                // Native-only lists cannot render our semantic colors. Keep the
                // native presentation only when the user explicitly selects it.
                if (previewItems.Count == 0 && snippetItems.Count == 0 &&
                    StartupUi.Preferences.CompletionUseVisualStudioColors) return;

                // Never display VCCompletionSet and CodeAssist CompletionSet together.
                // Visual Studio 17.14 can crash inside VCCompletionSet's highlight
                // callback while WPF realizes items from the mixed session.
                var combined = candidates.Select(symbol => CreateCompletion(symbol,builtInSet.Completions.FirstOrDefault(item=>string.Equals(item.InsertionText,symbol.Name,StringComparison.Ordinal)))).Concat(snippetItems).ToList();
                foreach (var native in builtInSet.Completions)
                {
                    if (combined.Any(item => string.Equals(item.InsertionText, native.InsertionText, StringComparison.OrdinalIgnoreCase))) continue;
                    combined.Add(CloneCompletion(native));
                    if (combined.Count >= 200) break;
                }
                completionSets.Clear();
                completionSets.Add(new PreviewCompletionSet(applicable, combined,memberOwner));
                MarkCodeAssistSession(session);
                return;
            }

            var completions = candidates.Select(CreateCompletion).Concat(snippetItems).ToList();
            completionSets.Add(new PreviewCompletionSet(applicable, completions,memberOwner));
            MarkCodeAssistSession(session);
        }

        private void RequestCompilerCompletion(ICompletionSession session, ITextSnapshot snapshot, int position, string prefix,bool membersOnly)
        {
            var root = indexService.ProjectRoot;
            var owner = ExtensionPackage.Current;
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(root) ||
                owner == null || ReferenceEquals(compilerSnapshot, snapshot) && compilerPosition == position &&
                string.Equals(compilerRoot, root, StringComparison.OrdinalIgnoreCase)) return;
            compilerPending?.Cancel();
            var pending = CancellationTokenSource.CreateLinkedTokenSource(owner.DisposalToken); compilerPending = pending;
            compilerRoot = root;
            compilerSnapshot = snapshot; compilerPosition = position; compilerCandidates = Array.Empty<IndexedSymbol>();
            var text = snapshot.GetText(); var line = snapshot.GetLineFromPosition(position);
            var executable = CompilerSemanticService.Instance.EditorExecutable;
            owner.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    var result = await CompilerSemanticService.Instance.QueryAsync("textDocument/completion", root, executable, filePath, text,
                        line.LineNumber + 1, position - line.Start.Position + 1, pending.Token,
                        new JObject { ["context"] = new JObject { ["triggerKind"] = 1 } });
                    if (result == null) return;
                    var found = SemanticCompletions.Read(result, text, position - prefix.Length, position,membersOnly).Select(item =>
                        new IndexedSymbol(item.Name, item.Kind == 2 || item.Kind == 3 || item.Kind == 4 ? SymbolKind.Function :
                            item.Kind == 7 || item.Kind == 8 || item.Kind == 13 || item.Kind == 22 || item.Kind == 25 ? SymbolKind.Type :
                            item.Kind == 5 || item.Kind == 6 || item.Kind == 10 || item.Kind == 20 || item.Kind == 21 ? SymbolKind.Variable : SymbolKind.Unknown,
                            "", 0, 0, signature: item.Detail)).ToArray();
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (disposed || pending.IsCancellationRequested || !ReferenceEquals(buffer.CurrentSnapshot, snapshot) ||
                        session.IsDismissed || session.TextView.IsClosed || session.TextView.Caret.Position.BufferPosition.Position != position ||
                        !string.Equals(root, indexService.ProjectRoot, StringComparison.OrdinalIgnoreCase)) return;
                    compilerCandidates = found;
                    if (found.Length > 0) session.Recalculate();
                }
                catch (OperationCanceledException) { }
                finally
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (ReferenceEquals(compilerPending, pending)) compilerPending = null;
                    pending.Dispose();
                }
            }).FileAndForget("CodeAssist/CompilerCompletion");
        }
        public void Dispose()
        {
            disposed = true;
            compilerPending?.Cancel();
        }

        private string ResolveLocalVariableType(ITextSnapshot snapshot, string name, int position)
        {
            if (contextVersion != snapshot.Version.VersionNumber)
            { context = new CppReferenceContext(snapshot.GetText()); contextVersion = snapshot.Version.VersionNumber; }
            return context.ResolveVariableType(name, position);
        }

        private static Completion CreateCompletion(IndexedSymbol symbol) => CreateCompletion(symbol,null);
        private static Completion CreateCompletion(IndexedSymbol symbol,Completion native)
        {
            var signature=string.IsNullOrWhiteSpace(symbol.Signature)?symbol.Name:symbol.Signature;
            if(!string.IsNullOrWhiteSpace(symbol.OwnerType)&&signature.StartsWith(symbol.Name,StringComparison.Ordinal))signature=symbol.OwnerType+"::"+signature;
            var declaration=(string.IsNullOrWhiteSpace(symbol.ValueType)?signature:symbol.ValueType+" "+signature).Trim();
            var description=!string.IsNullOrWhiteSpace(native?.Description)?native.Description:declaration;
            if(native==null&&!string.IsNullOrWhiteSpace(symbol.FilePath))description+="\n"+System.IO.Path.GetFileName(symbol.FilePath);
            return new ColoredCompletion(symbol.Kind,symbol.Name,symbol.Name,description,CompletionIcons.For(symbol.Kind),symbol.Kind.ToString(),null,"",symbol.OwnerType);
        }

        private static Completion CloneCompletion(Completion completion)
        {
            // Completion3.IconSource is always null: preserve its public image moniker.
            if (completion is Completion3 modern)
                return new Completion4(completion.DisplayText, completion.InsertionText,
                    completion.Description, modern.IconMoniker, completion.IconAutomationText,
                    modern.AttributeIcons?.OfType<CompletionIcon2>(), (completion as Completion4)?.Suffix ?? "");
            return new Completion(
                completion.DisplayText,
                completion.InsertionText,
                completion.Description,
                completion.IconSource,
                completion.IconAutomationText);
        }

        private static string GetIdentifierPrefix(string text)
        {
            var index = text.Length;
            while (index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_')) index--;
            return text.Substring(index);
        }

        private static string NormalizeType(string typeName)
        {
            return PersistentSymbolIndex.NormalizeTypeName(typeName);
        }

        private static string FindEnclosingOwner(ITextSnapshot snapshot, int caretPosition)
        {
            return CppExpressionContext.FindEnclosingFunctionOwner(snapshot.GetText(), caretPosition);
        }

        private static IReadOnlyList<IndexedSymbol> MergeCandidates(params IReadOnlyList<IndexedSymbol>[] sources)
        {
            return sources.SelectMany(items => items ?? Array.Empty<IndexedSymbol>())
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .ToArray();
        }

        private static IEnumerable<IndexedSymbol> RankByRecentUse(
            IReadOnlyList<IndexedSymbol> candidates, ITextSnapshot snapshot, int caretPosition)
        {
            if (candidates == null || candidates.Count == 0) return Array.Empty<IndexedSymbol>();
            var text = snapshot.GetText(0, Math.Max(0, Math.Min(caretPosition, snapshot.Length)));
            return candidates
                .Select(item => new { Item = item, LastUse = text.LastIndexOf(item.Name, StringComparison.OrdinalIgnoreCase) })
                .OrderByDescending(value => value.LastUse)
                .ThenBy(value => value.Item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(value => value.Item);
        }

        private static void MarkCodeAssistSession(ICompletionSession session)
        {
            // VC++ can create its completion session after our queued standalone session.
            // Retain one current preview per view instead of stacking two presenters.
            var properties=session.TextView.Properties;
            if(properties.TryGetProperty(CodeAssistCompletionSessionState.ActiveSession,out ICompletionSession previous)&&!ReferenceEquals(previous,session)&&!previous.IsDismissed)previous.Dismiss();
            properties.RemoveProperty(CodeAssistCompletionSessionState.ActiveSession);
            properties.AddProperty(CodeAssistCompletionSessionState.ActiveSession,session);
            if (!session.Properties.ContainsProperty(CodeAssistCompletionSessionState.Marker))
            {
                session.Properties.AddProperty(CodeAssistCompletionSessionState.Marker, true);
                if(EditorInputTrace.Enabled){WelcomeDocumentWindow.RecordEvent("CompletionSession created id="+session.GetHashCode()+" caret="+session.TextView.Caret.Position.BufferPosition.Position);session.Dismissed+=(sender,args)=>WelcomeDocumentWindow.RecordEvent("CompletionSession dismissed id="+session.GetHashCode()+" closed="+session.TextView.IsClosed);}

                session.Dismissed+=(sender,args)=>{if(properties.TryGetProperty(CodeAssistCompletionSessionState.ActiveSession,out ICompletionSession current)&&ReferenceEquals(current,session))properties.RemoveProperty(CodeAssistCompletionSessionState.ActiveSession);};
                session.Committed+=(sender,args)=>{
                    ThreadHelper.ThrowIfNotOnUIThread();
                    if(session.SelectedCompletionSet?.SelectionStatus?.Completion is SnippetCompletion snippet&&session.TextView is Microsoft.VisualStudio.Text.Editor.IWpfTextView view&&!view.IsClosed)
                    {var span=session.SelectedCompletionSet.ApplicableTo.GetSpan(view.TextSnapshot);SnippetInsertion.CommitSuggestion(view,snippet.Shortcut,span);}
                };
                CompletionPopupColors.Connect(session);
            }
        }
    }

    internal static class CodeAssistCompletionSessionState
    {
        internal static readonly object Marker = new object();
        internal static readonly object ActiveSession = new object();
    }

    internal sealed class SnippetCompletion:Completion
    {
        internal string Shortcut{get;}
        internal SnippetCompletion(SnippetTemplate template):base(template.Shortcut,template.Shortcut,template.Title+" (CodeAssist snippet)",null,"snippet"){Shortcut=template.Shortcut;}
    }
    internal sealed class PreviewCompletionSet : CompletionSet
    {
        private readonly Completion[] all;
        internal string MemberOwner {get;}
        public PreviewCompletionSet(ITrackingSpan applicableTo, IList<Completion> completions,string memberOwner=null)
            : base("CodeAssistPreview", "CodeAssist 미리보기", applicableTo, completions, null)
        {
            all=completions.ToArray();MemberOwner=memberOwner;
        }
        internal void SelectKinds(IEnumerable<SymbolKind> kinds,bool nonInheritedFirst)
        {
            var selected=SelectionStatus?.Completion?.InsertionText;
            var selectedKinds=new HashSet<SymbolKind>(kinds);
            IEnumerable<Completion> query=all.Where(item=>selectedKinds.Count==0||item is ColoredCompletion colored&&selectedKinds.Contains(colored.Kind));
            if(nonInheritedFirst&&!string.IsNullOrEmpty(MemberOwner))query=query.OrderBy(item=>item is ColoredCompletion colored&&colored.Kind==SymbolKind.Function&&string.Equals(colored.OwnerType,MemberOwner,StringComparison.OrdinalIgnoreCase)?0:1);
            var visible=query.ToArray();
            WritableCompletions.Clear();foreach(var item in visible)WritableCompletions.Add(item);
            Filter();
            var current=visible.FirstOrDefault(item=>item.InsertionText==selected)??visible.FirstOrDefault();
            SelectionStatus=new CompletionSelectionStatus(current,current!=null,current!=null);
        }

        public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
        {
            var snapshot = ApplicableTo.TextBuffer.CurrentSnapshot;
            var query = ApplicableTo.GetText(snapshot);
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(displayText)) return null;

            var start = displayText.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (start >= 0) return new[] { new Span(start, query.Length) };

            var spans = new List<Span>();
            var queryIndex = 0;
            for (var index = 0; index < displayText.Length && queryIndex < query.Length; index++)
            {
                if (char.ToUpperInvariant(displayText[index]) == char.ToUpperInvariant(query[queryIndex]))
                {
                    spans.Add(new Span(index, 1));
                    queryIndex++;
                }
            }
            return queryIndex == query.Length ? spans : null;
        }

    }


    internal static class CompletionIcons
    {
        private static readonly Dictionary<SymbolKind,Microsoft.VisualStudio.Imaging.Interop.IImageHandle> badges = new Dictionary<SymbolKind,Microsoft.VisualStudio.Imaging.Interop.IImageHandle>();
        internal static void Initialize(Microsoft.VisualStudio.Shell.IVsManagedImageService images,System.Windows.Media.ImageSource brand)
        {
            if(images==null)return;
            foreach(var kind in new[]{SymbolKind.Type,SymbolKind.Function,SymbolKind.Variable,SymbolKind.Macro,SymbolKind.Specifier})
            {
                var group=new System.Windows.Media.DrawingGroup();
                var color=kind==SymbolKind.Type?System.Windows.Media.Color.FromRgb(255,215,0):kind==SymbolKind.Variable?System.Windows.Media.Color.FromRgb(70,180,230):kind==SymbolKind.Macro?System.Windows.Media.Color.FromRgb(189,99,197):kind==SymbolKind.Specifier?System.Windows.Media.Color.FromRgb(100,200,160):System.Windows.Media.Color.FromRgb(160,120,200);
                using(var draw=group.Open())
                {
                    var brush=new System.Windows.Media.SolidColorBrush(color);var pen=new System.Windows.Media.Pen(brush,1);
                    draw.DrawGeometry(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60,color.R,color.G,color.B)),pen,System.Windows.Media.Geometry.Parse("M1,5 L7,2 L13,5 L13,12 L7,15 L1,12 Z M1,5 L7,8 L13,5 M7,8 L7,15"));
                    if(kind==SymbolKind.Type)draw.DrawLine(pen,new System.Windows.Point(3,11),new System.Windows.Point(5,12));
                    if(brand!=null)draw.DrawImage(brand,new System.Windows.Rect(9,0,7,7));
                }
                var icon=new System.Windows.Media.DrawingImage(group);icon.Freeze();badges[kind]=images.AddCustomImage(icon,false);
            }
        }
        public static Microsoft.VisualStudio.Imaging.Interop.ImageMoniker For(SymbolKind kind)
        {
            if(badges.TryGetValue(kind,out var badge))return badge.Moniker;
            return kind == SymbolKind.Type
                ? KnownMonikers.Class
                : kind == SymbolKind.Function
                    ? KnownMonikers.Method
                    : kind == SymbolKind.Macro
                        ? KnownMonikers.MacroPublic
                    : kind == SymbolKind.Specifier
                            ? KnownMonikers.IntellisenseKeyword
                            : kind == SymbolKind.Header
                                ? KnownMonikers.CPPHeaderFile
                                : KnownMonikers.Field;
        }
    }
}
