using CodeAssist.Core;
using System;
using System.Linq;
namespace BeforeCore
{
    // VA result filter: whitespace joins terms; commas separate alternatives.
    public static class ReferenceFilter
    {
        public static bool Matches(string text, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            return query.Split(',').Where(part => !string.IsNullOrWhiteSpace(part)).Any(part =>
                part.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                    .All(term => (text ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
        }
    }
}
