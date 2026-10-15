using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeAssist.Core
{
    public enum SemanticTokenKind
    {
        Type,
        Variable,
        Function
    }

    public sealed class SemanticToken
    {
        public SemanticToken(string name, int start, int length, SemanticTokenKind kind)
        {
            Name = name;
            Start = start;
            Length = length;
            Kind = kind;
        }

        public string Name { get; }
        public int Start { get; }
        public int Length { get; }
        public SemanticTokenKind Kind { get; }
    }

    public static class CppSemanticParser
    {
        private static readonly Regex IdentifierPattern = new Regex(@"\b[A-Za-z_]\w*\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex DeclaredTypePattern = new Regex(
            @"\b(?:class|struct|enum(?:\s+class)?)\s+(?:\w+_API\s+)?(?<name>[A-Za-z_]\w*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex UnrealTypePattern = new Regex(
            @"\b(?<name>(?:[AUFTEI][A-Z][A-Za-z0-9_]*))\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex UnrealClassInheritancePattern = new Regex(
            @"\bclass\s+(?:\w+_API\s+)?[A-Za-z_]\w*\s*:\s*(?:public|protected|private)\s+[A-Za-z_]\w*",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GeneratedSuperUsagePattern = new Regex(
            @"\bSuper\s*::",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex VariableDeclarationPattern = new Regex(
            @"\b(?:const\s+)?(?:auto|bool|char|short|int|long|float|double|int\d+|uint\d+|[AUFTEI][A-Z]\w*|[A-Za-z_]\w*\s*<[^;{}()]+>)\s*[*&]?\s*(?<name>[a-zA-Z_]\w*)\s*(?=[=;,\)\[])" ,
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex FunctionUsagePattern = new Regex(
            @"\b(?<name>[A-Za-z_]\w*)\s*(?=\()",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> NonFunctionKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "alignas", "alignof", "catch", "decltype", "delete", "for", "if", "new",
            "noexcept", "requires", "return", "sizeof", "static_assert", "switch", "throw",
            "typeid", "while"
        };

        public static IReadOnlyList<SemanticToken> Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<SemanticToken>();
            }

            var typeNames = CollectNames(text, DeclaredTypePattern);
            foreach (Match match in UnrealTypePattern.Matches(text))
            {
                var name = match.Groups["name"].Value;
                if (!IsUnrealMacro(name))
                {
                    typeNames.Add(name);
                }
            }

            if (GeneratedSuperUsagePattern.IsMatch(text)
                || (UnrealClassInheritancePattern.IsMatch(text)
                    && (text.IndexOf("GENERATED_BODY", StringComparison.Ordinal) >= 0
                        || text.IndexOf("UCLASS", StringComparison.Ordinal) >= 0)))
            {
                typeNames.Add("Super");
            }

            var variableNames = CollectNames(text, VariableDeclarationPattern);
            variableNames.ExceptWith(typeNames);

            var tokens = new List<SemanticToken>();
            foreach (Match identifier in IdentifierPattern.Matches(text))
            {
                var name = identifier.Value;
                if (typeNames.Contains(name)) tokens.Add(new SemanticToken(name, identifier.Index, identifier.Length, SemanticTokenKind.Type));
                else if (variableNames.Contains(name)) tokens.Add(new SemanticToken(name, identifier.Index, identifier.Length, SemanticTokenKind.Variable));
            }
            AddFunctionOccurrences(text, tokens);
            return tokens.OrderBy(token => token.Start).ToArray();
        }

        private static HashSet<string> CollectNames(string text, Regex pattern)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in pattern.Matches(text))
            {
                names.Add(match.Groups["name"].Value);
            }

            return names;
        }

        private static void AddFunctionOccurrences(string text, ICollection<SemanticToken> tokens)
        {
            var functions = new List<SemanticToken>();
            var positions = new HashSet<int>();
            foreach (Match match in FunctionUsagePattern.Matches(text))
            {
                var group = match.Groups["name"];
                var name = group.Value;
                if (NonFunctionKeywords.Contains(name) || IsUnrealMacro(name))
                {
                    continue;
                }

                positions.Add(group.Index);
                functions.Add(new SemanticToken(name, group.Index, group.Length, SemanticTokenKind.Function));
            }
            var list = (List<SemanticToken>)tokens;
            list.RemoveAll(token => positions.Contains(token.Start));
            list.AddRange(functions);
        }

        private static bool IsUnrealMacro(string name)
        {
            return UnrealMacroParser.Find(name).Count > 0;
        }
    }
}
