using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class LocalizationAndSelectionTests
{
    [Theory]
    [InlineData("en", "Save")]
    [InlineData("en-US", "Save")]
    [InlineData("ko", "저장")]
    [InlineData("ko-KR", "저장")]
    [InlineData("fr-FR", "Save")]
    public void ResolvesRegionAndFallsBackToEnglish(string language,string expected)
        => Assert.Equal(expected, UiText.Get("저장",language));
    [Fact] public void CatalogExposesEveryAvailableLanguage()
    { Assert.Contains("ko",UiText.LanguageCodes);Assert.Contains("en",UiText.LanguageCodes);Assert.Equal("한국어",UiText.LanguageName("ko")); }
    [Fact] public void UnknownSourceAndTechnicalTextArePreserved()
        => Assert.Equal("Type::Method",UiText.Get("Type::Method","ko"));
    [Fact] public void NewPreferencesSurviveCopyWithoutMutatingOriginal()
    {
        var settings=new StartupPreferences { UiLanguage="en",SmartSelectSplitWords=true,SmartSelectTrimWhitespace=true };
        var copy=settings.Copy(); Assert.Equal("en",copy.UiLanguage); Assert.True(copy.SmartSelectSplitWords); Assert.True(copy.SmartSelectTrimWhitespace);
        copy.UiLanguage="ko"; Assert.Equal("en",settings.UiLanguage);
    }
    [Theory]
    [InlineData("getHTTPResponse", 4, "HTTP")]
    [InlineData("my_variable", 5, "variable")]
    [InlineData("CamelCase", 7, "Case")]
    public void SplitsCaseUnderscoreAndAcronym(string value,int caret,string expected)
    {
        var selected=CppSelection.Expand(value,caret,0,false,true,false);
        Assert.Equal(expected,value.Substring(selected.Start,selected.Length));
    }
    [Fact] public void TrimmedLineExpandsToWholeLineAndShrinksExactly()
    {
        var text="    Alpha + Beta;  ";var history=new SelectionHistory();
        var range=history.Extend(text,4,5,false,false,true); Assert.Equal("Alpha + Beta;",text.Substring(range.Start,range.Length));
        var next=history.Extend(text,range.Start,range.Length,false,false,true);Assert.Equal(text,next.Start==0?text.Substring(next.Start,next.Length):"");
        var previous=history.Shrink(text,next.Start,next.Length);Assert.Equal(range.Start,previous.Start);Assert.Equal(range.Length,previous.Length);
    }
}