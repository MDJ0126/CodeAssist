using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
 public enum DisplayInk { Plain, Type, Function, Variable, Macro, Keyword, Namespace, EnumMember }
 public sealed class DisplayToken {
  public DisplayToken(int start,int length,DisplayInk ink){Start=start;Length=length;Ink=ink;}
  public int Start{get;}public int Length{get;}public DisplayInk Ink{get;}
 }
 public static class SymbolDisplayTokens {
  private static readonly HashSet<string> keywords=new HashSet<string>(StringComparer.Ordinal){"void","bool","char","wchar_t","char16_t","char32_t","short","int","long","signed","unsigned","float","double","const","volatile","static","virtual","override","final","noexcept","class","struct","enum","namespace","public","protected","private","auto","typename","template","using"};
  public static IReadOnlyList<DisplayToken> Parse(string text,SymbolKind terminal=SymbolKind.Unknown,Func<string,SymbolKind> resolve=null){
   text=text??"";var result=new List<DisplayToken>();var matches=Regex.Matches(text,@"\b[A-Za-z_]\w*\b",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));var open=text.IndexOf('(');var close=text.LastIndexOf(')');
   for(int n=0;n<matches.Count;n++){
    var m=matches[n];var after=text.Substring(m.Index+m.Length).TrimStart();var ink=DisplayInk.Plain;
    if(keywords.Contains(m.Value))ink=DisplayInk.Keyword;
    else if(after.StartsWith("::",StringComparison.Ordinal)||after.StartsWith(".",StringComparison.Ordinal))ink=DisplayInk.Type;
    else {
     var known=resolve==null?SymbolKind.Unknown:resolve(m.Value);ink=Kind(known);
     if(ink==DisplayInk.Plain){
      if(after.StartsWith("(",StringComparison.Ordinal))ink=DisplayInk.Function;
      else if(open>=0&&m.Index>open&&(close<0||m.Index<close)){
       // In a declaration-like parameter list, the final identifier is the parameter.
       var next=n+1<matches.Count?matches[n+1]:null;
       ink=next!=null&&next.Index<(close<0?text.Length:close)&&!text.Substring(m.Index+m.Length,next.Index-m.Index-m.Length).Contains(",")?DisplayInk.Type:DisplayInk.Variable;
      } else if(open<0&&n==matches.Count-1)ink=Kind(terminal);
     }
    }
    if(ink!=DisplayInk.Plain)result.Add(new DisplayToken(m.Index,m.Length,ink));
   }
   return result;
  }
  private static DisplayInk Kind(SymbolKind kind){switch(kind){case SymbolKind.Type:return DisplayInk.Type;case SymbolKind.Function:return DisplayInk.Function;case SymbolKind.Variable:return DisplayInk.Variable;case SymbolKind.Macro:case SymbolKind.Specifier:return DisplayInk.Macro;default:return DisplayInk.Plain;}}
 }
}
