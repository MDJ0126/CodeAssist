using System;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    public static class CppClassTransforms
    {
        // Resolve the complete class scope before inserting. Never insert into an identifier.
        public static EditorTextChange InsertMember(string text,int caret,string declaration,string owner=null)
        {
            if(text==null||caret<0||caret>text.Length||string.IsNullOrWhiteSpace(declaration))throw new ArgumentException("Invalid class member insertion");
            var code=CppCodeMask.Create(text);var braces=new BraceMap(code);
            var classes=Regex.Matches(code,@"\b(?:class|struct)\s+(?<name>[A-Za-z_]\w*)\b[^;{}]*\{",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)).Cast<Match>()
                .Where(m=>!Regex.IsMatch(code.Substring(Math.Max(0,m.Index-6),Math.Min(6,m.Index)),@"enum\s+$"))
                .Select(m=>new {Match=m,Open=m.Index+m.Length-1,Close=braces.At(m.Index+m.Length-1,out var end)?end:-1})
                .Where(c=>c.Close>=0).ToArray();
            var candidates=classes.Where(c=>owner==null?c.Match.Index<=caret&&caret<=c.Close:c.Match.Groups["name"].Value==owner.Split(new[]{"::"},StringSplitOptions.None).Last()).ToArray();
            // Qualified duplicate class names need semantic resolution; do not pick arbitrarily.
            if(owner!=null&&candidates.Length!=1)throw new NotSupportedException("The owner class is missing or ambiguous in this document.");
            var target=candidates.OrderBy(c=>c.Close-c.Match.Index).FirstOrDefault();
            if(target==null)throw new NotSupportedException("Place the caret on a complete class declaration or inside its body.");
            var header=code.Substring(target.Match.Index,target.Open-target.Match.Index);
            if(header.IndexOf('<')>=0||header.IndexOf('>')>=0)throw new NotSupportedException("Template classes require semantic member insertion.");
            var nl=text.Contains("\r\n")?"\r\n":"\n";
            var closeLine=text.LastIndexOf('\n',Math.Max(0,target.Close-1));var indentStart=closeLine<0?0:closeLine+1;var closeIndent=text.Substring(indentStart,target.Close-indentStart);
            if(closeIndent.Any(c=>c!=' '&&c!='\t'))closeIndent="";
            var beforeClose=target.Close-closeIndent.Length;var lineStart=beforeClose;
            var body=declaration.Replace("\r\n","\n").Trim().Replace("\n",nl+closeIndent+"    ");
            var inserted=(lineStart>0&&text[lineStart-1]!='\n'?nl:"")+closeIndent+"public:"+nl+closeIndent+"    "+body+nl;
            return new EditorTextChange(lineStart,0,inserted,lineStart,inserted.Length);
        }
        public static string MethodOwner(string definition)
        {
            var code=CppCodeMask.Create(definition??"");var open=code.IndexOf('(');if(open<0)return null;
            var m=Regex.Match(code.Substring(0,open),@"(?<owner>[A-Za-z_]\w*(?:::[A-Za-z_]\w*)*)::~?[A-Za-z_]\w*\s*$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
            return m.Success?m.Groups["owner"].Value:null;
        }
        public static string HeaderSafeImplementation(string implementation)
        {
            if(implementation==null)return null;
            // Out-of-class definitions in headers need inline to preserve the one-definition rule.
            var code=CppCodeMask.Create(implementation);var depth=0;var starts=new System.Collections.Generic.List<int>();
            for(var i=0;i<code.Length;i++)
            {
                if((i==0||code[i-1]=='\n')&&depth==0)
                {
                    var end=code.IndexOf('\n',i);if(end<0)end=code.Length;var line=code.Substring(i,end-i);
                    if(!line.TrimStart().StartsWith("inline ",StringComparison.Ordinal)&&CppRefactoring.CreateDeclaration(line)!=null)starts.Add(i);
                }
                if(code[i]=='{')depth++;else if(code[i]=='}')depth--;
            }
            foreach(var start in starts.OrderByDescending(x=>x))implementation=implementation.Insert(start,"inline ");
            return implementation;
        }
    }
}
