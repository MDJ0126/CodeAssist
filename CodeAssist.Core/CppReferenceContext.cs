using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeAssist.Core
{
    /// <summary>Conservative lexical block resolution, not a complete C++ semantic parser.</summary>
    public sealed class CppReferenceContext
    {
        private static readonly Regex Declaration = new Regex(@"\b(?<type>(?:const\s+)?[A-Za-z_]\w*(?:\s*<[^;{}()]+>)?\s*[*&]?)\s+(?<name>[A-Za-z_]\w*)\s*(?=[=;,\[(])" );
        private readonly List<Block> blocks = new List<Block>();
        private readonly List<Local> locals = new List<Local>();
        public string Code { get; }
        public CppReferenceContext(string text)
        {
            Code = CppCodeMask.Create(text);
            var stack = new Stack<Block>();
            var root = new Block(0, 0, Code.Length, null); blocks.Add(root); stack.Push(root);
            for (var i = 0; i < Code.Length; i++)
            {
                if (Code[i] == '{')
                {
                    var prefix = Code.Substring(Math.Max(0, i - 240), Math.Min(i, 240));
                    var executable = stack.Peek().Executable || Regex.IsMatch(prefix, @"\)\s*(?:(?:const|override|final|noexcept)\s*)*$");
                    var block = new Block(blocks.Count, i + 1, Code.Length, stack.Peek()) { Executable = executable };
                    blocks.Add(block); stack.Push(block);
                }
                else if (Code[i] == '}' && stack.Count > 1) stack.Pop().End = i;
            }
            foreach (Match match in Declaration.Matches(Code))
            {
                var type = match.Groups["type"].Value.Trim();
                if (new[] { "return", "throw", "delete", "new", "case" }.Contains(type)) continue;
                var name = match.Groups["name"];
                var block = BlockAt(name.Index);
                if (block.Executable) locals.Add(new Local(name.Value, name.Index, block, type));
            }
        }
        public string ScopeAt(int offset) => BlockAt(offset).Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public bool IsExecutableAt(int offset) => BlockAt(offset).Executable;
        public string ResolveVariableType(string name, int offset)
        {
            var declaration = DeclarationAt(name, offset);
            return declaration.HasValue ? locals.First(item => item.Offset == declaration.Value).Type : null;
        }
        public int? DeclarationAt(string name, int offset)
        {
            var previous = Math.Min(offset - 1, Code.Length - 1);
            while (previous >= 0 && char.IsWhiteSpace(Code[previous])) previous--;
            if (previous >= 0 && (Code[previous] == '.' || Code[previous] == '>' || Code[previous] == ':')) return null;
            for (var block = BlockAt(offset); block != null; block = block.Parent)
            {
                var local = locals.LastOrDefault(item => item.Block == block && item.Offset <= offset && item.Name == name);
                if (local != null) return local.Offset;
            }
            return null;
        }
        public IReadOnlyList<int> References(string name, int? declaration = null)
        {
            return Regex.Matches(Code, @"\b" + Regex.Escape(name) + @"\b").Cast<Match>()
                .Where(match => !declaration.HasValue || DeclarationAt(name, match.Index) == declaration)
                .Select(match => match.Index).ToArray();
        }
        private Block BlockAt(int offset) => blocks.Last(block => block.Start <= offset && offset <= block.End);
        private sealed class Block
        {
            public Block(int id, int start, int end, Block parent) { Id=id; Start=start; End=end; Parent=parent; }
            public int Id; public int Start; public int End; public Block Parent; public bool Executable;
        }
        private sealed class Local
        {
            public Local(string name, int offset, Block block, string type) { Name=name; Offset=offset; Block=block; Type=type; }
            public string Name; public int Offset; public Block Block; public string Type;
        }
    }
}
