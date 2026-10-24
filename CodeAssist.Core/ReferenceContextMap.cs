using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    // Lexical C++ display context. Does not replace semantic reference resolution.
    public sealed class ReferenceContextMap
    {
        private sealed class Scope { public int Start,End; public string Name; public bool Type; }
        private readonly List<Scope> scopes=new List<Scope>();
        public ReferenceContextMap(string text)
        {
            var code=CppCodeMask.Create(text??"");
            var names=new Dictionary<int,string>();var types=new HashSet<int>();
            foreach(Match m in Regex.Matches(code,@"\b(?:class|struct)\s+(?:\w+_API\s+)?(?<name>[A-Za-z_]\w*)[^;{}]*\{"))
            {var open=m.Index+m.Length-1;names[open]=m.Groups["name"].Value;types.Add(open);}
            foreach(var token in CppSemanticParser.Parse(code).Where(t=>t.Kind==SemanticTokenKind.Function))
            {
                var p=token.Start+token.Length;while(p<code.Length&&char.IsWhiteSpace(code[p]))p++;
                if(p>=code.Length||code[p]!='(')continue;
                var depth=1;p++;while(p<code.Length&&depth>0){if(code[p]=='(')depth++;else if(code[p]==')')depth--;p++;}
                if(depth!=0)continue;
                var tail=Regex.Match(code.Substring(p),@"^\s*(?:(?:const|volatile|override|final|noexcept)\s*)*\{");
                if(!tail.Success)continue;
                var open=p+tail.Length-1;if(names.ContainsKey(open))continue;
                var prefix=code.Substring(0,token.Start);var qualified=Regex.Match(prefix,@"(?<owner>[A-Za-z_]\w*)\s*::\s*$");
                names[open]=qualified.Success?qualified.Groups["owner"].Value:token.Name;
            }
            var stack=new Stack<Scope>();
            for(var i=0;i<code.Length;i++)
            {
                if(code[i]=='{')
                {
                    var parent=stack.Count>0?stack.Peek():null;var type=types.Contains(i);
                    var name=names.TryGetValue(i,out var declared)?declared:parent?.Name;
                    if(!type&&parent!=null&&parent.Type)name=parent.Name;
                    var scope=new Scope {Start=i,End=code.Length,Name=name??"",Type=type||(parent?.Type??false)};
                    scopes.Add(scope);stack.Push(scope);
                }
                else if(code[i]=='}'&&stack.Count>0)stack.Pop().End=i;
            }
        }
        public string At(int offset)=>scopes.LastOrDefault(scope=>scope.Start<=offset&&offset<=scope.End)?.Name??"";
    }
}
