using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
namespace CodeAssist.Core
{
    public static class UiText
    {
        private static readonly Lazy<Dictionary<string, Dictionary<string,string>>> Table = new Lazy<Dictionary<string, Dictionary<string,string>>>(Load);
        public static string[] LanguageCodes => Table.Value.Values.SelectMany(v => v.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.Ordinal).ToArray();
        public static string LanguageName(string code)
        { try { return CultureInfo.GetCultureInfo(code).NativeName; } catch (CultureNotFoundException) { return code; } }
        public static string Get(string source, string language)
        {
            if (source == null) return string.Empty;
            if (!Table.Value.TryGetValue(source, out var values)) return source;
            var code = string.IsNullOrWhiteSpace(language) || language == "auto" ? CultureInfo.CurrentUICulture.Name : language;
            if (values.TryGetValue(code, out var exact)) return exact;
            var split = code.IndexOf('-');
            if (split > 0 && values.TryGetValue(code.Substring(0, split), out var neutral)) return neutral;
            return values.TryGetValue("en", out var fallback) ? fallback : source;
        }
        private static Dictionary<string, Dictionary<string,string>> Load()
        {
            using (var stream = typeof(UiText).Assembly.GetManifestResourceStream("CodeAssist.Core.Localization.ui.xml"))
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                return XDocument.Load(reader).Root.Elements("entry").ToDictionary(e => (string)e.Attribute("source"), e => e.Elements("text").ToDictionary(t => (string)t.Attribute("lang"), t => t.Value, StringComparer.OrdinalIgnoreCase), StringComparer.Ordinal);
        }
    }
}