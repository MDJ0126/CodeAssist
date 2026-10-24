using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class ReferenceQueryContextTests
{
    [Theory]
    [InlineData("A.sln", "ProjectA", true)]
    [InlineData("a.SLN", "projecta", true)]
    [InlineData("B.sln", "ProjectA", false)]
    [InlineData("A.sln", "ProjectB", false)]
    [InlineData(null, null, false)]
    public void SwitchedSolutionOrIndexRejectsCapturedQuery(string solution, string root, bool expected)
    {
        var query = new ReferenceQueryContext("A.sln", "ProjectA");
        Assert.Equal(expected, query.Matches(solution, root));
    }
    [Fact]
    public async Task ProviderFinishingAfterProjectSwitchCannotBePublished()
    {
        var result = new TaskCompletionSource<string>();
        var query = new ReferenceQueryContext("A.sln", "ProjectA");
        var current = "A.sln";
        async Task<string?> Search()
        {
            var text = await result.Task;
            return query.Matches(current, "ProjectA") ? text : null;
        }
        var pending = Search();
        current = "B.sln";
        result.SetResult("old project references");
        Assert.Null(await pending);
    }
}
