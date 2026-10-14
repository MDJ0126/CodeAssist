using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using UEAssist.Core;

namespace UEAssist.Extension
{
    internal static class UnrealMacroClassification
    {
        public const string Name = "UEAssist Unreal Macro";

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(Name)]
        internal static ClassificationTypeDefinition TypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Name)]
        [Name(Name)]
        [UserVisible(true)]
        [Order(After = Priority.Default)]
        internal sealed class FormatDefinition : ClassificationFormatDefinition
        {
            public FormatDefinition()
            {
                DisplayName = "UEAssist Unreal Macro";
                ForegroundColor = Color.FromRgb(190, 183, 255);
            }
        }
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("C/C++")]
    internal sealed class UnrealMacroClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null;

        [Import]
        internal CppColorSynchronizer ColorSynchronizer = null;

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            ColorSynchronizer.Synchronize();
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new UnrealMacroClassifier(
                    textBuffer,
                    ClassificationRegistry.GetClassificationType(UnrealMacroClassification.Name)));
        }
    }

    internal sealed class UnrealMacroClassifier : IClassifier
    {
        private readonly ITextBuffer textBuffer;
        private readonly IClassificationType classificationType;
        private ITextSnapshot cachedSnapshot;
        private IReadOnlyList<MacroOccurrence> cachedMacros;
        private readonly object cacheGate = new object();

        public UnrealMacroClassifier(ITextBuffer textBuffer, IClassificationType classificationType)
        {
            this.textBuffer = textBuffer;
            this.classificationType = classificationType;
            textBuffer.Changed += OnBufferChanged;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var results = new List<ClassificationSpan>();
            var snapshot = span.Snapshot;
            IReadOnlyList<MacroOccurrence> macros;
            lock (cacheGate)
            {
                if (!ReferenceEquals(cachedSnapshot, snapshot))
                {
                    cachedMacros = UnrealMacroParser.Find(snapshot.GetText());
                    cachedSnapshot = snapshot;
                }
                macros = cachedMacros;
            }
            foreach (var macro in macros)
            {
                var macroSpan = new SnapshotSpan(snapshot, macro.Start, macro.Length);
                if (macroSpan.IntersectsWith(span))
                    results.Add(new ClassificationSpan(macroSpan, classificationType));
            }

            return results;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.After.Length == 0)
            {
                return;
            }

            ClassificationChanged?.Invoke(
                this,
                new ClassificationChangedEventArgs(new SnapshotSpan(e.After, 0, e.After.Length)));
        }
    }
}
