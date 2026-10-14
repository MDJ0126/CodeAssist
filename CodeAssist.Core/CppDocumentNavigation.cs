using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeAssist.Core
{
    public sealed class DocumentNavigationItem
    {
        public DocumentNavigationItem(string name, int offset, int line) { Name = name; Offset = offset; Line = line; }
        public string Name { get; }
        public int Offset { get; }
        public int Line { get; }
    }
    public static class CppDocumentNavigation
    {
        public static IReadOnlyList<DocumentNavigationItem> Hashtags(string text)
        {
            return Items(CppCodeMask.Comments(text), @"(?<![\w#])#[A-Za-z_][A-Za-z_0-9]*");
        }
        public static IReadOnlyList<DocumentNavigationItem> Includes(string text)
        {
            var results = new List<DocumentNavigationItem>();
            var code = CppCodeMask.Create(text);
            var scanned = 0; var line = 1;
            foreach (Match match in Regex.Matches(text ?? "", "^[ \\t]*#[ \\t]*include[ \\t]*[<\"](?<name>[^>\"\\r\\n]+)[>\"]", RegexOptions.Multiline))
            {
                var hash = text.IndexOf('#', match.Index);
                if (hash >= 0 && code[hash] == '#')
                    results.Add(new DocumentNavigationItem(match.Groups["name"].Value, hash, LineAt(text, hash, ref scanned, ref line)));
            }
            return results;
        }
        private static IReadOnlyList<DocumentNavigationItem> Items(string text, string pattern)
        {
            var result = new List<DocumentNavigationItem>();
            var scanned = 0; var line = 1;
            foreach (Match match in Regex.Matches(text, pattern))
                result.Add(new DocumentNavigationItem(match.Value, match.Index, LineAt(text, match.Index, ref scanned, ref line)));
            return result;
        }
        private static int LineAt(string text, int offset, ref int scanned, ref int line)
        {
            while (scanned < offset) if (text[scanned++] == '\n') line++;
            return line;
        }
    }
}
