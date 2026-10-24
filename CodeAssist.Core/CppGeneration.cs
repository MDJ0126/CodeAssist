using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    public static class CppGeneration
    {
        public static string DocumentMethod(string signature,string newline="\n",string indent="")
        {
            if(string.IsNullOrWhiteSpace(signature)||signature.Length>32768)return null;
            var code=CppCodeMask.Create(signature);var open=code.IndexOf('(');if(open<0)return null;
            var name=Regex.Match(code.Substring(0,open),@"(?<name>~?[A-Za-z_]\w*(?:::[~A-Za-z_]\w*)*)\s*$");if(!name.Success)return null;
            if(new[]{"if","for","while","switch","catch"}.Contains(name.Groups["name"].Value))return null;
            var close=-1;var depth=1;for(var i=open+1;i<code.Length;i++){if(code[i]=='(')depth++;if(code[i]==')'&&--depth==0){close=i;break;}}
            if(close<0)return null;
            var args=signature.Substring(open+1,close-open-1);var masked=CppCodeMask.Create(args);var parts=new List<string>();var start=0;var nesting=0;
            for(var i=0;i<=masked.Length;i++)
            {
                if(i<masked.Length&&"<([{ ".IndexOf(masked[i])>=0&&masked[i]!=' ')nesting++;
                if(i<masked.Length&&">)]}".IndexOf(masked[i])>=0)nesting--;
                if(i==masked.Length||(masked[i]==','&&nesting==0)){parts.Add(args.Substring(start,i-start));start=i+1;}
            }
            var lines=new List<string> {"/**"," * @brief "};
            foreach(var part in parts)
            {
                var argument=part.Split('=')[0].Trim();
                var pointer=Regex.Match(argument,@"\(\s*[*&]\s*(?<name>[A-Za-z_]\w*)\s*\)");
                var parameter=pointer.Success?pointer:Regex.Match(argument,@"(?<name>[A-Za-z_]\w*)\s*(?:\[[^\]]*\]\s*)*$");
                if(parameter.Success&&(pointer.Success||parameter.Index>0)&&!argument.Substring(0,parameter.Index).TrimEnd().EndsWith("::",StringComparison.Ordinal))lines.Add(" * @param "+parameter.Groups["name"].Value+" ");
            }
            var returns=code.Substring(0,name.Index).Trim();if(returns.Length>0&&!Regex.IsMatch(returns,@"\bvoid\s*$"))lines.Add(" * @return ");
            lines.Add(" */");return string.Join(newline,lines.Select(line=>indent+line))+newline;
        }
        public static EditorTextChange Include(string text,string header,bool system)
        {
            if(text==null||string.IsNullOrWhiteSpace(header)||header.IndexOfAny(new[]{'\r','\n','"','<','>','\0'})>=0)throw new ArgumentException("Invalid include name");
            if(CppDocumentNavigation.Includes(text).Any(i=>i.Name.Replace('\\','/')==header.Replace('\\','/')))return null;
            var newline=text.Contains("\r\n")?"\r\n":"\n";var mask=CppCodeMask.Create(text);var offset=text.StartsWith("\ufeff",StringComparison.Ordinal)?1:0;
            while(offset<text.Length)
            {
                var end=text.IndexOf('\n',offset);if(end<0)end=text.Length-1;
                var length=end-offset+1;if(mask.Substring(offset,length).Trim().Length>0)break;offset=end+1;
            }
            var remaining=text.Substring(offset);var pragma=Regex.Match(remaining,@"^\s*#\s*pragma\s+once[^\r\n]*(?:\r?\n|$)");
            if(pragma.Success)offset+=pragma.Length;
            else
            {
                var guard=Regex.Match(remaining,@"^\s*#\s*ifndef\s+(?<guard>[A-Za-z_]\w*)[^\r\n]*\r?\n\s*#\s*define\s+\k<guard>\b[^\r\n]*(?:\r?\n|$)");
                if(guard.Success)offset+=guard.Length;
            }
            var prefix=offset>0&&text[offset-1]!='\n'?newline:"";
            var directive=prefix+"#include "+(system?"<":"\"")+header+(system?">":"\"")+newline;
            return new EditorTextChange(offset,0,directive,offset+directive.Length,0);
        }
        public static string ForwardDeclaration(string qualifiedName,bool structure)
        {
            if(!Regex.IsMatch(qualifiedName??"",@"^[A-Za-z_]\w*(?:::[A-Za-z_]\w*)*$"))throw new ArgumentException("Expected a qualified C++ name");
            var names=qualifiedName.Split(new[]{"::"},StringSplitOptions.None);
            return string.Concat(names.Take(names.Length-1).Select(n=>"namespace "+n+" { "))+(structure?"struct ":"class ")+names.Last()+";"+string.Concat(names.Take(names.Length-1).Select(n=>" }"));
        }
        public static string SnippetFromSelection(string title,string shortcut,string selection)
        {
            if(string.IsNullOrWhiteSpace(title)||title.IndexOfAny(new[]{'\r','\n','\f'})>=0||!Regex.IsMatch(shortcut??"",@"^[A-Za-z_]\w*$")||string.IsNullOrEmpty(selection)||selection.Contains("\f"))throw new ArgumentException("Invalid snippet title, shortcut, or selection");
            return "a:"+title+":"+shortcut+":\n"+selection.Replace("$","$$");
        }
        public static Uri HyperlinkAt(string text,int caret)
        {
            if(text==null||caret<0||caret>text.Length)return null;
            foreach(Match match in Regex.Matches(text,@"https?://[^\s<>\""']+",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(30)))
            {
                if(match.Index>caret||match.Index+match.Length<caret)continue;
                var candidate=match.Value.TrimEnd('.',',',';',')',']','}');
                if(Uri.TryCreate(candidate,UriKind.Absolute,out var uri)&&(uri.Scheme=="http"||uri.Scheme=="https"))return uri;
            }
            return null;
        }
    }
}
