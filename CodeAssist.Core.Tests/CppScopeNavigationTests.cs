using CodeAssist.Core;

namespace CodeAssist.Core.Tests;
public class CppScopeNavigationTests
{
    [Fact]
    public void MovesToEnclosingBoundaryThenAdjacentGlobalBlock()
    {
        var text = "void One() { Call(); /* } */ }\nvoid Two() { Done(); }";
        var open = text.IndexOf('{'); var close = text.IndexOf("}\n");
        Assert.Equal(close, CppScopeNavigation.Move(text, text.IndexOf("Call"), true));
        Assert.Equal(open, CppScopeNavigation.Move(text, close, false));
        Assert.Equal(0, CppScopeNavigation.Move(text, open, false));
        Assert.Equal(text.LastIndexOf('{'), CppScopeNavigation.Move(text, close, true));
    }
    [Fact]
    public void NestedParenthesesAndBlocksMoveOutward()
    {
        var text = "void Run() { if (Check(Value)) { Use(); } }";
        var position = text.IndexOf("Value");
        var close = CppScopeNavigation.Move(text, position, true);
        Assert.Equal(text.IndexOf(')', position), close);
        Assert.Equal(close + 1, CppScopeNavigation.Move(text, close, true));
        Assert.Equal(text.LastIndexOf('}'), CppScopeNavigation.Move(text, close + 1, true));
    }
}
