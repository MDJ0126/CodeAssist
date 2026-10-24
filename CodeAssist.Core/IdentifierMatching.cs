using System;
using System.Text;
namespace CodeAssist.Core
{
    public static class IdentifierMatching
    {
        public static string Acronym(string name)
        {
            var result=new StringBuilder();name=name??"";
            for(var i=0;i<name.Length;i++)if(char.IsLetterOrDigit(name[i])&&(i==0||name[i-1]=='_'||char.IsLower(name[i-1])&&char.IsUpper(name[i])||char.IsUpper(name[i])&&i+1<name.Length&&char.IsLower(name[i+1])&&char.IsUpper(name[i-1])))result.Append(name[i]);
            return result.ToString();
        }
        public static int Rank(string name,string query,bool acronyms,bool shorthand)
        {
            if(string.IsNullOrEmpty(name))return int.MaxValue;query=query??"";
            if(name.Equals(query,StringComparison.Ordinal))return 0;
            if(name.StartsWith(query,StringComparison.OrdinalIgnoreCase))return 1;
            if(query.Length<2||name.Length>512)return int.MaxValue;
            if(acronyms&&Acronym(name).StartsWith(query,StringComparison.OrdinalIgnoreCase))return 2;
            if(shorthand&&char.ToUpperInvariant(name[0])==char.ToUpperInvariant(query[0]))
            {var position=0;foreach(var c in name)if(position<query.Length&&char.ToUpperInvariant(c)==char.ToUpperInvariant(query[position]))position++;if(position==query.Length)return 3;}
            return int.MaxValue;
        }
    }
}
