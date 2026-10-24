using CodeAssist.Core;
public class CompletionFilterSelectionTests
{
    [Fact] public void PlainClickReplacesSelectionAndSecondClickClearsIt()
    {
        var selected=CompletionFilterSelection.Toggle(new[]{true,false,false},1,0,false,false);
        Assert.Equal(new[]{false,true,false},selected);
        Assert.Equal(new[]{false,false,false},CompletionFilterSelection.Toggle(selected,1,1,false,false));
    }
    [Fact] public void ControlAddsAndRemovesKindsWithoutDiscardingOtherSelections()
    {
        var selected=CompletionFilterSelection.Toggle(new[]{true,false,false},2,0,true,false);
        Assert.Equal(new[]{true,false,true},selected);
        Assert.Equal(new[]{true,false,false},CompletionFilterSelection.Toggle(selected,2,2,true,false));
    }
    [Theory] [InlineData(0,2)] [InlineData(2,0)]
    public void ShiftSelectsInclusiveRangeInEitherDirection(int anchor,int clicked)
    {
        var current=new bool[4];current[anchor]=true;
        Assert.Equal(new[]{true,true,true,false},CompletionFilterSelection.Toggle(current,clicked,anchor,false,true));
    }
    [Fact] public void ShiftWithoutAnchorFallsBackToOrdinaryClick()
    {
        Assert.Equal(new[]{false,true,false},CompletionFilterSelection.Toggle(new[]{true,false,false},1,-1,false,true));
    }
    [Fact] public void ShiftRetainsSelectionsOutsideRange()
    {
        Assert.Equal(new[]{true,true,true,true},CompletionFilterSelection.Toggle(new[]{false,true,false,true},0,2,true,true));
    }
}