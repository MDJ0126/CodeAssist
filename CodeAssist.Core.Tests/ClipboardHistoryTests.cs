using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class ClipboardHistoryTests
{
    [Fact]
    public void DuplicateMovesToFrontAndCapacityEvictsOldest()
    {
        var history = new ClipboardHistory(2);
        history.Record("one"); history.Record("two"); history.Record("one");
        Assert.Equal(new[] { "one", "two" }, history.Items());
        history.Record("three"); Assert.Equal(new[] { "three", "one" }, history.Items());
        var snapshot = history.Items(); snapshot[0] = "changed";
        Assert.Equal("three", history.Items()[0]);
    }
    [Fact]
    public void BlankAndOversizeEntriesDoNotEvictUsefulHistory()
    {
        var history = new ClipboardHistory(1); history.Record("code");
        history.Record(""); history.Record(new string('x', 1024 * 1024 + 1));
        Assert.Equal(new[] { "code" }, history.Items());
    }
}
