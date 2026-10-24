using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeAssist.Core
{
    public static class CppScopeNavigation
    {
        private sealed class Pair { public int Open; public int Close; public char Kind; public int BraceDepth; }
        public static int Move(string text, int position, bool next)
        {
            var code = CppCodeMask.Create(text);
            position = Math.Max(0, Math.Min(position, code.Length));
            var stack = new Stack<Pair>();
            var pairs = new List<Pair>();
            var braceDepth = 0;
            for (var i = 0; i < code.Length; i++)
            {
                var ch = code[i];
                if (ch == '{' || ch == '(')
                {
                    stack.Push(new Pair { Open = i, Kind = ch, BraceDepth = braceDepth });
                    if (ch == '{') braceDepth++;
                }
                else if (ch == '}' || ch == ')')
                {
                    if (stack.Count == 0 || stack.Peek().Kind != (ch == '}' ? '{' : '(')) continue;
                    var pair = stack.Pop(); pair.Close = i; pairs.Add(pair);
                    if (ch == '}') braceDepth--;
                }
            }
            var enclosing = pairs.Where(pair => next ? pair.Open <= position && position < pair.Close :
                pair.Open < position && position <= pair.Close).OrderBy(pair => pair.Close - pair.Open).FirstOrDefault();
            if (enclosing != null) return next ? enclosing.Close : enclosing.Open;
            // At global scope VA moves between method declarations, not opening braces.
            var globals=pairs.Where(pair=>pair.Kind=='{'&&pair.BraceDepth==0).OrderBy(pair=>pair.Open).ToArray();
            var functions=CppSemanticParser.Parse(code).Where(t=>t.Kind==SemanticTokenKind.Function).ToArray();
            var methods=new List<int>();var boundary=0;
            foreach(var pair in globals) {
                var function=functions.FirstOrDefault(t=>t.Start>=boundary&&t.Start<pair.Open);
                if(function!=null) {
                    var start=Math.Max(code.LastIndexOf('\n',Math.Max(0,function.Start-1)),code.LastIndexOf(';',Math.Max(0,function.Start-1)))+1;
                    start=Math.Max(start,boundary);while(start<function.Start&&char.IsWhiteSpace(code[start]))start++;
                    methods.Add(start);
                }
                boundary=pair.Close+1;
            }
            return next?methods.Where(start=>start>position).DefaultIfEmpty(code.Length).Min():methods.Where(start=>start<position).DefaultIfEmpty(0).Max();
        }
    }
}
