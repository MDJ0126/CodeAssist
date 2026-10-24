using CodeAssist.Core;
public class FeedbackIssueLinkTests
{
    [Fact] public void OpensOnlyTheProjectIssueFormAndEncodesUserText()
    {
        var uri=new Uri(FeedbackIssueLink.Create("0.0.173","한글 &body=evil#fragment\nsecond line"));
        Assert.Equal("https",uri.Scheme);Assert.Equal("github.com",uri.Host);
        Assert.Equal("/MDJ0126/CodeAssist/issues/new",uri.AbsolutePath);Assert.Empty(uri.Fragment);
        Assert.Contains("한글 &body=evil#fragment\nsecond line",Uri.UnescapeDataString(uri.Query));
        Assert.Contains("CodeAssist 0.0.173",Uri.UnescapeDataString(uri.Query));
        Assert.Equal(2,uri.Query.TrimStart('?').Split('&').Length);
    }
    [Theory] [InlineData("en","Find Symbol Alt+Shift+S","Open File Alt+Shift+O","Open GitHub issue")]
    [InlineData("ko","심볼 찾기 Alt+Shift+S","파일 열기 Alt+Shift+O","GitHub 이슈 작성")]
    public void WelcomeActionsUseTheSelectedLanguage(string language,string symbols,string files,string issue)
    {
        Assert.Equal(symbols,UiText.Get("심볼 찾기 Alt+Shift+S",language));
        Assert.Equal(files,UiText.Get("파일 열기 Alt+Shift+O",language));
        Assert.Equal(issue,UiText.Get("Open GitHub issue",language));
    }
}