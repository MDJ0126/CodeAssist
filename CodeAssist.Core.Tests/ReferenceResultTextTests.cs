using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class ReferenceResultTextTests
{
    [Theory]
    [InlineData("ko", "CodeAssist 참조", "컴파일러 참조", "참조 후보")]
    [InlineData("ko-KR", "CodeAssist 참조", "컴파일러 참조", "참조 후보")]
    [InlineData("en-US", "CodeAssist References", "Compiler references", "Reference candidates")]
    [InlineData("fr-FR", "CodeAssist References", "Compiler references", "Reference candidates")]
    public void TitlesAndDescriptionsRespectLanguageAndFallback(string language, string title, string compiler, string candidates)
    {
        Assert.Equal(title, ReferenceResultText.WindowTitle(language));
        Assert.Equal("Type::Get{Value} — " + compiler, ReferenceResultText.Description("Type::Get{Value}", true, language));
        Assert.Equal("Type::Get{Value} — " + candidates, ReferenceResultText.Description("Type::Get{Value}", false, language));
        var clone = ReferenceResultText.CloneTitle("Type::Get{Value}", language);
        Assert.EndsWith(": Type::Get{Value}", clone);
        var summary = ReferenceResultText.Summary("Type::Get{Value}", 12, true, language);
        Assert.Contains("Type::Get{Value}", summary);
        Assert.Contains("12", summary);
        Assert.Equal(language.StartsWith("ko") ? "CodeAssist: 'Type::Get{Value}' 참조 후보 12개 (어휘·지역 블록 기준)" : "CodeAssist: 'Type::Get{Value}' reference candidates 12 (lexical/local-block scope)", ReferenceResultText.Summary("Type::Get{Value}", 12, false, language));
    }
}
