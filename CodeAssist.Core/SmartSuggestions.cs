using System;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    public static class SmartSuggestions
    {
        public static string[] At(string text,int caret)
        {
            if(text==null||caret<0||caret>text.Length)return Array.Empty<string>();
            var start=text.LastIndexOf('\n',Math.Max(0,caret-1));start=start<0?0:start+1;var code=CppCodeMask.Create(text.Substring(start,caret-start));
            if(Regex.IsMatch(code,@"\bbool\s+[A-Za-z_]\w*\s*=\s*\w*$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20)))return new[]{"true","false"};
            if(Regex.IsMatch(code,@"\*\s*(?:const\s+)?[A-Za-z_]\w*\s*=\s*\w*$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20)))return new[]{"nullptr"};
            return Array.Empty<string>();
        }
    }
}
