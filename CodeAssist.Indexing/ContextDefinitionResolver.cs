using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeAssist.Indexing
{
    public static class ContextDefinitionResolver
    {
        public static IndexedSymbol Resolve(IEnumerable<IndexedSymbol> symbols, string name, string activeFile, int line, string owner)
        {
            var candidates = symbols.Where(s => s.Name == name).ToArray();
            var local = candidates.Where(s => string.Equals(s.FilePath, activeFile, StringComparison.OrdinalIgnoreCase)).ToArray();
            var exact = local.Where(s => s.Line == line).ToArray();
            if (exact.Length == 1) return exact[0];
            if (!string.IsNullOrEmpty(owner))
            {
                var owned = candidates.Where(s => s.OwnerType == owner).ToArray();
                if (owned.Length > 0) candidates = owned;
            }
            else if (local.Length > 0) candidates = local;
            // A same-name overload is not evidence of the active expression's type.
            var definitions = candidates.GroupBy(s => s.OwnerType + "|" + s.Kind + "|" + s.Signature + "|" + s.ValueType).ToArray();
            return definitions.Length == 1 ? definitions[0].First() : null;
        }
    }
}
