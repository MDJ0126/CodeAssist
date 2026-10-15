using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System.Windows.Media;
using System;
namespace CodeAssist.Extension
{
    internal static class RecommendedColors
    {
        internal static void Apply(IComponentModel components)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!StartupUi.Preferences.ColoringEnabled || components == null) return;
            var service = components.GetService<IClassificationFormatMapService>();
            var registry = components.GetService<IClassificationTypeRegistryService>();
            if (service == null || registry == null) return;
            var map = service.GetClassificationFormatMap("text");
            map.BeginBatchUpdate();
            try
            {
                Set(map, registry, CppSemanticClassification.TypeName, Parse(StartupUi.Preferences.TypeColor, "#FFD700"));
                Set(map, registry, CppSemanticClassification.VariableName, Parse(StartupUi.Preferences.VariableColor, "#BDB76B"));
                Set(map, registry, CppSemanticClassification.FunctionName, Parse(StartupUi.Preferences.FunctionColor, "#FF8000"));
                Set(map, registry, UnrealMacroClassification.Name, Parse(StartupUi.Preferences.MacroColor, "#BD63C5"));
            }
            finally { map.EndBatchUpdate(); }
        }
        private static Color Parse(string value, string fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(value); }
            catch (Exception error) when (error is FormatException || error is NotSupportedException || error is ArgumentException)
            { return (Color)ColorConverter.ConvertFromString(fallback); }
        }
        private static void Set(IClassificationFormatMap map, IClassificationTypeRegistryService registry, string name, Color color)
        {
            var type = registry.GetClassificationType(name);
            if (type == null) return;
            var brush = new SolidColorBrush(color); brush.Freeze();
            map.SetTextProperties(type, map.GetTextProperties(type).SetForeground(color));
        }
    }
}