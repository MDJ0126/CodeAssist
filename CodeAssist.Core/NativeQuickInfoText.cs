using System;
using System.Globalization;
using System.Linq;
namespace CodeAssist.Core
{
    public static class NativeQuickInfoText
    {
        // The C++ provider embeds format markers around semantic identifiers.
        // Compare displayed identifiers without changing the native presentation model.
        public static string Normalize(string text) => text==null?string.Empty:new string(text.Where(c=>char.GetUnicodeCategory(c)!=UnicodeCategory.Format).ToArray());
        public static bool IsSymbolRun(string classification,string text,string word)
        {
            var kind=(classification??string.Empty).ToLowerInvariant();
            var semantic=kind.StartsWith("cpp",StringComparison.Ordinal)||kind=="class name"||kind=="struct name"||kind=="method name"||kind=="field name"||kind=="local name"||kind=="parameter name"||kind=="namespace name"||kind=="enum member name";
            return semantic&&string.Equals(Normalize(text).Trim(),word,StringComparison.Ordinal);
        }
    }
}
