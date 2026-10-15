using System;
using System.Linq;
namespace CodeAssist.Core
{
    public sealed class SortedLineEdit
    {
        public SortedLineEdit(int start, int length, string text) { Start=start; Length=length; Text=text; }
        public int Start { get; } public int Length { get; } public string Text { get; }
    }
    public static class SelectedLineSort
    {
        public static SortedLineEdit Create(string text, int start, int length, bool descending, bool ignoreCase)
        {
            if (text == null || start < 0 || length < 0 || start > text.Length - length) throw new ArgumentOutOfRangeException();
            if (length == 0) return new SortedLineEdit(start, 0, "");
            var first = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;
            var end = start + length;
            if (text[end - 1] != '\n') { var next = text.IndexOf('\n', end); end = next < 0 ? text.Length : next + 1; }
            var selected = text.Substring(first, end - first);
            var newline = selected.Contains("\r\n") ? "\r\n" : text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = selected.Replace("\r\n", "\n").Split('\n');
            if (selected.EndsWith("\n", StringComparison.Ordinal)) lines = lines.Take(lines.Length - 1).ToArray();
            var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var sorted = descending ? lines.OrderByDescending(line => line, comparer) : lines.OrderBy(line => line, comparer);
            return new SortedLineEdit(first, end-first, string.Join(newline, sorted) + newline);
        }
    }
}
