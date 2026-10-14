using UEAssist.Core;

namespace UEAssist.Indexing
{
    public sealed class IndexedSymbol
    {
        public IndexedSymbol(string name, SymbolKind kind, string filePath, int line, int column, string ownerType = null, string valueType = null, string baseType = null, string scopeId = null, string signature = null, bool isLocal = false)
        {
            Name = name;
            Kind = kind;
            FilePath = filePath;
            Line = line;
            Column = column;
            OwnerType = ownerType ?? string.Empty;
            ValueType = valueType ?? string.Empty;
            BaseType = baseType ?? string.Empty;
            ScopeId = scopeId ?? string.Empty;
            Signature = signature ?? string.Empty;
            IsLocal = isLocal;
        }

        public string Name { get; }
        public SymbolKind Kind { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }
        public string OwnerType { get; }
        public string ValueType { get; }
        public string BaseType { get; }
        public string ScopeId { get; }
        public string Signature { get; }
        public bool IsLocal { get; }
        public string Identity => string.Join("|", FilePath.ToUpperInvariant(), ScopeId, OwnerType, Kind, Name, Signature, Line, Column);
    }
}
