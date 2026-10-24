using System;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    public sealed class EditorTextChange
    {
        public EditorTextChange(int start,int length,string text,int selectionStart,int selectionLength)
        {Start=start;Length=length;Text=text;SelectionStart=selectionStart;SelectionLength=selectionLength;}
        public int Start{get;} public int Length{get;} public string Text{get;}
        public int SelectionStart{get;} public int SelectionLength{get;}
    }
    public static class CppEditing
    {
        public static EditorTextChange Surround(string text,int start,int length,char key,bool spaceAfterSlash)
        {
            if(text==null||start<0||length<=0||start>text.Length-length)return null;
            var value=text.Substring(start,length);
            if(key=='/'&&(value.IndexOf('\n')>=0||WholeLine(text,start,length)))return ToggleLines(text,start,length,spaceAfterSlash);
            if(key=='/'||key=='*')
            {
                if(value.StartsWith("/*",StringComparison.Ordinal)&&value.EndsWith("*/",StringComparison.Ordinal)&&value.Length>=4)
                {var inner=value.Substring(2,value.Length-4);return new EditorTextChange(start,length,inner,start,inner.Length);}
                if(value.Contains("*/"))return null;
                return new EditorTextChange(start,length,"/*"+value+"*/",start,length+4);
            }
            if(key=='('||key=='{')return new EditorTextChange(start,length,key+value+(key=='('?')':'}'),start,length+2);
            return null;
        }
        public static EditorTextChange SurroundDirective(string text,int start,int length,string condition="_DEBUG")
        {
            if(text==null||start<0||length<=0||start>text.Length-length||!Regex.IsMatch(condition??"",@"^[A-Za-z_]\w*$"))return null;
            var newline=text.Contains("\r\n")?"\r\n":"\n";var selected=text.Substring(start,length);
            var next=start+length;
            var existingBreak=next<text.Length&&(text[next]=='\r'||text[next]=='\n');
            var value="#ifdef "+condition+newline+selected+(selected.EndsWith(newline,StringComparison.Ordinal)?"":newline)+"#endif // "+condition+(existingBreak?"":newline);
            return new EditorTextChange(start,length,value,start+7,condition.Length);
        }
        private static bool WholeLine(string text,int start,int length)
        {
            var left=start==0?0:text.LastIndexOf('\n',start-1)+1;
            var right=text.IndexOf('\n',start+length);
            if(right<0)right=text.Length;
            return string.IsNullOrWhiteSpace(text.Substring(left,start-left))&&string.IsNullOrWhiteSpace(text.Substring(start+length,right-start-length));
        }
        private static EditorTextChange ToggleLines(string text,int start,int length,bool space)
        {
            var left=start==0?0:text.LastIndexOf('\n',start-1)+1;
            var selectedEnd=start+length;
            var right=selectedEnd>0&&text[selectedEnd-1]=='\n'?selectedEnd:text.IndexOf('\n',selectedEnd);
            if(right<0)right=text.Length;
            var value=text.Substring(left,right-left);
            var lines=Regex.Split(value,"(?<=\n)");
            var nonblank=lines.Where(line=>!string.IsNullOrWhiteSpace(line)).ToArray();
            var remove=nonblank.Length>0&&nonblank.All(line=>line.TrimStart(' ','\t').StartsWith("//",StringComparison.Ordinal));
            var transformed=string.Concat(lines.Select(line=>
            {
                if(string.IsNullOrWhiteSpace(line))return line;
                var indent=0;while(indent<line.Length&&(line[indent]==' '||line[indent]=='\t'))indent++;
                if(!remove)return line.Insert(indent,space?"// ":"//");
                var count=2;if(indent+2<line.Length&&line[indent+2]==' ')count++;
                return line.Remove(indent,count);
            }));
            return new EditorTextChange(left,right-left,transformed,left,transformed.Length);
        }
        public static string CommentContinuation(string text,int caret)
        {
            if(text==null||caret<0||caret>text.Length)return null;
            var lineStart=caret==0?0:text.LastIndexOf('\n',caret-1)+1;
            var current=text.Substring(lineStart,caret-lineStart);
            var indent=new string(current.TakeWhile(ch=>ch==' '||ch=='\t').ToArray());
            var comment=CppCodeMask.Comments(text);
            var prefix=comment.Substring(lineStart,caret-lineStart);
            var at=prefix.IndexOf("//",StringComparison.Ordinal);
            if(at>=0&&string.IsNullOrWhiteSpace(current.Substring(0,at)))
            {
                var end=at+2;while(end<prefix.Length&&prefix[end]=='/')end++;
                if(end<prefix.Length&&(prefix[end]=='!'||prefix[end]=='<'))end++;
                return indent+prefix.Substring(at,end-at)+" ";
            }
            var before=comment.Substring(0,caret);
            var open=before.LastIndexOf("/*",StringComparison.Ordinal);
            if(open<0||before.LastIndexOf("*/",StringComparison.Ordinal)>open)return null;
            return indent+(current.TrimStart(' ','\t').StartsWith("*",StringComparison.Ordinal)?"* ":" * ");
        }
    }
}