using System;
using System.Globalization;
namespace CodeAssist.Core
{
    public static class ReferenceResultText
    {
        public static string WindowTitle(string language) => UiText.Get("CodeAssist References", language);
        public static string CloneTitle(string symbol, string language) => string.Format(CultureInfo.CurrentCulture, UiText.Get("CodeAssist Find: {0}", language), symbol);
        public static string Description(string symbol, bool compiler, string language) => symbol + " — " + UiText.Get(compiler ? "Compiler references" : "Reference candidates", language);
        public static string Summary(string symbol, int count, bool compiler, string language) => string.Format(CultureInfo.CurrentCulture,
            UiText.Get(compiler ? "CodeAssist: '{0}' compiler references {1}" : "CodeAssist: '{0}' reference candidates {1} (lexical/local-block scope)", language), symbol, count);
    }
}
