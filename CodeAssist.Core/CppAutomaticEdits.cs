using System;
using System.Text.RegularExpressions;
using System.Linq;
namespace CodeAssist.Core
{
    public static class CppAutomaticEdits
    {
        public static string WordBefore(string text,int caret)
        {
            if(text==null||caret<0||caret>text.Length)return "";
            var start=caret;while(start>0&&(char.IsLetterOrDigit(text[start-1])||text[start-1]=='_'))start--;
            if(start==caret||char.IsDigit(text[start]))return "";
            var word=text.Substring(start,caret-start);var code=CppCodeMask.Create(text.Substring(0,caret));
            return code.Substring(start,word.Length)==word?word:"";
        }
        public static EditorTextChange DotToArrow(string text,int caret,string valueType,bool overloadedArrow=false,bool allowOverloaded=true)
        {
            if(WordBefore(text,caret).Length==0||valueType==null||(!(overloadedArrow&&allowOverloaded)&&!Regex.IsMatch(valueType,@"\*\s*(?:const\s*)?$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20))))return null;
            return new EditorTextChange(caret,0,"->",caret+2,0);
        }
        public static bool HasArrowOperator(string source,string type)
        {
            if(string.IsNullOrWhiteSpace(type))return false;
            var name=Regex.Match(type,@"[A-Za-z_]\w*").Value;
            if(name.Length==0)return false;
            var code=CppCodeMask.Create(source??"");
            var declaration=Regex.Match(code,@"\b(?:class|struct)\s+"+Regex.Escape(name)+@"\b[^;{]*\{",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
            if(!declaration.Success)return false;
            var start=declaration.Index+declaration.Length;var depth=1;var end=start;
            for(;end<code.Length&&depth>0;end++){if(code[end]=='{')depth++;else if(code[end]=='}')depth--;}
            return depth==0&&Regex.IsMatch(code.Substring(start,end-start-1),@"\boperator\s*->\s*\(",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
        }
        public static string FindDeclaredTypeCasing(string text,string word)
        {
            if(string.IsNullOrEmpty(word))return null;
            var matches=CppSemanticParser.Parse(CppCodeMask.Create(text??"")).Where(t=>t.Kind==SemanticTokenKind.Type&&string.Equals(t.Name,word,StringComparison.OrdinalIgnoreCase)).Select(t=>t.Name).Distinct(StringComparer.Ordinal).Take(2).ToArray();
            return matches.Length==1?matches[0]:null;
        }
        public static EditorTextChange RepairCase(string text,int caret,string corrected)
        {
            var word=WordBefore(text,caret);if(word.Length==0||corrected==null||word==corrected||!word.Equals(corrected,StringComparison.OrdinalIgnoreCase))return null;
            return new EditorTextChange(caret-word.Length,word.Length,corrected,caret-word.Length+corrected.Length,0);
        }
    }
}
