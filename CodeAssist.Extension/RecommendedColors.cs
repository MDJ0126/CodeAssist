using CodeAssist.Core;
using Microsoft.VisualStudio.PlatformUI;
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
                Set(map, registry, CppSemanticClassification.TypeName, ThemeColor(StartupUi.Preferences.TypeColor, "#FFD700"));
                Set(map, registry, CppSemanticClassification.VariableName, ThemeColor(StartupUi.Preferences.VariableColor, "#BDB76B"));
                Set(map, registry, CppSemanticClassification.FunctionName, ThemeColor(StartupUi.Preferences.FunctionColor, "#FF8000"));
                Set(map, registry, CppSemanticClassification.NamespaceName, ThemeColor(StartupUi.Preferences.NamespaceColor,"#B9D7A2"));
                Set(map, registry, CppSemanticClassification.EnumMemberName, ThemeColor(StartupUi.Preferences.EnumMemberColor,"#B88020"));
                Set(map, registry, UnrealMacroClassification.Name, ThemeColor(StartupUi.Preferences.MacroColor, "#BD63C5"));
            }
            finally { map.EndBatchUpdate(); }
        }
        internal static Color ThemeColor(string value,string fallback)
        {
            var background=VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var light=(background.R*299+background.G*587+background.B*114)>=128000;
            return Parse(DefaultSemanticPalette.Resolve(Parse(value,fallback).ToString(),fallback,light),fallback);
        }
        internal static Brush DefaultTextBrush(){var c=VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);var b=new SolidColorBrush(Color.FromArgb(c.A,c.R,c.G,c.B));b.Freeze();return b;}
        internal static Brush SymbolBrush(SymbolKind kind)
        {
            var background=VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var light=(background.R*299+background.G*587+background.B*114)>=128000;
            var value=SymbolColorPalette.Resolve(StartupUi.Preferences,kind,light);
            if(value==null)return null;
            var brush=new SolidColorBrush(Parse(value,"#FFD700"));brush.Freeze();return brush;
        }
        internal static string DisplayClassification(DisplayInk ink){switch(ink){case DisplayInk.Type:return CppSemanticClassification.TypeName;case DisplayInk.Function:return CppSemanticClassification.FunctionName;case DisplayInk.Variable:return CppSemanticClassification.VariableName;case DisplayInk.Macro:return UnrealMacroClassification.Name;case DisplayInk.Keyword:return "keyword";case DisplayInk.Namespace:return CppSemanticClassification.NamespaceName;case DisplayInk.EnumMember:return CppSemanticClassification.EnumMemberName;default:return "text";}}
        internal static Brush DisplayBrush(DisplayInk ink){
            switch(ink){case DisplayInk.Type:return SymbolBrush(SymbolKind.Type);case DisplayInk.Function:return SymbolBrush(SymbolKind.Function);case DisplayInk.Variable:return SymbolBrush(SymbolKind.Variable);case DisplayInk.Macro:return SymbolBrush(SymbolKind.Macro);case DisplayInk.Keyword:var brush=new SolidColorBrush(ThemeColor("#569CD6","#569CD6"));brush.Freeze();return brush;default:return null;}
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