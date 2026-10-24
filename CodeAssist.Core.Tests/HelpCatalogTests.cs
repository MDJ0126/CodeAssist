using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class HelpCatalogTests
{
    [Theory]
    [InlineData("ko","시작하기")]
    [InlineData("ko-KR","시작하기")]
    [InlineData("en-US","Getting started")]
    [InlineData("fr-FR","Getting started")]
    public void EmbeddedHelpResolvesLanguageWithoutFileOrNetwork(string language,string title)
    {
        var sections=HelpCatalog.Sections(language);Assert.Equal(8,sections.Length);
        Assert.Equal(8,sections.Select(s=>s.Id).Distinct().Count());Assert.Equal(title,sections[0].Title);
        Assert.All(sections,s=>Assert.NotEmpty(s.Paragraphs));
    }
    [Fact] public void EveryWelcomeHelpTargetExists()
    {foreach(var id in new[]{"startup","features","colors","navigation","inspection","shortcuts"})Assert.Contains(HelpCatalog.Sections("ko"),s=>s.Id==id);}
    [Fact] public void SearchIncludesParagraphTextAndNoResultIsEmpty()
    {Assert.Contains(HelpCatalog.Search("ko","compile_commands"),s=>s.Id=="navigation");Assert.Empty(HelpCatalog.Search("ko","unmatchable-6fa8288e"));}
    [Fact] public void WelcomeHeadingAndCardsTranslateToKorean()
    {
        Assert.Equal("CodeAssist에 오신 것을 환영합니다",string.Format(UiText.Get("Welcome to {0}","ko"),"CodeAssist"));
        Assert.Equal("프로젝트 코드 검사",UiText.Get("PROJECT CODE INSPECTION","ko"));
        Assert.Equal("CodeAssist의 주요 기능",UiText.Get("What's New with CodeAssist","ko"));
    }
    [Theory][InlineData("ko")][InlineData("en")]
    public void ShortcutRowsKeepKeysSeparateFromDescriptions(string language)
    {
        var section=HelpCatalog.Sections(language).Single(s=>s.Id=="shortcuts");
        var rows=section.Blocks.Where(b=>b.Kind=="shortcut").ToArray();
        Assert.Equal(15,rows.Length);Assert.Equal(15,rows.Select(b=>b.Label).Distinct().Count());
        Assert.All(rows,b=>{Assert.DoesNotContain(" · ",b.Text);Assert.DoesNotContain("\n",b.Label);Assert.NotEmpty(b.Text);});
        Assert.Contains(rows,b=>b.Label=="Alt+Shift+S");
        Assert.Contains(HelpCatalog.Search(language,"Alt+Shift+S"),s=>s.Id=="shortcuts");
    }
    [Theory][InlineData("ko")][InlineData("en")]
    public void EveryHelpTopicHasSubheadingsAndBody(string language)
    {foreach(var section in HelpCatalog.Sections(language)){Assert.Contains(section.Blocks,b=>b.Kind=="heading");Assert.NotEmpty(section.Paragraphs);}}
}
