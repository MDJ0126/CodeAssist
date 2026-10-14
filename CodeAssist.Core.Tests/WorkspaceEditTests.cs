using Newtonsoft.Json.Linq;
using CodeAssist.Semantics;
namespace CodeAssist.Core.Tests;
public class WorkspaceEditTests
{
    private static JObject Edit(int start, int end, string replacement) => new() { ["range"] = new JObject
        { ["start"] = new JObject { ["line"] = 0, ["character"] = start }, ["end"] = new JObject { ["line"] = 0, ["character"] = end } }, ["newText"] = replacement };
    [Fact]
    public void MultipleEditsUseOriginalUtf16Offsets()
    {
        Assert.Equal("😀 Longer + Other\r\n", WorkspaceEdits.Apply("😀 Value + Value\r\n", new JArray(Edit(3, 8, "Longer"), Edit(11, 16, "Other"))));
        Assert.Equal(4, WorkspaceEdits.Offset("x\r\nabc", 1, 1));
    }
    [Fact]
    public void RejectsOverlappingAndOutOfBoundsPlansBeforeChangingText()
    {
        Assert.Throws<ArgumentException>(() => WorkspaceEdits.Apply("Value", new JArray(Edit(0, 3, "x"), Edit(2, 5, "y"))));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkspaceEdits.Apply("Value", new JArray(Edit(0, 9, "x"))));
        Assert.Throws<ArgumentException>(() => WorkspaceEdits.Apply("Value", new JArray(Edit(0, 0, "x"), Edit(0, 0, "y"))));
    }
}
