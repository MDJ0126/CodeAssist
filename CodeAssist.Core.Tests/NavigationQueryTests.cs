using CodeAssist.Core;

namespace CodeAssist.Core.Tests;

public class NavigationQueryTests
{
    [Theory]
    [InlineData("main", "main.cpp")]
    [InlineData("main:6", "main.cpp")]
    [InlineData("Fixture -cpp", "FixtureActor.h")]
    [InlineData("Fixture cpp", "FixtureActor.cpp")]
    [InlineData("Fixture,main", "FixtureActor.cpp|FixtureActor.h|main.cpp")]
    [InlineData(".Fixture", "FixtureActor.cpp|FixtureActor.h")]
    [InlineData("cpp.", "FixtureActor.cpp|main.cpp")]
    [InlineData("#VAParity", "")]
    [InlineData("VAParity", "")]
    [InlineData("\\VAParity", "FixtureActor.cpp|FixtureActor.h|main.cpp")]
    public void MatchesObservedVAFileSets(string query, string expected)
    {
        var files = new[] { "FixtureActor.cpp", "FixtureActor.h", "main.cpp" };
        var filter = new NavigationQuery(query);
        Assert.Equal(expected, string.Join("|", files.Where(name => filter.Matches(name,
            "C:\\Projects\\CodeAssist\\tests\\VAParityFixture\\" + name))));
    }
    [Fact]
    public void LineQualifierDoesNotConsumeWindowsDriveColon()
    {
        var filter = new NavigationQuery("\\C:\\Projects\\main.cpp:6");
        Assert.Equal(6, filter.Line);
        Assert.True(filter.Matches("main.cpp", "C:\\Projects\\main.cpp"));
        Assert.Null(new NavigationQuery("main:0").Line);
        Assert.Null(new NavigationQuery("main:9999999999999").Line);
    }
}
