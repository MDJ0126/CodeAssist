using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeAssist.Semantics
{
    public sealed class SemanticCompletion
    {
        public string Name { get; internal set; }
        public string Detail { get; internal set; }
        public int Kind { get; internal set; }
    }
    public static class SemanticCompletions
    {
        public static SemanticCompletion[] Read(JToken result, string text, int expectedStart, int expectedEnd)
        {
            var items = result as JArray ?? (result is JObject ? result["items"] as JArray : null);
            var completions = new List<SemanticCompletion>();
            if (items == null) return completions.ToArray();
            foreach (var item in items)
            {
                if ((int?)item["insertTextFormat"] == 2 || item["additionalTextEdits"] is JArray additional && additional.Count > 0) continue;
                var edit = item["textEdit"];
                if (edit?["range"] == null) continue;
                var range = edit["range"];
                int start, end;
                try
                {
                    start = WorkspaceEdits.Offset(text, (int)range["start"]["line"], (int)range["start"]["character"]);
                    end = WorkspaceEdits.Offset(text, (int)range["end"]["line"], (int)range["end"]["character"]);
                }
                catch (ArgumentException) { continue; }
                catch (FormatException) { continue; }
                var name = (string)edit["newText"];
                if (start != expectedStart || end != expectedEnd || name == null || !Regex.IsMatch(name, @"^[A-Za-z_]\w*$")) continue;
                completions.Add(new SemanticCompletion { Name = name, Kind = (int?)item["kind"] ?? 0,
                    Detail = ((string)item["label"] ?? name).Trim() + "\n" + ((string)item["detail"] ?? "") });
            }
            return completions.ToArray();
        }
    }
}
