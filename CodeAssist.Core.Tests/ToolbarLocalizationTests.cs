using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class ToolbarLocalizationTests {
 [Theory][InlineData("ko","심볼 찾기")][InlineData("en","Find Symbol")][InlineData("ko-KR","심볼 찾기")]
 public void ToolbarUsesRequestedLanguage(string language,string expected)=>Assert.Equal(expected,UiText.Get("Toolbar.FindSymbol",language));
 [Fact] public void AllToolbarLabelsHaveBothLanguages(){foreach(var key in new[]{"Settings","OpenFile","FindSymbol","FindReferences","PreviousContext","NextContext","Snippet","SpellCheck","CorrespondingFile","SkipBreakpoints","ThreadBreakpoints","Welcome"}){Assert.NotEqual("Toolbar."+key,UiText.Get("Toolbar."+key,"ko"));Assert.NotEqual("Toolbar."+key,UiText.Get("Toolbar."+key,"en"));}}
}
