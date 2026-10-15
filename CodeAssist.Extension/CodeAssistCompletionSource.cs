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
            if (!StartupUi.Preferences.CompletionEnabled) return;
            if (disposed || indexService.Index.Count == 0) return;
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
                        : ResolveLocalVariableType(snapshot, receiver, point.Value.Position) ?? indexService.Index.ResolveVariableType(currentOwner, receiver);
                hasResolvedMemberContext = !string.IsNullOrWhiteSpace(typeName);
                candidates = hasResolvedMemberContext
                    ? indexService.Index.CompleteMembers(NormalizeType(typeName), prefix, 100)
                    : receiver == "Super"
                        ? indexService.Index.Complete(prefix, 100)
                        : Array.Empty<IndexedSymbol>();
            }
            else
            {
                prefix = GetIdentifierPrefix(beforeCaret);
                candidates = MergeCandidates(
                    indexService.Index.Complete(prefix, 200),
                    indexService.CompleteLive(prefix, 200, filePath).Where(item => !item.IsLocal ||
                        ResolveLocalVariableType(snapshot, item.Name, point.Value.Position) != null).ToArray());
            }


            if (!includeMatch.Success && !UnrealSpecifierContext.IsMatch(beforeCaret))
            {
                RequestCompilerCompletion(session, snapshot, point.Value.Position, prefix);
                if (ReferenceEquals(compilerSnapshot, snapshot) && compilerPosition == point.Value.Position &&
                    string.Equals(compilerRoot, indexService.ProjectRoot, StringComparison.OrdinalIgnoreCase))
                    candidates = compilerCandidates.Concat(candidates).GroupBy(item => item.Name + "|" + item.Signature, StringComparer.Ordinal)
                        .Select(group => group.First()).ToArray();
            }
            candidates = RankByRecentUse(candidates, snapshot, point.Value.Position).Take(100).ToArray();

            if (candidates.Count == 0) return;
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
                if (indexService.IntelliSenseReady && previewItems.Count == 0) return;
                if (previewItems.Count == 0) return;

                // Never display VCCompletionSet and CodeAssist CompletionSet together.
                // Visual Studio 17.14 can crash inside VCCompletionSet's highlight
                // callback while WPF realizes items from the mixed session.
                var combined = candidates.Select(CreateCompletion).ToList();
                foreach (var native in builtInSet.Completions)
                {
                    if (combined.Any(item => string.Equals(item.InsertionText, native.InsertionText, StringComparison.OrdinalIgnoreCase))) continue;
                    combined.Add(CloneCompletion(native));
                    if (combined.Count >= 200) break;
                }
                completionSets.Clear();
                completionSets.Add(new PreviewCompletionSet(applicable, combined));
                MarkCodeAssistSession(session);
                return;
            }

            var completions = candidates.Select(CreateCompletion).ToList();
            completionSets.Add(new PreviewCompletionSet(applicable, completions));
            MarkCodeAssistSession(session);
        }

        private void RequestCompilerCompletion(ICompletionSession session, ITextSnapshot snapshot, int position, string prefix)
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
                    var found = SemanticCompletions.Read(result, text, position - prefix.Length, position).Select(item =>
                        new IndexedSymbol(item.Name, item.Kind == 2 || item.Kind == 3 || item.Kind == 4 ? SymbolKind.Function :
                            item.Kind == 7 || item.Kind == 8 || item.Kind == 13 || item.Kind == 22 || item.Kind == 25 ? SymbolKind.Type :
                            item.Kind == 5 || item.Kind == 6 || item.Kind == 10 || item.Kind == 21 ? SymbolKind.Variable : SymbolKind.Unknown,
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

        private static Completion CreateCompletion(IndexedSymbol symbol)
        {
            var owner = string.IsNullOrWhiteSpace(symbol.OwnerType) ? string.Empty : " — " + symbol.OwnerType;
            return new ColoredCompletion(symbol.Kind,
                symbol.Name,
                symbol.Name,
                symbol.Kind + owner + (symbol.Signature.Length > 0 ? "\n" + symbol.Signature : "") + " (CodeAssist 미리보기)",
                CompletionIcons.For(symbol.Kind),
                symbol.Kind.ToString(),
                null,
                "");
        }

        private static Completion CloneCompletion(Completion completion)
        {
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
            if (!session.Properties.ContainsProperty(CodeAssistCompletionSessionState.Marker))
            {
                session.Properties.AddProperty(CodeAssistCompletionSessionState.Marker, true);
                CompletionPopupColors.Connect(session);
            }
        }
    }

    internal static class CodeAssistCompletionSessionState
    {
        internal static readonly object Marker = new object();
    }

    internal sealed class PreviewCompletionSet : CompletionSet
    {
        public PreviewCompletionSet(ITrackingSpan applicableTo, IList<Completion> completions)
            : base("CodeAssistPreview", "CodeAssist 미리보기", applicableTo, completions, null)
        {
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
