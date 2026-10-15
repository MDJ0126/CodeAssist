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
        public SnippetTemplate WithEnvironment(Func<string, string> lookup)
        {
            var body = Regex.Replace(Body, @"%([^%\r\n]+)%", match => lookup?.Invoke(match.Groups[1].Value) ?? "");
            return new SnippetTemplate(Title, Shortcut, body);
        }
        private static string BaseName(string token, out string modifier, out string initial)
        {
            var equal = token.IndexOf('=');
            initial = equal < 0 ? null : token.Substring(equal + 1);
            var name = equal < 0 ? token : token.Substring(0, equal);
            modifier = null;
            foreach (var candidate in new[] { "Upper", "Lower", "Pascal", "Camel" })
                if (name.EndsWith("_" + candidate, StringComparison.Ordinal))
                { modifier = candidate; return name.Substring(0, name.Length - candidate.Length - 1); }
            return name;
        }
        private IEnumerable<string> Tokens()
        {
            for (var i = 0; i < Body.Length; i++)
            {
                if (Body[i] != '$') continue;
                if (i + 1 < Body.Length && Body[i + 1] == '$') { i++; continue; }
                var close = Body.IndexOf('$', i + 1); if (close < 0) yield break;
                yield return Body.Substring(i + 1, close - i - 1); i = close;
            }
        }
        public SnippetInput[] Inputs(IReadOnlyDictionary<string, string> reserved = null)
        {
            var fields = new Dictionary<string, SnippetInput>(StringComparer.Ordinal);
            var names = new List<string>();
            foreach (var token in Tokens())
            {
                var name = BaseName(token, out _, out var initial);
                if (name.Length == 0 || name == "end" || reserved != null && (reserved.ContainsKey(token) || reserved.ContainsKey(name))) continue;
                if (!fields.TryGetValue(name, out var field))
                { fields[name] = new SnippetInput(name, initial); names.Add(name); }
                else if (field.DefaultValue == null && initial != null) fields[name] = new SnippetInput(name, initial);
            }
            return names.Select(name => fields[name]).ToArray();
        }
        public string[] Variables() => Inputs().Select(field => field.Name).ToArray();
        private static string Modify(string value, string modifier)
        {
            if (modifier == "Upper") return value.ToUpperInvariant();
            if (modifier == "Lower") return value.ToLowerInvariant();
            if (value.Length == 0) return value;
            if (modifier == "Pascal") return char.ToUpperInvariant(value[0]) + value.Substring(1);
            if (modifier == "Camel") return char.ToLowerInvariant(value[0]) + value.Substring(1);
            return value;
        }
        public SnippetExpansion Expand(IReadOnlyDictionary<string, string> values)
        {
            var defaults = Inputs(values).Where(field => field.DefaultValue != null).ToDictionary(field => field.Name, field => field.DefaultValue, StringComparer.Ordinal);
            var result = new StringBuilder(); var caret = -1;
            var missing = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Body.Length;)
            {
                if (Body[i] != '$') { result.Append(Body[i++]); continue; }
                if (i + 1 < Body.Length && Body[i + 1] == '$') { result.Append('$'); i += 2; continue; }
                var close = Body.IndexOf('$', i + 1);
                if (close < 0) { result.Append(Body[i++]); continue; }
                var token = Body.Substring(i + 1, close - i - 1);
                var name = BaseName(token, out var modifier, out _);
                if (token == "end") { if (caret < 0) caret = result.Length; }
                else if (values != null && values.TryGetValue(token, out var exact)) result.Append(exact);
                else if (values != null && values.TryGetValue(name, out var replacement)) result.Append(Modify(replacement, modifier));
                else if (defaults.TryGetValue(name, out var initial)) result.Append(Modify(initial, modifier));
                else { missing.Add(name); result.Append(Body, i, close - i + 1); }
                i = close + 1;
            }
            return new SnippetExpansion(result.ToString(), caret < 0 ? result.Length : caret, missing.ToArray());
        }
    }
    public sealed class SnippetInput
    {
        public SnippetInput(string name, string defaultValue) { Name = name; DefaultValue = defaultValue; }
        public string Name { get; } public string DefaultValue { get; }
    }
    public sealed class SnippetExpansion
    {
        public SnippetExpansion(string text, int caret, string[] missing) { Text = text; Caret = caret; Missing = missing; }
        public string Text { get; }
        public int Caret { get; }
        public string[] Missing { get; }
    }
}
