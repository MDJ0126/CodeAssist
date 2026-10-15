using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CodeAssist.Core
{
    public static class SnippetReservedValues
    {
        public static Dictionary<string, string> Create(string file, string selected, DateTime now, CultureInfo culture)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            var english = CultureInfo.GetCultureInfo("en-US");
            var values = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["selected"] = selected ?? "", ["FILE"] = file ?? "",
                ["FILE_BASE"] = Path.GetFileNameWithoutExtension(file ?? ""),
                ["FILE_EXT"] = Path.GetExtension(file ?? "").TrimStart('.'),
                ["FILE_PATH"] = string.IsNullOrEmpty(file) ? "" : Path.GetDirectoryName(file) ?? "",
                ["DATE"] = now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
                ["DATE_LOCALE"] = now.ToString("d", culture),
                ["DAY"] = now.Day.ToString(CultureInfo.InvariantCulture),
                ["DAY_02"] = now.ToString("dd", CultureInfo.InvariantCulture),
                ["MONTH"] = now.Month.ToString(CultureInfo.InvariantCulture),
                ["MONTH_02"] = now.ToString("MM", CultureInfo.InvariantCulture),
                ["YEAR"] = now.Year.ToString(CultureInfo.InvariantCulture),
                ["YEAR_02"] = now.ToString("yy", CultureInfo.InvariantCulture),
                ["HOUR"] = now.Hour.ToString(CultureInfo.InvariantCulture),
                ["HOUR_02"] = now.ToString("HH", CultureInfo.InvariantCulture),
                ["MINUTE"] = now.ToString("mm", CultureInfo.InvariantCulture),
                ["SECOND"] = now.ToString("ss", CultureInfo.InvariantCulture),
                ["DAYNAME"] = now.ToString("ddd", culture),
                ["DAYNAME_EN"] = now.ToString("ddd", english),
                ["DAYLONGNAME"] = now.ToString("dddd", culture),
                ["DAYLONGNAME_EN"] = now.ToString("dddd", english),
                ["MONTHNAME"] = now.ToString("MMM", culture),
                ["MONTHNAME_EN"] = now.ToString("MMM", english),
                ["MONTHLONGNAME"] = now.ToString("MMMM", culture),
                ["MONTHLONGNAME_EN"] = now.ToString("MMMM", english)
            };
            foreach (var name in new[] { "FILE", "FILE_BASE", "FILE_EXT", "FILE_PATH", "DATE_LOCALE",
                "DAYNAME", "DAYNAME_EN", "DAYLONGNAME", "DAYLONGNAME_EN", "MONTHNAME", "MONTHNAME_EN", "MONTHLONGNAME", "MONTHLONGNAME_EN" })
            {
                values[name + "_UPPER"] = values[name].ToUpper(culture);
                values[name + "_LOWER"] = values[name].ToLower(culture);
            }
            return values;
        }
        public static void AddWorkspace(Dictionary<string, string> values, string projectFile, string solutionFile, Guid guid, CultureInfo culture)
        {
            foreach (var item in new[] { new { Prefix = "PROJECT", File = projectFile ?? "" }, new { Prefix = "SOLUTION", File = solutionFile ?? "" } })
            {
                values[item.Prefix + "_FILE"] = item.File;
                values[item.Prefix + "_NAME"] = Path.GetFileNameWithoutExtension(item.File);
                values[item.Prefix + "_EXT"] = Path.GetExtension(item.File).TrimStart('.');
                values[item.Prefix + "_PATH"] = item.File.Length == 0 ? "" : Path.GetDirectoryName(item.File) ?? "";
                foreach (var suffix in new[] { "_FILE", "_NAME", "_EXT", "_PATH" })
                {
                    var name = item.Prefix + suffix;
                    values[name + "_UPPER"] = values[name].ToUpper(culture);
                    values[name + "_LOWER"] = values[name].ToLower(culture);
                }
            }
            values["GUID_STRING"] = guid.ToString("D");
            values["GUID_STRING_UPPER"] = values["GUID_STRING"].ToUpperInvariant();
            values["GUID_SYMBOL"] = values["GUID_STRING"].Replace('-', '_');
            values["GUID_SYMBOL_UPPER"] = values["GUID_SYMBOL"].ToUpperInvariant();
        }
    }
}
