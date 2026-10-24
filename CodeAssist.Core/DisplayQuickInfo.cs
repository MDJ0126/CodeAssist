using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace CodeAssist.Core {
 public static class DisplayQuickInfo {
  private static readonly TimeSpan Timeout=TimeSpan.FromMilliseconds(40);
  private sealed class Macro {public string Name,Body;public string[] Parameters;}
  private static Dictionary<string,Macro> Macros(string text){
   var result=new Dictionary<string,Macro>(StringComparer.Ordinal);if(text==null||text.Length>1024*1024)return result;
   var logical=Regex.Replace(text,@"\\\r?\n","",RegexOptions.CultureInvariant,Timeout);
   var mask=CppCodeMask.Create(logical);
   foreach(Match match in Regex.Matches(logical,@"^[ \t]*#[ \t]*(?<kind>define|undef)[ \t]+(?<name>[A-Za-z_]\w*)(?<parameters>\([^\r\n)]*\))?[ \t]*(?<body>[^\r\n]*)",RegexOptions.Multiline|RegexOptions.CultureInvariant,Timeout)){
    var hash=logical.IndexOf('#',match.Index);if(hash<0||hash>=mask.Length||mask[hash]!='#')continue;
    var name=match.Groups["name"].Value;if(match.Groups["kind"].Value=="undef"){result.Remove(name);continue;}
    var args=match.Groups["parameters"];var parameters=args.Success?args.Value.Substring(1,args.Value.Length-2).Split(',').Select(v=>v.Trim()).Where(v=>v.Length>0).ToArray():null;
    if(parameters!=null&&parameters.Any(v=>!Regex.IsMatch(v,@"\A[A-Za-z_]\w*\z",RegexOptions.CultureInvariant,Timeout)))continue;
    result[name]=new Macro {Name=name,Body=match.Groups["body"].Value.Trim(),Parameters=parameters};
   }return result;
  }
  private static bool Arguments(string expression,int start,out string[] arguments,out int end){
   var code=CppCodeMask.Create(expression);var depth=0;var from=start+1;var values=new List<string>();end=start;arguments=null;
   for(var i=start;i<code.Length;i++){var c=code[i];if(c=='('||c=='['||c=='{')depth++;if(c==')'||c==']'||c=='}'){depth--;if(depth==0){if(i>from||values.Count>0)values.Add(expression.Substring(from,i-from).Trim());arguments=values.ToArray();end=i+1;return true;}}if(c==','&&depth==1){values.Add(expression.Substring(from,i-from).Trim());from=i+1;}}
   return false;
  }
  private static string ReplaceWord(string text,string word,string replacement){var code=CppCodeMask.Create(text);var matches=Regex.Matches(code,@"\b"+Regex.Escape(word)+@"\b",RegexOptions.CultureInvariant,Timeout);var result=new StringBuilder(text);for(var i=matches.Count-1;i>=0;i--){var match=matches[i];result.Remove(match.Index,match.Length).Insert(match.Index,replacement);}return result.ToString();}
  public static string ExpandMacro(string document,string expression){
   if(expression==null||expression.Length>32768)return null;var macros=Macros(document);var current=expression;
   for(var pass=0;pass<12;pass++){
    var code=CppCodeMask.Create(current);var names=Regex.Matches(code,@"\b[A-Za-z_]\w*\b",RegexOptions.CultureInvariant,Timeout);var changed=false;
    for(var i=names.Count-1;i>=0;i--){var token=names[i];if(!macros.TryGetValue(token.Value,out var macro))continue;var end=token.Index+token.Length;var body=macro.Body;
     if(macro.Parameters!=null){while(end<code.Length&&char.IsWhiteSpace(code[end]))end++;if(end>=code.Length||code[end]!='('||!Arguments(current,end,out var args,out var closing)||args.Length!=macro.Parameters.Length)continue;end=closing;
      for(var n=0;n<args.Length;n++){var key=macro.Parameters[n];var quoted="\""+args[n].Replace("\\","\\\\").Replace("\"","\\\"")+"\"";body=Regex.Replace(body,@"(?<!#)#\s*\b"+Regex.Escape(key)+@"\b",m=>quoted,RegexOptions.CultureInvariant,Timeout);}
      if(macro.Parameters.Length>0){
      var argumentMap=macro.Parameters.Select((name,index)=>new {name,index}).ToDictionary(entry=>entry.name,entry=>args[entry.index],StringComparer.Ordinal);
      var bodyCode=CppCodeMask.Create(body);var placeholders=Regex.Matches(bodyCode,@"\b(?:"+string.Join("|",macro.Parameters.Select(Regex.Escape))+@")\b",RegexOptions.CultureInvariant,Timeout);var rewritten=new StringBuilder(body);for(var index=placeholders.Count-1;index>=0;index--){var placeholder=placeholders[index];rewritten.Remove(placeholder.Index,placeholder.Length).Insert(placeholder.Index,argumentMap[placeholder.Value]);}body=rewritten.ToString();
      }
      body=Regex.Replace(body,@"\s*##\s*","",RegexOptions.CultureInvariant,Timeout);
     }
     current=current.Substring(0,token.Index)+body+current.Substring(end);changed=true;if(current.Length>32768)return null;
     // Re-tokenize after each replacement so overlapping nested calls cannot corrupt offsets.
     break;
    }
    if(!changed)return current;
   }return current;
  }
  public static IReadOnlyList<string> MacroPreview(string document,int position,string word,StartupPreferences preferences){
   if(!preferences.EnhancedMacroExpansion||document==null||position<0||position>document.Length||!Macros(document).TryGetValue(word,out var macro))return Array.Empty<string>();
   var expression=word;
   if(macro.Parameters!=null){var start=position+word.Length;while(start<document.Length&&char.IsWhiteSpace(document[start]))start++;if(start>=document.Length||document[start]!='('||!Arguments(document,start,out var arguments,out var end))return Array.Empty<string>();expression=document.Substring(position,end-position);}
   var expanded=ExpandMacro(document,expression);if(expanded==null)return Array.Empty<string>();
   if(preferences.FormatMacroExpansion)expanded=FormatExpansion(expanded);
   var rows=new List<string>();if(preferences.KeepOriginalMacroExpansion)rows.Add("#define "+word+(macro.Parameters==null?"":"("+string.Join(", ",macro.Parameters)+")")+" "+macro.Body);rows.Add(expanded);return rows;
  }
  public static string FormatExpansion(string text){var code=CppCodeMask.Create(text);var output=new StringBuilder();var depth=0;var parens=0;for(var i=0;i<text.Length;i++){var c=code[i];if(c=='(')parens++;if(c==')')parens=Math.Max(0,parens-1);if(c=='}'){depth=Math.Max(0,depth-1);if(output.Length>0&&output[output.Length-1]!='\n')output.Append('\n');output.Append(' ',depth*2);}output.Append(text[i]);if(c=='{'){depth++;output.Append('\n').Append(' ',depth*2);}else if(c==';'&&parens==0)output.Append('\n').Append(' ',depth*2);}return output.ToString().Trim();}
  public static string CommentAbove(string document,int oneBasedLine,bool doxygen){
   var lines=(document??"").Replace("\r\n","\n").Split('\n');var index=Math.Min(lines.Length-1,oneBasedLine-2);var comments=new List<string>();
   for(var i=index;i>=0&&comments.Count<12;i--){var line=lines[i].Trim();if(line.StartsWith("//",StringComparison.Ordinal)||line.StartsWith("/*",StringComparison.Ordinal)||line.StartsWith("*",StringComparison.Ordinal)||line.EndsWith("*/",StringComparison.Ordinal))comments.Insert(0,line);else break;}
   var text=string.Join("\n",comments);if(!doxygen)return text;
   return string.Join("\n",comments.Select(line=>Regex.Replace(line,@"^\s*(?:///!?|/\*\*?|\*)\s?|\*/\s*$","",RegexOptions.CultureInvariant,Timeout)).Select(line=>Regex.Replace(line,@"[@\\]brief\s*","",RegexOptions.CultureInvariant,Timeout)).Select(line=>Regex.Replace(line,@"[@\\]param\s+(\w+)\s*",m=>m.Groups[1].Value+" — ",RegexOptions.CultureInvariant,Timeout))).Trim();
  }
  public static string StructuralDescription(string word){switch(word){case "if":return "if (condition) { statements }";case "for":return "for (initializer; condition; increment) { statements }";case "while":return "while (condition) { statements }";case "switch":return "switch (expression) { case value: statements; break; }";case "class":return "class Name { members };";case "namespace":return "namespace Name { declarations }";case "try":return "try { statements } catch (...) { handler }";default:return null;}}
  public static bool IntegratedMenuVisible(bool integrated,bool mainMenu)=>integrated==mainMenu;
 }
}
