using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CodeAssist.Core
{
    public static class CppExpressionContext
    {
        private static readonly Regex QualifiedFunction = new Regex(
            @"\b(?<owner>[A-Za-z_]\w*)\s*::\s*~?[A-Za-z_]\w*\s*\([^;{}]*\)\s*(?:const\s*)?(?:override\s*)?\{",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string FindEnclosingFunctionOwner(string text, int caretPosition)
        {
            if (string.IsNullOrEmpty(text) || caretPosition <= 0) return null;
            var prefix = CppCodeMask.Create(text.Substring(0, Math.Min(caretPosition, text.Length)));
            var owners = new Dictionary<int, string>();
            foreach (Match match in QualifiedFunction.Matches(prefix)) owners[match.Index + match.Length - 1] = match.Groups["owner"].Value;
            foreach (Match match in Regex.Matches(prefix, @"\b(?:class|struct)\s+(?:\w+_API\s+)?(?<owner>[A-Za-z_]\w*)[^;{}]*\{"))
                owners[match.Index + match.Length - 1] = match.Groups["owner"].Value;
            var stack = new Stack<string>();
            for (var index=0; index < prefix.Length; index++)
            {
                if (prefix[index] == '{') stack.Push(owners.TryGetValue(index, out var owner) ? owner : stack.Count > 0 ? stack.Peek() : null);
                else if (prefix[index] == '}' && stack.Count > 0) stack.Pop();
            }
            return stack.Count > 0 ? stack.Peek() : null;
        }
    }
}
