using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.Extension
{
    internal static class SymbolStyles
    {
        [Export(typeof(ClassificationTypeDefinition))][Name("CodeAssist Local Bold")][BaseDefinition(CppSemanticClassification.VariableName)]internal static ClassificationTypeDefinition Local=null;
        [Export(typeof(ClassificationTypeDefinition))][Name("CodeAssist System Type")][BaseDefinition(CppSemanticClassification.TypeName)]internal static ClassificationTypeDefinition SystemType=null;
        [Export(typeof(ClassificationTypeDefinition))][Name("CodeAssist System Function")][BaseDefinition(CppSemanticClassification.FunctionName)]internal static ClassificationTypeDefinition SystemFunction=null;
        [Export(typeof(ClassificationTypeDefinition))][Name("CodeAssist System Variable")][BaseDefinition(CppSemanticClassification.VariableName)]internal static ClassificationTypeDefinition SystemVariable=null;
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames="CodeAssist Local Bold")][Name("CodeAssist Local Bold")][UserVisible(true)][Order(After=Priority.High)]
        internal sealed class Bold:ClassificationFormatDefinition {public Bold(){DisplayName="CodeAssist Local Bold";IsBold=true;}}
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames="CodeAssist System Type")][Name("CodeAssist System Type")][UserVisible(true)][Order(After=Priority.High)]
        internal sealed class ItalicType:ClassificationFormatDefinition {public ItalicType(){DisplayName="CodeAssist System Type";IsItalic=true;}}
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames="CodeAssist System Function")][Name("CodeAssist System Function")][UserVisible(true)][Order(After=Priority.High)]
        internal sealed class ItalicFunction:ClassificationFormatDefinition {public ItalicFunction(){DisplayName="CodeAssist System Function";IsItalic=true;}}
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames="CodeAssist System Variable")][Name("CodeAssist System Variable")][UserVisible(true)][Order(After=Priority.High)]
        internal sealed class ItalicVariable:ClassificationFormatDefinition {public ItalicVariable(){DisplayName="CodeAssist System Variable";IsItalic=true;}}
    }
}
