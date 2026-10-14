using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeAssist.Core
{
    public sealed class SnippetTemplate
    {
        public SnippetTemplate(string title, string shortcut, string body) { Title = title; Shortcut = shortcut; Body = body; }
        public string Title { get; }
        public string Shortcut { get; }
        public string Body { get; }
        public override string ToString() => string.IsNullOrEmpty(Title) ? Shortcut : Title;
        public static IReadOnlyList<SnippetTemplate> Parse(string value)
        {
            var snippets = new List<SnippetTemplate>();
            foreach (var chunk in (value ?? "").Split('\f'))
            {
                var content = chunk.TrimStart('\r', '\n');
                var newline = content.IndexOf('\n');
                if (newline < 0) continue;
                var header = Regex.Match(content.Substring(0, newline).TrimEnd('\r'), @"^a:(?<title>.*):(?<shortcut>[^:]*):$");
                if (header.Success) snippets.Add(new SnippetTemplate(header.Groups["title"].Value,
                    header.Groups["shortcut"].Value, content.Substring(newline + 1)));
            }
            return snippets;
        }
        public string[] Variables()
        {
            var names = new List<string>();
            for (var i = 0; i < Body.Length; i++)
            {
                if (Body[i] != '$') continue;
                if (i + 1 < Body.Length && Body[i + 1] == '$') { i++; continue; }
                var close = Body.IndexOf('$', i + 1);
                if (close < 0) break;
                var name = Body.Substring(i + 1, close - i - 1);
                if (name != "end" && Regex.IsMatch(name, @"^[A-Za-z_]\w*$") && !names.Contains(name)) names.Add(name);
                i = close;
            }
            return names.ToArray();
        }
        public SnippetExpansion Expand(IReadOnlyDictionary<string, string> values)
        {
            var result = new StringBuilder(); var caret = -1;
            var missing = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Body.Length;)
            {
                if (Body[i] != '$') { result.Append(Body[i++]); continue; }
                if (i + 1 < Body.Length && Body[i + 1] == '$') { result.Append('$'); i += 2; continue; }
                var close = Body.IndexOf('$', i + 1);
                if (close < 0) { result.Append(Body[i++]); continue; }
                var name = Body.Substring(i + 1, close - i - 1);
                if (name == "end") { if (caret < 0) caret = result.Length; }
                else if (values != null && values.TryGetValue(name, out var replacement)) result.Append(replacement);
                else { missing.Add(name); result.Append(Body, i, close - i + 1); }
                i = close + 1;
            }
            return new SnippetExpansion(result.ToString(), caret < 0 ? result.Length : caret, missing.ToArray());
        }
    }
    public sealed class SnippetExpansion
    {
        public SnippetExpansion(string text, int caret, string[] missing) { Text = text; Caret = caret; Missing = missing; }
        public string Text { get; }
        public int Caret { get; }
        public string[] Missing { get; }
    }
}
