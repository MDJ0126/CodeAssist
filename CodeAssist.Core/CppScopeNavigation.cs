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
            // At global scope, skip parameter lists and visit outer brace blocks.
            var globals = pairs.Where(pair => pair.Kind == '{' && pair.BraceDepth == 0);
            return next ? globals.Where(pair => pair.Open > position).Select(pair => pair.Open).DefaultIfEmpty(code.Length).Min() :
                globals.Where(pair => pair.Close < position).Select(pair => pair.Close).DefaultIfEmpty(0).Max();
        }
    }
}
