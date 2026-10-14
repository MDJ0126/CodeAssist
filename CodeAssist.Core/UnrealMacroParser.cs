using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeAssist.Core
{
    public sealed class MacroOccurrence
    {
        public MacroOccurrence(string name, int start, int length)
        {
            Name = name;
            Start = start;
            Length = length;
        }

        public string Name { get; }
        public int Start { get; }
        public int Length { get; }
    }

    public static class UnrealMacroParser
    {
        private static readonly Regex UnrealMacroPattern = new Regex(
            @"\b(?:UCLASS|USTRUCT|UENUM|UINTERFACE|UFUNCTION|UDELEGATE|UPROPERTY|UMETA|UPARAM|GENERATED_BODY|GENERATED_UCLASS_BODY|GENERATED_USTRUCT_BODY|GENERATED_UINTERFACE_BODY|GENERATED_IINTERFACE_BODY|RIGVM_METHOD|[A-Z][A-Z0-9_]*_API|DECLARE_[A-Z0-9_]+|DEFINE_[A-Z0-9_]+|IMPLEMENT_[A-Z0-9_]+)\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static IReadOnlyList<MacroOccurrence> Find(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<MacroOccurrence>();
            }

            var results = new List<MacroOccurrence>();
            foreach (Match match in UnrealMacroPattern.Matches(CppCodeMask.Create(text)))
            {
                results.Add(new MacroOccurrence(match.Value, match.Index, match.Length));
            }

            return results;
        }
    }
}
