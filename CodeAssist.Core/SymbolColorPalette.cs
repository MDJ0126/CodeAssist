using System;
namespace CodeAssist.Core
{
    // Shared symbol palette for editor-adjacent surfaces. Unknown/file rows inherit IDE ink.
    public static class SymbolColorPalette
    {
        public static string Resolve(StartupPreferences preferences, SymbolKind kind, bool light)
        {
            if (preferences == null) throw new ArgumentNullException(nameof(preferences));
            string configured, fallback;
            switch (kind)
            {
                case SymbolKind.Type: configured=preferences.TypeColor; fallback="#FFD700"; break;
                case SymbolKind.Function: configured=preferences.FunctionColor; fallback="#FF8000"; break;
                case SymbolKind.Variable: configured=preferences.VariableColor; fallback="#BDB76B"; break;
                case SymbolKind.Macro:
                case SymbolKind.Specifier: configured=preferences.MacroColor; fallback="#BD63C5"; break;
                default: return null;
            }
            return DefaultSemanticPalette.Resolve(configured, fallback, light);
        }
    }
}
