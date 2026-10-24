using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class LatestOperationGateTests
{
    [Fact]
    public void OneOperationPerBindingAndCompletedTicketsCannotBeReused()
    {
        var gate = new LatestOperationGate();
        Assert.False(gate.IsRunning);
        Assert.False(gate.IsCurrent(null));
        Assert.False(gate.TryComplete(null));
        var first = gate.TryBegin();
        Assert.NotNull(first);
        Assert.Null(gate.TryBegin());
        Assert.True(gate.TryComplete(first));
        Assert.False(gate.TryComplete(first));
        var second = gate.TryBegin();
        Assert.NotSame(first, second);
        Assert.False(gate.IsCurrent(first));
        Assert.True(gate.IsCurrent(second));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupersededCompletionOrFailureCannotPublishOrUnlockNewRefresh(bool failOld)
    {
        var gate = new LatestOperationGate();
        var oldResult = new TaskCompletionSource<string>();
        var newResult = new TaskCompletionSource<string>();
        var published = new List<string>();
        var errors = new List<string>();
        async Task Refresh(Task<string> result)
        {
            var ticket = gate.TryBegin();
            Assert.NotNull(ticket);
            try
            {
                var value = await result;
                if (gate.IsCurrent(ticket)) published.Add(value);
            }
            catch (InvalidOperationException error)
            {
                if (gate.IsCurrent(ticket)) errors.Add(error.Message);
            }
            finally { gate.TryComplete(ticket); }
        }
        var oldTask = Refresh(oldResult.Task);
        gate.Reset(); // Bind a different search while the original provider is pending.
        var newTask = Refresh(newResult.Task);
        if (failOld) oldResult.SetException(new InvalidOperationException("old failure"));
        else oldResult.SetResult("old result");
        await oldTask;
        Assert.Empty(published);
        Assert.Empty(errors);
        Assert.True(gate.IsRunning);
        Assert.Null(gate.TryBegin());
        newResult.SetResult("new result");
        await newTask;
        Assert.Equal(new[] { "new result" }, published);
        Assert.False(gate.IsRunning);
    }
}
