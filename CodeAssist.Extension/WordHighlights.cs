using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using CodeAssist.Core;

namespace CodeAssist.Extension
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(WordHighlights.Marker)]
    [UserVisible(true)]
    internal sealed class WordHighlightFormat : MarkerFormatDefinition
    {
        public WordHighlightFormat() { DisplayName = "CodeAssist Find Selected"; BackgroundColor = Color.FromRgb(95, 85, 35); }
    }
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("C/C++")]
    [TagType(typeof(TextMarkerTag))]
    internal sealed class WordHighlightProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag =>
            buffer == view.TextBuffer ? WordHighlights.For(view) as ITagger<T> : null;
    }
    internal sealed class WordHighlights : ITagger<TextMarkerTag>
    {
        internal const string Marker = "CodeAssist Find Selected";
        private readonly ITextView view;
        private string word;
        private ITextSnapshot parsed;
        private int[] offsets = Array.Empty<int>();
        private readonly object gate = new object();
        private WordHighlights(ITextView view)
        {
            this.view = view; view.TextBuffer.Changed += OnChanged; view.Closed += OnClosed;
        }
        internal static WordHighlights For(ITextView view) => view.Properties.GetOrCreateSingletonProperty(() => new WordHighlights(view));
        internal bool Active { get { lock (gate) return !string.IsNullOrEmpty(word); } }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        internal void Activate()
        {
            var caret = view.Caret.Position.BufferPosition;
            var line = caret.GetContainingLine();
            lock (gate) { word = IdentifierParser.At(line.GetText(), caret.Position - line.Start.Position); parsed = null; }
            Raise();
        }
        internal void Clear() { lock (gate) { word = null; parsed = null; offsets = Array.Empty<int>(); } Raise(); }
        internal bool Navigate(bool next)
        {
            if (!Active || view.IsClosed) return false;
            int[] matches; int length;
            lock (gate) { if (!Active) return false; Parse(view.TextSnapshot); matches = offsets; length = word.Length; }
            if (matches.Length == 0) return true;
            var caret = view.Caret.Position.BufferPosition.Position;
            var target = next ? matches.Where(offset => offset > caret).DefaultIfEmpty(matches[0]).First() :
                matches.Where(offset => offset < caret).DefaultIfEmpty(matches[matches.Length - 1]).Last();
            var span = new SnapshotSpan(view.TextSnapshot, target, length);
            view.Selection.Select(span, false); view.Caret.MoveTo(span.Start); view.ViewScroller.EnsureSpanVisible(span); return true;
        }
        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!Active || spans.Count == 0) yield break;
            int[] matches; int length;
            lock (gate) { if (!Active) yield break; Parse(spans[0].Snapshot); matches = offsets; length = word.Length; }
            foreach (var offset in matches)
            {
                var span = new SnapshotSpan(spans[0].Snapshot, offset, length);
                if (spans.Any(requested => requested.IntersectsWith(span))) yield return new TagSpan<TextMarkerTag>(span, new TextMarkerTag(Marker));
            }
        }
        private void Parse(ITextSnapshot snapshot)
        {
            if (ReferenceEquals(parsed, snapshot)) return;
            offsets = Regex.Matches(snapshot.GetText(), @"\b" + Regex.Escape(word) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                .Cast<Match>().Select(match => match.Index).ToArray(); parsed = snapshot;
        }
        private void Raise() => TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length)));
        private void OnChanged(object sender, TextContentChangedEventArgs args) { lock (gate) parsed = null; if (Active) Raise(); }
        private void OnClosed(object sender, EventArgs args) { view.TextBuffer.Changed -= OnChanged; view.Closed -= OnClosed; lock (gate) { word = null; offsets = Array.Empty<int>(); } }
    }
}
