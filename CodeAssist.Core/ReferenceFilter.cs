using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeAssist.Core
{
    public static class ReferenceFilter
    {
        public static ReferenceFilterQuery Compile(string query) => new ReferenceFilterQuery(query);
        public static bool Matches(string text,string query) => Compile(query).Matches(text);
    }
    public sealed class ReferenceFilterQuery
    {
        private readonly string[][] alternatives;
        private readonly bool all;
        public IReadOnlyList<string> Terms { get; }
        internal ReferenceFilterQuery(string query)
        {
            query=query??"";all=string.IsNullOrWhiteSpace(query);
            alternatives=query.Split(',').Where(part=>!string.IsNullOrWhiteSpace(part))
                .Select(part=>part.Split((char[])null,StringSplitOptions.RemoveEmptyEntries)).ToArray();
            Terms=Array.AsReadOnly(alternatives.SelectMany(part=>part).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        }
        public bool Matches(string text)
        {
            if(all)return true;text=text??"";
            foreach(var group in alternatives){var match=true;foreach(var term in group)if(text.IndexOf(term,StringComparison.OrdinalIgnoreCase)<0){match=false;break;}if(match)return true;}
            return false;
        }
    }
}
