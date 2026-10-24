using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeAssist.Indexing
{
    public static class QuickInfoSymbolResolver
    {
        public static IndexedSymbol[] Resolve(IEnumerable<IndexedSymbol> candidates,string name,string file,int line,string owner)
        {
            var selected=ContextDefinitionResolver.Resolve(candidates.GroupBy(s=>s.Identity).Select(g=>g.First()),name,file,line,owner);
            return selected==null?Array.Empty<IndexedSymbol>():new[]{selected};
        }
        public static IndexedSymbol[] MergeLiveDeclarations(IEnumerable<IndexedSymbol> persisted,IReadOnlyDictionary<string,IReadOnlyList<IndexedSymbol>> live,string name)
        {
            if(string.IsNullOrEmpty(name))return Array.Empty<IndexedSymbol>();
            var paths=new HashSet<string>(live.Keys,StringComparer.OrdinalIgnoreCase);
            return persisted.Where(s=>!paths.Contains(s.FilePath)).Concat(live.Values.SelectMany(s=>s))
                .Where(s=>s.Name==name&&!s.IsLocal&&!string.IsNullOrWhiteSpace(s.FilePath))
                .GroupBy(s=>s.Identity).Select(g=>g.First()).ToArray();
        }
        public static string DisplaySignature(IndexedSymbol symbol)
        {
            var signature=string.IsNullOrWhiteSpace(symbol.Signature)?symbol.Name:symbol.Signature;
            return (string.IsNullOrWhiteSpace(symbol.ValueType)?signature:symbol.ValueType+" "+signature).Trim();
        }
    }
}
