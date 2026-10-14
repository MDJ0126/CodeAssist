using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UEAssist.Semantics
{
    public sealed class TextReplacement
    {
        public TextReplacement(int start, int length, string text) { Start = start; Length = length; Text = text; }
        public int Start { get; }
        public int Length { get; }
        public string Text { get; }
    }
    public static class WorkspaceEdits
    {
        public static string Apply(string text, JArray edits)
        {
            foreach (var edit in Plan(text, edits).Reverse()) text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Text);
            return text;
        }
        public static TextReplacement[] Plan(string text, JArray edits)
        {
            var replacements = new List<TextReplacement>();
            foreach (var edit in edits)
            {
                var start = Offset(text, (int)edit["range"]["start"]["line"], (int)edit["range"]["start"]["character"]);
                var end = Offset(text, (int)edit["range"]["end"]["line"], (int)edit["range"]["end"]["character"]);
                if (end < start) throw new ArgumentException("Reversed edit range");
                replacements.Add(new TextReplacement(start, end - start, (string)edit["newText"] ?? ""));
            }
            var sorted = replacements.OrderBy(edit => edit.Start).ToArray();
            for (var i = 1; i < sorted.Length; i++)
                if (sorted[i].Start < sorted[i - 1].Start + sorted[i - 1].Length || sorted[i].Start == sorted[i - 1].Start)
                    throw new ArgumentException("Overlapping or ambiguous edits");
            return sorted;
        }
        public static int Offset(string text, int line, int character)
        {
            if (line < 0 || character < 0) throw new ArgumentOutOfRangeException();
            var offset = 0;
            for (var i = 0; i < line; i++)
            {
                var newline = text.IndexOf('\n', offset);
                if (newline < 0) throw new ArgumentOutOfRangeException(nameof(line)); offset = newline + 1;
            }
            var end = text.IndexOf('\n', offset); if (end < 0) end = text.Length;
            if (end > offset && text[end - 1] == '\r') end--;
            if (character > end - offset) throw new ArgumentOutOfRangeException(nameof(character));
            return offset + character;
        }
        public static IDictionary<string, JArray> Files(JToken workspaceEdit)
        {
            var files = new Dictionary<string, JArray>(StringComparer.OrdinalIgnoreCase);
            if (workspaceEdit?["changes"] is JObject changes)
                foreach (var property in changes.Properties()) Add(files, property.Name, (JArray)property.Value);
            if (workspaceEdit?["documentChanges"] is JArray documents)
                foreach (var document in documents)
                {
                    if (document["kind"] != null) throw new NotSupportedException("File creation, renaming, and deletion require a separate operation");
                    if (document["textDocument"]?["version"]?.Type == JTokenType.Integer)
                        throw new NotSupportedException("Versioned document edits require matching LSP document versions");
                    Add(files, (string)document["textDocument"]["uri"], (JArray)document["edits"]);
                }
            return files;
        }
        private static void Add(IDictionary<string, JArray> files, string uriText, JArray edits)
        {
            if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri) || !uri.IsFile) throw new ArgumentException("Non-file edit URI");
            if (files.ContainsKey(uri.LocalPath)) throw new ArgumentException("Duplicate document edit group");
            files.Add(uri.LocalPath, edits);
        }
    }
}
