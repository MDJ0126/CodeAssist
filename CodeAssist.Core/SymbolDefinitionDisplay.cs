using System;
namespace CodeAssist.Core {
 public static class SymbolDefinitionDisplay {
  public static string FromLine(string line,string fallback,string nextCodeLine=null) {
   if(string.IsNullOrWhiteSpace(line))return fallback??string.Empty;
   var text=line.Trim();var mask=CppCodeMask.Create(text);var brace=mask.IndexOf('{');
   if(brace>=0)return text.Substring(0,brace).TrimEnd()+"{...}";
   var comments=CppCodeMask.Comments(text);
   for(var i=0;i+1<text.Length;i++)if(comments[i]=='/'&&comments[i+1]=='/'){text=text.Substring(0,i).TrimEnd();break;}
   if(!mask.TrimEnd().EndsWith(";",StringComparison.Ordinal)&&!string.IsNullOrWhiteSpace(nextCodeLine)&&CppCodeMask.Create(nextCodeLine).TrimStart().StartsWith("{",StringComparison.Ordinal))return text+"{...}";
   return text.TrimEnd(';').TrimEnd();
  }
 }
}
