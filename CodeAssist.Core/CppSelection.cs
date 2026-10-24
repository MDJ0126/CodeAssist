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
    public sealed class SmartSelectionOptions
    {
        public bool SmallIncrements { get; set; } = true;
        public bool StartWithWord { get; set; } = true;
        public bool SplitCase { get; set; }
        public bool SplitUnderscores { get; set; }
        public bool TrimWhitespace { get; set; }
    }
    public static class CppSelection
    {
        public static SelectionRange Expand(string text, int start, int length, bool blockOnly)
        {
            return Expand(text, start, length, blockOnly, false, false);
        }
        public static SelectionRange Expand(string text, int start, int length, bool blockOnly, bool splitWords, bool trimWhitespace)
        {
            return Expand(text,start,length,blockOnly,new SmartSelectionOptions {SplitCase=splitWords,SplitUnderscores=splitWords,TrimWhitespace=trimWhitespace});
        }
        public static SelectionRange Expand(string text,int start,int length,bool blockOnly,SmartSelectionOptions options)
        {
            options=options??new SmartSelectionOptions();
            text = text ?? "";
            start = Math.Max(0, Math.Min(start, text.Length));
            var end = start + Math.Min(text.Length-start,Math.Max(0,length));
            var candidates = new List<SelectionRange>();
            if (!blockOnly && (length>0 || options.StartWithWord))
            {
                var left = start; var right = end;
                while (left > 0 && Word(text[left - 1])) left--;
                while (right < text.Length && Word(text[right])) right++;
                if ((options.SplitCase || options.SplitUnderscores) && right > left)
                {
                    var boundaries = new List<int> { left };
                    for (var i = left + 1; i < right; i++)
                        if ((options.SplitUnderscores && (text[i - 1] == '_' || text[i] == '_')) ||
                            (options.SplitCase && ((char.IsLower(text[i - 1]) && char.IsUpper(text[i])) ||
                            (char.IsUpper(text[i - 1]) && char.IsUpper(text[i]) && i + 1 < right && char.IsLower(text[i + 1]))))) boundaries.Add(i);
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
                    if (!blockOnly && options.SmallIncrements) {
                        var first=left+1;var last=i;
                        while(first<last&&char.IsWhiteSpace(text[first]))first++;
                        while(last>first&&char.IsWhiteSpace(text[last-1]))last--;
                        if(last>first)candidates.Add(new SelectionRange(first,last-first));
                        candidates.Add(new SelectionRange(left+1,i-left-1));
                    }
                    candidates.Add(new SelectionRange(left, i - left + 1));
                    if(blockOnly&&open=='{'){var declaration=DeclarationBlock(text,code,left,i);if(declaration!=null)candidates.Add(declaration);}
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
                if (options.TrimWhitespace)
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
        private static SelectionRange DeclarationBlock(string text,string code,int open,int close)
        {
            var boundary=open;
            while(boundary>0&&code[boundary-1]!=';'&&code[boundary-1]!='{'&&code[boundary-1]!='}')boundary--;
            var header=Regex.Match(code.Substring(boundary,open-boundary),@"(?m)^[ \t]*(?:(?:template\s*<[^;{}]+>)\s*)?(?:class|struct|union|enum(?:\s+(?:class|struct))?|namespace)\b[^;{}]*\z");
            if(!header.Success)return null;
            var end=close+1;
            while(end<text.Length&&(text[end]==' '||text[end]=='\t'))end++;
            if(end<text.Length&&text[end]==';')end++;
            var lineEnd=end;while(lineEnd<text.Length&&(text[lineEnd]==' '||text[lineEnd]=='\t'))lineEnd++;
            if(lineEnd<text.Length&&text[lineEnd]=='\r'&&lineEnd+1<text.Length&&text[lineEnd+1]=='\n')end=lineEnd+2;
            else if(lineEnd<text.Length&&text[lineEnd]=='\n')end=lineEnd+1;
            return new SelectionRange(boundary+header.Index,end-boundary-header.Index);
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
            return Extend(value,start,length,blockOnly,new SmartSelectionOptions {SplitCase=splitWords,SplitUnderscores=splitWords,TrimWhitespace=trimWhitespace});
        }
        public SelectionRange Extend(string value,int start,int length,bool blockOnly,SmartSelectionOptions options)
        {
            Synchronize(value, start, length);
            var next = CppSelection.Expand(value, start, length, blockOnly, options);
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
