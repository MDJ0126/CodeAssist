using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class ReferenceResultNavigationTests
{
    private static readonly ReferenceLocation[] Rows={new("Fixture.cpp",4,6),new("Fixture.cpp",5,33),new("Fixture.cpp",6,34)};
    [Theory]
    [InlineData(4,12,1,1)]
    [InlineData(5,39,1,2)]
    [InlineData(6,40,1,0)]
    [InlineData(4,6,-1,2)]
    [InlineData(5,33,-1,0)]
    public void EditorCaretResolvesCurrentReferenceAfterResultsLoseFocus(int line,int column,int direction,int expected)
        =>Assert.Equal(expected,ReferenceResultNavigation.Next(Rows,-1,"fixture.CPP",line,column,6,direction));
    [Fact] public void EmptyAndUnrelatedPositionsFallBackWithoutInvalidIndex()
    {
        Assert.Equal(-1,ReferenceResultNavigation.Next(Array.Empty<ReferenceLocation>(),-1,"Fixture.cpp",4,6,6,1));
        Assert.Equal(0,ReferenceResultNavigation.Next(Rows,-1,"Other.cpp",4,6,6,1));
        Assert.Equal(2,ReferenceResultNavigation.Next(Rows,-1,null,0,0,6,-1));
    }
    [Fact] public void FilteredRowsWrapWithinVisibleResults()
    {
        Assert.Equal(0,ReferenceResultNavigation.Next(new[]{Rows[2]},-1,"Fixture.cpp",6,40,6,1));
        Assert.Equal(2,ReferenceResultNavigation.Next(Rows,0,"Fixture.cpp",5,39,6,1));
    }
}
