using System;
using System.Linq;
namespace CodeAssist.Core
{
    public enum ClosingBracePlacement { BlankLine, NextLine, SameLine }
    public sealed class PairInsertion
    {
        public PairInsertion(string text,int caretOffset,int closingOffset){Text=text;CaretOffset=caretOffset;ClosingOffset=closingOffset;}
        public string Text{get;} public int CaretOffset{get;} public int ClosingOffset{get;}
    }
    public static class CppPairing
    {
        public static bool Opening(char key)=>key=='('||key=='['||key=='{'||key=='\''||key=='"';
        public static char Closing(char key)=>key=='('?')':key=='['?']':key=='{'?'}':key;
        public static bool CanOpen(string text,int caret,char key)
        {
            if(text==null||caret<0||caret>text.Length||!Opening(key))return false;
            if(key=='\''&&caret>0&&char.IsDigit(text[caret-1]))return false;
            var probe=text.Substring(0,caret)+"\u0001";
            var code=CppCodeMask.Create(probe);
            if(code[code.Length-1]!='\u0001')return false;
            if(caret<text.Length && !char.IsWhiteSpace(text[caret]) && ")]},;>".IndexOf(text[caret])<0 && text[caret]!=Closing(key))return false;
            return true;
        }
        public static bool IsFreshPair(string before,int start,int length,string after,int caret,char key)
        {
            if(before==null||after==null||start<0||length<0||start>before.Length-length||caret!=start+1||!Opening(key))return false;
            if(after.Length!=before.Length-length+2||after[start]!=key||after[start+1]!=Closing(key))return false;
            return string.CompareOrdinal(before,0,after,0,start)==0 &&
                string.CompareOrdinal(before,start+length,after,start+2,before.Length-start-length)==0;
        }
        public static PairInsertion AfterOpening(string text,int caret,char key,ClosingBracePlacement placement,string indentUnit="    ")
        {
            if(text==null||caret<=0||caret>text.Length||text[caret-1]!=key||!CanOpen(text.Remove(caret-1,1),caret-1,key))return null;
            var close=Closing(key);
            if(caret<text.Length&&text[caret]==close)return null;
            if(key!='{'||placement==ClosingBracePlacement.SameLine)return new PairInsertion(close.ToString(),0,0);
            var lineStart=caret<=1?0:text.LastIndexOf('\n',caret-2)+1;
            var indent=new string(text.Substring(lineStart,caret-lineStart).TakeWhile(ch=>ch==' '||ch=='\t').ToArray());
            var newline=text.Contains("\r\n")?"\r\n":"\n";
            if(placement==ClosingBracePlacement.NextLine)return new PairInsertion(newline+indent+"}",0,newline.Length+indent.Length);
            var inner=newline+indent+(indentUnit??"    ");
            return new PairInsertion(inner+newline+indent+"}",inner.Length,inner.Length+newline.Length+indent.Length);
        }
        public static bool CanAddMethodCall(string text,int caret,string name)
        {
            if(text==null||string.IsNullOrEmpty(name)||caret<name.Length||caret>text.Length)return false;
            if(string.CompareOrdinal(text,caret-name.Length,name,0,name.Length)!=0)return false;
            var next=caret;while(next<text.Length&&(text[next]==' '||text[next]=='\t'))next++;
            if(next<text.Length&&text[next]=='(')return false;
            var start=caret-name.Length;var previous=start-1;while(previous>=0&&char.IsWhiteSpace(text[previous]))previous--;
            if(previous>=0&&text[previous]=='&')return false;
            var lineStart=start==0?0:text.LastIndexOf('\n',start-1)+1;
            if(text.Substring(lineStart,start-lineStart).TrimStart().StartsWith("#",StringComparison.Ordinal))return false;
            return CanOpen(text,caret,'(');
        }
    }
}