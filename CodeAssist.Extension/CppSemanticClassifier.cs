using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using CodeAssist.Core;

namespace CodeAssist.Extension
{
    internal static class CppSemanticClassification
    {
        public const string TypeName = "CodeAssist C++ Type";
        public const string VariableName = "CodeAssist C++ Variable";
        public const string FunctionName = "CodeAssist C++ Function";
        public const string NamespaceName = "CodeAssist C++ Namespace";
        public const string EnumMemberName = "CodeAssist C++ Enum Member";
        [Export(typeof(ClassificationTypeDefinition))][Name(NamespaceName)] internal static ClassificationTypeDefinition NamespaceDefinition=null;
        [Export(typeof(ClassificationTypeDefinition))][Name(EnumMemberName)] internal static ClassificationTypeDefinition EnumDefinition=null;
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames=NamespaceName)][Name(NamespaceName)][UserVisible(true)][Order(After=Priority.High)]
        internal sealed class NamespaceFormat:ClassificationFormatDefinition {public NamespaceFormat(){DisplayName=NamespaceName;ForegroundColor=Color.FromRgb(185,215,162);}}
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames=EnumMemberName)][Name(EnumMemberName)][UserVisible(true)][Order(After=Priority.High)]
        internal sealed class EnumFormat:ClassificationFormatDefinition {public EnumFormat(){DisplayName=EnumMemberName;ForegroundColor=Color.FromRgb(184,128,32);}}

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(TypeName)]
        internal static ClassificationTypeDefinition TypeDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(VariableName)]
        internal static ClassificationTypeDefinition VariableDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FunctionName)]
        internal static ClassificationTypeDefinition FunctionDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeName)]
        [Name(TypeName)]
        [UserVisible(true)]
        [Order(After = Priority.High)]
        internal sealed class TypeFormat : ClassificationFormatDefinition
        {
            public TypeFormat()
            {
                DisplayName = "CodeAssist C++ Type";
                ForegroundColor = Color.FromRgb(255, 215, 0);
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = VariableName)]
        [Name(VariableName)]
        [UserVisible(true)]
        [Order(After = Priority.High)]
        internal sealed class VariableFormat : ClassificationFormatDefinition
        {
            public VariableFormat()
            {
                DisplayName = "CodeAssist C++ Variable";
                ForegroundColor = Color.FromRgb(189, 183, 107);
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = FunctionName)]
        [Name(FunctionName)]
        [UserVisible(true)]
        [Order(After = Priority.High)]
        internal sealed class FunctionFormat : ClassificationFormatDefinition
        {
            public FunctionFormat()
            {
                DisplayName = "CodeAssist C++ Function";
                ForegroundColor = Color.FromRgb(255, 128, 0);
            }
        }
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("C/C++")]
    internal sealed class CppSemanticClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService Registry = null;
        [Import] internal ProjectIndexService Indexes=null;


        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new CppSemanticClassifier(
                    textBuffer,
                    Registry.GetClassificationType(CppSemanticClassification.TypeName),
                    Registry.GetClassificationType(CppSemanticClassification.VariableName),
                    Registry.GetClassificationType(CppSemanticClassification.FunctionName),Registry,Indexes));
        }
    }

    internal sealed class CppSemanticClassifier : IClassifier
    {
        private readonly ITextBuffer textBuffer;
        private readonly IClassificationType typeClassification;
        private readonly IClassificationType variableClassification;
        private readonly IClassificationType functionClassification;
        private readonly IClassificationTypeRegistryService registry;private readonly ProjectIndexService indexes;private CppReferenceContext localContext;
        private ITextSnapshot cachedSnapshot;
        private IReadOnlyList<SemanticToken> cachedTokens = Array.Empty<SemanticToken>();

        public CppSemanticClassifier(ITextBuffer textBuffer, IClassificationType typeClassification, IClassificationType variableClassification, IClassificationType functionClassification,IClassificationTypeRegistryService registry=null,ProjectIndexService indexes=null)
        {
            this.textBuffer = textBuffer;this.registry=registry;this.indexes=indexes;
            this.typeClassification = typeClassification;
            this.variableClassification = variableClassification;
            this.functionClassification = functionClassification;
            textBuffer.Changed += OnBufferChanged;
            var weak = new WeakReference(this);
            Action settingsChanged = null;
            settingsChanged = () =>
            {
                var target = weak.Target as CppSemanticClassifier;
                if (target == null) { StartupUi.SettingsApplied -= settingsChanged; return; }
                target.cachedSnapshot=null;
                var snapshot = target.textBuffer.CurrentSnapshot;
                target.ClassificationChanged?.Invoke(target, new ClassificationChangedEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
            };
            StartupUi.SettingsApplied += settingsChanged;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            if (!StartupUi.Preferences.ColoringEnabled || !StartupUi.Preferences.ColorTextEditors) return new List<ClassificationSpan>();
            EnsureParsed(span.Snapshot);
            var results = new List<ClassificationSpan>();
            foreach (var token in cachedTokens)
            {
                if (token.Start >= span.End.Position || token.Start + token.Length <= span.Start.Position)
                {
                    continue;
                }

                var classification = token.Kind == SemanticTokenKind.Type
                    ? typeClassification
                    : token.Kind == SemanticTokenKind.Function
                        ? functionClassification
                        : variableClassification;
                if(token.Kind==SemanticTokenKind.Namespace)classification=registry?.GetClassificationType(CppSemanticClassification.NamespaceName)??classification;
                else if(token.Kind==SemanticTokenKind.EnumMember)classification=registry?.GetClassificationType(CppSemanticClassification.EnumMemberName)??classification;
                if(StartupUi.Preferences.LocalSymbolsBold&&token.Kind==SemanticTokenKind.Variable&&localContext?.DeclarationAt(token.Name,token.Start)!=null)classification=registry?.GetClassificationType("CodeAssist Local Bold")??classification;
                else if(StartupUi.Preferences.SystemSymbolsItalic&&indexes!=null&&registry!=null&&token.Kind!=SemanticTokenKind.Namespace&&token.Kind!=SemanticTokenKind.EnumMember)
                {
                    var definitions=indexes.Index.FindDefinitions(token.Name);
                    if(definitions.Count>0&&definitions.All(item=>SystemPath(item.FilePath)))classification=registry.GetClassificationType(token.Kind==SemanticTokenKind.Type?"CodeAssist System Type":token.Kind==SemanticTokenKind.Function?"CodeAssist System Function":"CodeAssist System Variable")??classification;
                }
                results.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot, token.Start, token.Length), classification));
            }

            return results;
        }

        private void EnsureParsed(ITextSnapshot snapshot)
        {
            if (ReferenceEquals(snapshot, cachedSnapshot))
            {
                return;
            }

            var text=snapshot.GetText();
            cachedTokens = CppSemanticParser.Parse(text);
            localContext=StartupUi.Preferences.LocalSymbolsBold&&snapshot.Length<=512*1024?new CppReferenceContext(text):null;
            cachedSnapshot = snapshot;
        }

        private static bool SystemPath(string path)=>!string.IsNullOrEmpty(path)&&(path.IndexOf(@"\Program Files",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("/usr/include/",StringComparison.Ordinal)>=0||path.IndexOf(@"\Engine\Source\",StringComparison.OrdinalIgnoreCase)>=0);
        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            cachedSnapshot = null;
            cachedTokens = Array.Empty<SemanticToken>();
            if (e.After.Length > 0)
            {
                ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(new SnapshotSpan(e.After, 0, e.After.Length)));
            }
        }
    }
}
