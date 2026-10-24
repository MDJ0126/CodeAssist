using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeAssist.Core
{
    public sealed class ReferenceLocation
    {
        public ReferenceLocation(string file,int line,int column,string fallback="") {File=file;Line=line;Column=column;Fallback=fallback??"";}
        public string File {get;} public int Line {get;} public int Column {get;} public string Fallback {get;}
    }
    public sealed class ReferenceDisplayRow
    {
        internal ReferenceDisplayRow(int index,ReferenceLocation location,string caption){Index=index;File=location.File;Line=location.Line;Column=location.Column;Caption=caption;}
        public int Index {get;} public string File {get;} public int Line {get;} public int Column {get;} public string Caption {get;}
    }
    // Immutable captions for one search revision. Filtering and cloning do not read source documents.
    public sealed class ReferenceDisplaySnapshot
    {
        private ReferenceDisplaySnapshot(ReferenceDisplayRow[] rows,int documents){Rows=Array.AsReadOnly(rows);DocumentCount=documents;}
        public IReadOnlyList<ReferenceDisplayRow> Rows {get;}
        public int DocumentCount {get;}
        public static ReferenceDisplaySnapshot Create(IEnumerable<ReferenceLocation> source,Func<string,string> readDocument)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));if(readDocument==null)throw new ArgumentNullException(nameof(readDocument));
            var indexed=source.Select((location,index)=>new {location,index}).ToArray();var rows=new ReferenceDisplayRow[indexed.Length];var documents=0;
            foreach(var group in indexed.GroupBy(item=>item.location.File,StringComparer.OrdinalIgnoreCase))
            {
                var text=(readDocument(group.Key)??"").Replace("\r\n","\n");documents++;
                var lines=text.Length==0?Array.Empty<string>():text.Split('\n');var starts=new int[lines.Length];
                for(var n=1;n<lines.Length;n++)starts[n]=starts[n-1]+lines[n-1].Length+1;
                var contexts=new ReferenceContextMap(text);
                foreach(var item in group)
                {
                    var location=item.location;var valid=location.Line>0&&location.Line<=lines.Length;
                    var snippet=valid?lines[location.Line-1].Trim():location.Fallback;
                    var offset=valid?starts[location.Line-1]+Math.Max(0,location.Column-1):0;
                    var owner=contexts.At(offset);var caption=(owner.Length>0?owner+" ":"")+"("+location.Line+"): "+snippet;
                    rows[item.index]=new ReferenceDisplayRow(item.index,location,caption);
                }
            }
            return new ReferenceDisplaySnapshot(rows,documents);
        }
    }
}
