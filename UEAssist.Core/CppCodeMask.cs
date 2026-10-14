using System;

namespace UEAssist.Core
{
    /// <summary>Preserves offsets and newlines while removing comments and C++ literals.</summary>
    public static class CppCodeMask
    {
        public static string Create(string text)
        {
            return Create(text, false);
        }
        public static string Comments(string text)
        {
            return Create(text, true);
        }
        private static string Create(string text, bool commentsOnly)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            var chars = text.ToCharArray();
            if (commentsOnly)
                for (var p = 0; p < chars.Length; p++) if (chars[p] != '\n' && chars[p] != '\r') chars[p] = ' ';
            for (var i = 0; i < text.Length;)
            {
                var start = i;
                var comment = false;
                if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    comment = true;
                    i += 2;
                    while (i < text.Length)
                    {
                        if (text[i] == '\n')
                        {
                            var previous = i > 0 && text[i - 1] == '\r' ? i - 2 : i - 1;
                            if (previous < 0 || text[previous] != '\\') break;
                        }
                        i++;
                    }
                }
                else if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    comment = true;
                    var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 2;
                }
                else if (text[i] == 'R' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    start = PrefixStart(text, start);
                    var open = text.IndexOf('(', i + 2);
                    if (open < 0 || open - i > 18) { i++; continue; }
                    var delimiter = text.Substring(i + 2, open - i - 2);
                    var end = text.IndexOf(")" + delimiter + "\"", open + 1, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + delimiter.Length + 2;
                }
                else if (text[i] == '"' || text[i] == '\'')
                {
                    // A digit separator (1'000) is not a character literal.
                    if (text[i] == '\'' && IsDigitSeparator(text, i)) { i++; continue; }
                    start = PrefixStart(text, start);
                    var quote = text[i++];
                    while (i < text.Length)
                    {
                        if (text[i] == '\\') { i = Math.Min(text.Length, i + 2); continue; }
                        if (text[i++] == quote) break;
                    }
                }
                else { i++; continue; }
                for (var p = start; p < i; p++)
                    if (chars[p] != '\n' && chars[p] != '\r') chars[p] = commentsOnly && comment ? text[p] : ' ';
            }
            return new string(chars);
        }

        private static int PrefixStart(string text, int offset)
        {
            foreach (var prefix in new[] { "u8", "u", "U", "L" })
            {
                var start = offset - prefix.Length;
                if (start >= 0 && string.CompareOrdinal(text, start, prefix, 0, prefix.Length) == 0 &&
                    (start == 0 || (!char.IsLetterOrDigit(text[start - 1]) && text[start - 1] != '_'))) return start;
            }
            return offset;
        }
        private static bool IsDigitSeparator(string text, int offset)
        {
            if (offset == 0 || offset + 1 >= text.Length || !char.IsLetterOrDigit(text[offset + 1])) return false;
            var start = offset - 1;
            while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '\'' || text[start - 1] == '.')) start--;
            return char.IsDigit(text[start]);
        }
    }
}
