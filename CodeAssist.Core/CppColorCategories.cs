using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace CodeAssist.Core {
 public static class CppColorCategories {
  public static IReadOnlyList<SemanticToken> Parse(string source) {
   var code=CppCodeMask.Create(source??"");var names=new Dictionary<string,SemanticTokenKind>(StringComparer.Ordinal);
   foreach(Match m in Regex.Matches(code,@"\bnamespace\s+([A-Za-z_]\w*(?:\s*::\s*[A-Za-z_]\w*)*)\s*\{",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50)))
    foreach(Match name in Regex.Matches(m.Groups[1].Value,@"[A-Za-z_]\w*"))names[name.Value]=SemanticTokenKind.Namespace;
   foreach(Match m in Regex.Matches(code,@"\benum\s+(?:(?:class|struct)\s+)?(?:[A-Za-z_]\w*)?\s*(?::[^;{]+)?\{",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50))) {
    var start=m.Index+m.Length;var depth=1;var end=start;
    for(;end<code.Length&&depth>0;end++){if(code[end]=='{')depth++;else if(code[end]=='}')depth--;}
    if(depth!=0)continue;
    foreach(var part in Members(code,start,end-1)){var name=Regex.Match(part,@"^\s*([A-Za-z_]\w*)");if(name.Success)names[name.Groups[1].Value]=SemanticTokenKind.EnumMember;}
   }
   var result=new List<SemanticToken>();foreach(Match m in Regex.Matches(code,@"\b[A-Za-z_]\w*\b",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50)))
    if(names.TryGetValue(m.Value,out var kind))result.Add(new SemanticToken(m.Value,m.Index,m.Length,kind));
   return result;
  }
  private static IEnumerable<string> Members(string code,int start,int end) {
   int parentheses=0,brackets=0,braces=0,part=start;
   for(var i=start;i<end;i++) {
    switch(code[i]) {
     case '(':parentheses++;break;case ')':parentheses--;break;
     case '[':brackets++;break;case ']':brackets--;break;
     case '{':braces++;break;case '}':braces--;break;
     case ',':if(parentheses==0&&brackets==0&&braces==0){yield return code.Substring(part,i-part);part=i+1;}break;
    }
   }
   yield return code.Substring(part,end-part);
  }
 }
}
