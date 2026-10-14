using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class EditorNavigationTests
{
    [Fact]
    public void MatchesNativeGetHealthSelectionSequence()
    {
        var text = "    const auto Health = Actor.GetHealth();";
        var history = new SelectionHistory();
        var word = history.Extend(text, text.IndexOf("GetHealth") + 2, 0, false);
        Assert.Equal("GetHealth", text.Substring(word.Start, word.Length));
        var call = history.Extend(text, word.Start, word.Length, false);
        Assert.Equal("Actor.GetHealth();", text.Substring(call.Start, call.Length));
        var back = history.Shrink(text, call.Start, call.Length);
        Assert.Equal(word.Start, back.Start); Assert.Equal(word.Length, back.Length);
    }
    [Fact]
    public void HashtagsOnlyComeFromCommentsIncludingContinuedLines()
    {
        var text = "#include <x>\n auto s=R\"x(// #FAKE)x\";\n// #TODO\\\n #FIXME\n/* #TODO */\n#define FAKE #IGNORED";
        var tags = CppDocumentNavigation.Hashtags(text);
        Assert.Equal(new[] { "#TODO", "#FIXME", "#TODO" }, tags.Select(x => x.Name));
        Assert.Equal(new[] { 3, 4, 5 }, tags.Select(x => x.Line));
        Assert.All(tags, x => Assert.Equal(x.Name, text.Substring(x.Offset, x.Name.Length)));
    }
    [Fact]
    public void IncludesIgnoreBlockCommentsAndRawStrings()
    {
        var text = "/*\n#include \"Fake.h\"\n*/\n#include \"Real.h\"\n auto s=R\"x(\n#include <Fake2.h>\n)x\";\n#include <Engine/World.h>";
        Assert.Equal(new[] { "Real.h", "Engine/World.h" }, CppDocumentNavigation.Includes(text).Select(x => x.Name));
    }
    [Fact]
    public void SelectionRestoresEachStepAndInvalidatesHistoryAfterEdits()
    {
        var text = "void Run() { Call(Value); }";
        var history = new SelectionHistory(); var start = text.IndexOf("Value") + 2;
        var word = history.Extend(text, start, 0, false);
        Assert.Equal("Value", text.Substring(word.Start, word.Length));
        var expression = history.Extend(text, word.Start, word.Length, false);
        Assert.Equal("(Value)", text.Substring(expression.Start, expression.Length));
        var restored = history.Shrink(text, expression.Start, expression.Length);
        Assert.Equal(word.Start, restored.Start); Assert.Equal(word.Length, restored.Length);
        Assert.Equal(restored.Length, history.Shrink(text + " ", restored.Start, restored.Length).Length);
    }
    [Fact]
    public void BlockSelectionDoesNotUseBracesInsideLiterals()
    {
        var text = "void Run() { auto s=\"}\"; if (ok) { Call(); } }";
        var block = CppSelection.Expand(text, text.IndexOf("Call"), 0, true);
        Assert.Equal("{ Call(); }", text.Substring(block.Start, block.Length));
        var outer = CppSelection.Expand(text, block.Start, block.Length, true);
        Assert.Equal(text.IndexOf('{'), outer.Start);
    }
}
