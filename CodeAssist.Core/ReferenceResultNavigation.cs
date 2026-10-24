using System;
using System.Collections.Generic;
namespace CodeAssist.Core
{
    public static class ReferenceResultNavigation
    {
        public static int Next(IReadOnlyList<ReferenceLocation> rows,int selected,string file,int line,int column,int symbolLength,int direction)
        {
            if(rows==null||rows.Count==0)return -1;
            var index=selected>=0&&selected<rows.Count?selected:-1;
            if(!string.IsNullOrEmpty(file))
                for(var i=0;i<rows.Count;i++)
                    if(string.Equals(rows[i].File,file,StringComparison.OrdinalIgnoreCase)&&rows[i].Line==line&&column>=rows[i].Column&&column<=rows[i].Column+Math.Max(0,symbolLength)) {index=i;break;}
            var step=direction<0?-1:1;
            return index<0?(step>0?0:rows.Count-1):(index+step+rows.Count)%rows.Count;
        }
    }
}
