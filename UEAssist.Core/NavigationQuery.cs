using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace UEAssist.Core
{
    /// <summary>Substring filters observed in VA 2630's Open File dialog.</summary>
    public sealed class NavigationQuery
    {
        private readonly string[][] alternatives;
        private readonly bool searchPath;
        public int? Line { get; }
        public NavigationQuery(string query)
        {
            query = (query ?? string.Empty).Trim();
            var lineMatch = Regex.Match(query, @":(?<line>[0-9]+)$", RegexOptions.CultureInvariant);
            if (lineMatch.Success && int.TryParse(lineMatch.Groups["line"].Value, out var line) && line > 0)
            {
                Line = line;
                query = query.Substring(0, lineMatch.Index);
            }
            searchPath = query.StartsWith("\\", StringComparison.Ordinal);
            if (searchPath) query = query.Substring(1);
            alternatives = query.Split(',').Select(part => part.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
        }
        public bool Matches(string name, string path)
        {
            var text = searchPath ? path : name;
            text = text ?? string.Empty;
            return alternatives.Any(tokens => tokens.All(token =>
            {
                var exclude = token.StartsWith("-", StringComparison.Ordinal);
                if (exclude) token = token.Substring(1);
                return exclude ? !MatchToken(text, token) : MatchToken(text, token);
            }));
        }
        private static bool MatchToken(string text, string token)
        {
            var begin = token.StartsWith(".", StringComparison.Ordinal);
            var end = token.EndsWith(".", StringComparison.Ordinal);
            token = token.Trim('.');
            if (token.Length == 0) return true;
            if (begin && end) return string.Equals(text, token, StringComparison.OrdinalIgnoreCase);
            if (begin) return text.StartsWith(token, StringComparison.OrdinalIgnoreCase) ||
                text.IndexOf("." + token, StringComparison.OrdinalIgnoreCase) >= 0;
            if (end) return text.EndsWith(token, StringComparison.OrdinalIgnoreCase);
            return text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
