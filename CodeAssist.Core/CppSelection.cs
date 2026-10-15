using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeAssist.Core
{
    public sealed class SelectionRange
    {
        public SelectionRange(int start, int length) { Start = start; Length = length; }
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;
    }
    public static class CppSelection
    {
        public static SelectionRange Expand(string text, int start, int length, bool blockOnly)
        {
            return Expand(text, start, length, blockOnly, false, false);
        }
        public static SelectionRange Expand(string text, int start, int length, bool blockOnly, bool splitWords, bool trimWhitespace)
        {
            text = text ?? "";
            start = Math.Max(0, Math.Min(start, text.Length));
            var end = Math.Min(text.Length, start + Math.Max(0, length));
            var candidates = new List<SelectionRange>();
            if (!blockOnly)
            {
                var left = start; var right = end;
                while (left > 0 && Word(text[left - 1])) left--;
                while (right < text.Length && Word(text[right])) right++;
                if (splitWords && right > left)
                {
                    var boundaries = new List<int> { left };
                    for (var i = left + 1; i < right; i++)
                        if (text[i - 1] == '_' || text[i] == '_' ||
                            (char.IsLower(text[i - 1]) && char.IsUpper(text[i])) ||
                            (char.IsUpper(text[i - 1]) && char.IsUpper(text[i]) && i + 1 < right && char.IsLower(text[i + 1]))) boundaries.Add(i);
                    boundaries.Add(right);
                    for (var i = 0; i + 1 < boundaries.Count; i++)
                        candidates.Add(new SelectionRange(boundaries[i], boundaries[i + 1] - boundaries[i]));
                }
                if (right > left) candidates.Add(new SelectionRange(left, right - left));
            }
            var code = CppCodeMask.Create(text);
            var stack = new Stack<int>();
            for (var i = 0; i < code.Length; i++)
            {
                var ch = code[i];
                if (ch == '{' || (!blockOnly && (ch == '(' || ch == '['))) stack.Push(i);
                else if (ch == '}' || (!blockOnly && (ch == ')' || ch == ']')))
                {
                    var open = ch == '}' ? '{' : ch == ')' ? '(' : '[';
                    if (stack.Count == 0 || code[stack.Peek()] != open) continue;
                    var left = stack.Pop();
                    if (!blockOnly) candidates.Add(new SelectionRange(left + 1, i - left - 1));
                    candidates.Add(new SelectionRange(left, i - left + 1));
                    if (!blockOnly && open == '(')
                    {
                        var nameStart = left;
                        while (nameStart > 0 && (Word(code[nameStart - 1]) || code[nameStart - 1] == '.' || code[nameStart - 1] == ':' || code[nameStart - 1] == '-' || code[nameStart - 1] == '>')) nameStart--;
                        var name = Regex.Match(code.Substring(nameStart, left - nameStart), @"[A-Za-z_]\w*(?:(?:\.|->|::)[A-Za-z_]\w*)*$");
                        if (name.Success && !Regex.IsMatch(name.Value, @"^(?:if|while|for|switch|catch)$"))
                        {
                            var callEnd = i + 1;
                            if (callEnd < code.Length && code[callEnd] == ';') callEnd++;
                            candidates.Add(new SelectionRange(nameStart + name.Index, callEnd - nameStart - name.Index));
                        }
                    }
                }
            }
            if (!blockOnly)
            {
                var lineStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;
                var lineEnd = text.IndexOf('\n', end);
                if (lineEnd < 0) lineEnd = text.Length;
                if (trimWhitespace)
                {
                    var first = lineStart; var last = lineEnd;
                    while (first < last && char.IsWhiteSpace(text[first])) first++;
                    while (last > first && char.IsWhiteSpace(text[last - 1])) last--;
                    candidates.Add(new SelectionRange(first, last - first));
                }
                candidates.Add(new SelectionRange(lineStart, lineEnd - lineStart));
            }
            candidates.Add(new SelectionRange(0, text.Length));
            return candidates.Where(range => range.Start <= start && range.End >= end && range.Length > end - start)
                .OrderBy(range => range.Length).ThenByDescending(range => range.Start).FirstOrDefault() ?? new SelectionRange(start, end - start);
        }
        private static bool Word(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    }
    public sealed class SelectionHistory
    {
        private readonly Stack<SelectionRange> history = new Stack<SelectionRange>();
        private string text;
        private SelectionRange current;
        public SelectionRange Extend(string value, int start, int length, bool blockOnly, bool splitWords = false, bool trimWhitespace = false)
        {
            Synchronize(value, start, length);
            var next = CppSelection.Expand(value, start, length, blockOnly, splitWords, trimWhitespace);
            if (next.Start != start || next.Length != length) history.Push(current);
            return current = next;
        }
        public SelectionRange Shrink(string value, int start, int length)
        {
            Synchronize(value, start, length);
            return current = history.Count > 0 ? history.Pop() : current;
        }
        private void Synchronize(string value, int start, int length)
        {
            if (text != value || current == null || current.Start != start || current.Length != length) history.Clear();
            text = value; current = new SelectionRange(start, length);
        }
    }
}
