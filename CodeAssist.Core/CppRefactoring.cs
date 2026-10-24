using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    // Explicit, previewable transformations. Unsupported syntax is rejected rather than guessed.
    public static class CppRefactoring
    {
        private static readonly TimeSpan Timeout=TimeSpan.FromMilliseconds(100);
        private static Match Match(string value,string pattern)=>Regex.Match(value,pattern,RegexOptions.CultureInvariant,Timeout);
        public static bool QualifiedIdentifier(string value)=>value!=null&&Match(value,@"^[A-Za-z_]\w*(?:::[A-Za-z_]\w*)*$").Success;
        public static string SimplifyInstance(string declaration)
        {
            var m=Match(declaration??"",@"^(?<indent>\s*)(?<type>[A-Za-z_]\w*(?:::[A-Za-z_]\w*)*)\s+(?<name>[A-Za-z_]\w*)\s*=\s*\k<type>\s*(?<args>\([^;{}]*\))\s*;\s*$");
            if(!m.Success||m.Groups["args"].Value=="()")return null;
            return m.Groups["indent"].Value+m.Groups["type"].Value+" "+m.Groups["name"].Value+m.Groups["args"].Value+";";
        }
        private sealed class Method {internal string Returns,Name,Arguments,Suffix;}
        private static Method Parse(string declaration)
        {
            if(string.IsNullOrWhiteSpace(declaration)||declaration.Length>32768)return null;
            var code=CppCodeMask.Create(declaration);var open=code.IndexOf('(');if(open<0)return null;
            var map=new BraceMap(code);if(!map.At(open,out var close)||close<0)return null;
            var name=Match(code.Substring(0,open),@"(?<name>~?[A-Za-z_]\w*(?:::[~A-Za-z_]\w*)*)\s*$");if(!name.Success)return null;
            if(new[]{"if","while","for","switch","catch"}.Contains(name.Groups["name"].Value))return null;
            var end=code.IndexOfAny(new[]{'{',';'},close+1);if(end<0)end=code.Length;
            var suffix=code.Substring(close+1,end-close-1).Trim();
            if(!Match(suffix,@"^(?:(?:const|volatile|noexcept|override|final)\s*|&&?\s*)*(?:=\s*0\s*)?$").Success)return null;
            return new Method {Returns=declaration.Substring(0,name.Index).Trim(),Name=name.Groups["name"].Value,Arguments=declaration.Substring(open+1,close-open-1),Suffix=suffix};
        }
        public static string[] SplitArguments(string input)
        {
            if(string.IsNullOrWhiteSpace(input))return Array.Empty<string>();
            var code=CppCodeMask.Create(input);var depth=0;var start=0;var parts=new List<string>();
            for(var i=0;i<=code.Length;i++)
            {
                if(i<code.Length&&"<([{ ".IndexOf(code[i])>=0&&code[i]!=' ')depth++;
                else if(i<code.Length&&">)]}".IndexOf(code[i])>=0)depth--;
                if(i==code.Length||code[i]==','&&depth==0){parts.Add(input.Substring(start,i-start).Trim());start=i+1;}
                if(depth<0)throw new ArgumentException("Unbalanced arguments");
            }
            if(depth!=0)throw new ArgumentException("Unbalanced arguments");return parts.ToArray();
        }
        private static string WithoutDefault(string argument)
        {
            var code=CppCodeMask.Create(argument);var depth=0;
            for(var i=0;i<code.Length;i++){if("<([{ ".IndexOf(code[i])>=0&&code[i]!=' ')depth++;else if(">)]}".IndexOf(code[i])>=0)depth--;else if(code[i]=='='&&depth==0)return argument.Substring(0,i).TrimEnd();}return argument;
        }
        public static string CreateDeclaration(string definition)
        {
            var method=Parse(definition);if(method==null)return null;
            return (method.Returns.Length>0?method.Returns+" ":"")+method.Name.Split(new[]{"::"},StringSplitOptions.None).Last()+"("+method.Arguments+")"+(method.Suffix.Length>0?" "+method.Suffix:"")+";";
        }
        public static string CreateImplementation(string declaration,string owner,string newline="\n",bool inlineOverride=false,string bodyTemplate=null)
        {
            var method=Parse(declaration);if(method==null)return null;
            if(owner!=null&&owner.Length>0&&!QualifiedIdentifier(owner))throw new ArgumentException("Invalid owner type");
            var returns=Regex.Replace(method.Returns,@"\b(?:virtual|explicit|static|friend)\s*","",RegexOptions.CultureInvariant,Timeout).Trim();
            var suffix=Regex.Replace(method.Suffix,@"\b(?:override|final)\b|=\s*0","",RegexOptions.CultureInvariant,Timeout).Trim();
            var name=method.Name;if(!inlineOverride&&!string.IsNullOrEmpty(owner)&&!name.Contains("::"))name=owner+"::"+name;
            if(inlineOverride)name=name.Split(new[]{"::"},StringSplitOptions.None).Last();
            var values=new Dictionary<string,string>{{"MethodName",method.Name},{"ClassName",owner??""},{"ReturnType",returns},{"Parameters",method.Arguments},{"Body","    throw \"Not implemented\";"}};
            var template=new SnippetTemplate("Method body","methodbody",bodyTemplate??"$Body$");if(template.Inputs(values).Length>0)throw new ArgumentException("Unknown method body template placeholder");
            var body=template.Expand(values).Text.Replace("\r\n","\n").Replace("\n",newline);
            return (returns.Length>0?returns+" ":"")+name+"("+string.Join(", ",SplitArguments(method.Arguments).Select(WithoutDefault))+")"+(suffix.Length>0?" "+suffix:"")+(inlineOverride?" override":"")+newline+"{"+newline+body+newline+"}"+newline;
        }
        public static string CreateImplementations(string declarations,string owner,string newline="\n",bool overrides=false,string bodyTemplate=null)
        {
            if(declarations==null||declarations.Length>128*1024)return null;
            var output=new List<string>();var code=CppCodeMask.Create(declarations);var depth=0;var start=0;
            for(var i=0;i<code.Length;i++){if(code[i]=='(')depth++;if(code[i]==')')depth--;if(code[i]==';'&&depth==0){var part=CreateImplementation(declarations.Substring(start,i-start+1),owner,newline,overrides,bodyTemplate);if(part!=null)output.Add(part);start=i+1;}}
            return output.Count==0?null:string.Join(newline,output);
        }
        public static string SimilarMember(string declaration,string newName)
        {
            if(!QualifiedIdentifier(newName)||newName.Contains("::"))throw new ArgumentException("Invalid member name");
            var code=CppCodeMask.Create(declaration??"");var m=Match(code,@"(?<name>[A-Za-z_]\w*)\s*(?:\([^;{}]*\)(?:\s*(?:const|noexcept|override|final))*)?\s*;\s*$");
            return m.Success?declaration.Remove(m.Groups["name"].Index,m.Groups["name"].Length).Insert(m.Groups["name"].Index,newName):null;
        }
        public static string EncapsulateField(string declaration,string propertyName,string newline="\n")
        {
            var m=Match(declaration??"",@"^\s*(?<type>(?:const\s+)?[A-Za-z_]\w*(?:::[A-Za-z_]\w*)*(?:\s*[*&])?)\s+(?<field>[A-Za-z_]\w*)\s*;\s*$");
            if(!m.Success||!QualifiedIdentifier(propertyName)||propertyName.Contains("::"))return null;
            var type=m.Groups["type"].Value;var field=m.Groups["field"].Value;
            return "private:"+newline+"    "+declaration.Trim()+newline+"public:"+newline+"    "+type+" Get"+propertyName+"() const { return "+field+"; }"+newline+(type.StartsWith("const ",StringComparison.Ordinal)?"":"    void Set"+propertyName+"("+type+" value) { "+"this->"+field+" = value; }"+newline);
        }
        public static string UsingNamespace(string name)=>QualifiedIdentifier(name)?"using namespace "+name+";":null;
        public static string ToggleBraces(string selected)
        {
            if(string.IsNullOrWhiteSpace(selected))return null;
            var value=selected.Trim();var code=CppCodeMask.Create(value);
            if(code[0]=='{')
            {
                var map=new BraceMap(code);if(!map.At(0,out var end)||end!=code.Length-1)return null;
                var body=value.Substring(1,value.Length-2).Trim();var masked=CppCodeMask.Create(body);
                if(Match(masked,@"^(?:if|for|while|switch|do|try|catch|else)\b").Success)return null;
                // Restrict removal to one simple expression statement; never alter declarations or dangling else.
                if(!Match(masked,@"^(?:return\s+)?[A-Za-z_]\w*(?:(?:::|->|\.)[A-Za-z_]\w*)?\s*\([^;{}]*\)\s*;$").Success)return null;
                return body;
            }
            if(!code.EndsWith(";",StringComparison.Ordinal)||code.IndexOf('{')>=0||Regex.Matches(code,";",RegexOptions.CultureInvariant,Timeout).Count!=1)return null;
            return "{ "+value+" }";
        }
        public static string MissingCases(string selectedSwitch,string enumDeclaration,string qualifiedEnum,string newline="\n")
        {
            if(!QualifiedIdentifier(qualifiedEnum))return null;
            var code=CppCodeMask.Create(enumDeclaration??"");var start=code.IndexOf('{');var end=code.LastIndexOf('}');if(start<0||end<start)return null;
            var names=SplitArguments(code.Substring(start+1,end-start-1)).Select(s=>Match(s,@"^\s*(?<name>[A-Za-z_]\w*)\s*(?:=.*)?$")).Where(m=>m.Success).Select(m=>m.Groups["name"].Value).ToArray();
            var switchCode=CppCodeMask.Create(selectedSwitch??"");var close=switchCode.LastIndexOf('}');if(close<0||!Match(switchCode,@"^\s*switch\s*\(").Success)return null;
            var existing=Regex.Matches(switchCode,@"\bcase\s+(?:(?:[A-Za-z_]\w*)::)*(?<name>[A-Za-z_]\w*)\s*:",RegexOptions.CultureInvariant,Timeout).Cast<Match>().Select(m=>m.Groups["name"].Value).ToArray();
            var cases=names.Where(n=>!existing.Contains(n)).Select(n=>"    case "+qualifiedEnum+"::"+n+":"+newline+"        break;"+newline);
            return selectedSwitch.Insert(close,string.Concat(cases));
        }
    }
}
