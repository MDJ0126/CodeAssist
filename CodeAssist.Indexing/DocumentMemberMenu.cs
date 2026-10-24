using System;
using System.Linq;
using CodeAssist.Core;
namespace CodeAssist.Indexing
{
    public sealed class DocumentMember
    {
        internal DocumentMember(IndexedSymbol symbol,string caption,int start,int target)
        { Symbol=symbol;Caption=caption;NameStart=start;NavigationOffset=target; }
        public IndexedSymbol Symbol { get; }
        public string Caption { get; }
        public int NameStart { get; }
        public int NavigationOffset { get; }
        public int NameLength => Symbol.Name.Length;
    }
    public static class DocumentMemberMenu
    {
        public static DocumentMember[] Build(string path,string text,bool alphabetical)
        {
            text=text??string.Empty;
            var starts=new System.Collections.Generic.List<int>{0};
            for(var i=0;i<text.Length;i++)if(text[i]=='\n')starts.Add(i+1);
            var items=PersistentSymbolIndex.ReadDocumentSymbols(path,text)
                .Where(s=>s.Kind==SymbolKind.Type||s.Kind==SymbolKind.Function)
                .Where(s=>s.Line>0&&s.Line<=starts.Count)
                .Select(s=>{
                    var offset=starts[s.Line-1]+Math.Max(0,s.Column-1);
                    var target=Math.Min(text.Length,offset+s.Name.Length);
                    var caption=(s.OwnerType.Length==0?"":s.OwnerType+"::")+
                        (s.Kind==SymbolKind.Function?(s.Signature.Length==0?s.Name+"()":s.Signature):s.Name);
                    return new DocumentMember(s,caption,offset,target);
                }).Where(s=>s.NameStart>=0&&s.NameStart+s.NameLength<=text.Length)
                .GroupBy(s=>s.Symbol.Identity).Select(g=>g.First());
            return alphabetical?items.OrderBy(s=>s.Caption,StringComparer.OrdinalIgnoreCase).ThenBy(s=>s.NameStart).ToArray():items.OrderBy(s=>s.NameStart).ToArray();
        }
    }
}
