using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class SmartSelectionPolicyTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void StatementInnerSelectionMatchesRecordedVa(bool trim)
    {
        var text="void Sample() { int value = 1; }\r\n";
        var range=CppSelection.Expand(text,20,5,false,new SmartSelectionOptions{TrimWhitespace=trim});
        Assert.Equal(16,range.Start);Assert.Equal(14,range.Length);
        var history=new SelectionHistory();history.Extend(text,20,5,false,new SmartSelectionOptions{TrimWhitespace=trim});
        var restored=history.Shrink(text,16,14);Assert.Equal(20,restored.Start);Assert.Equal(5,restored.Length);
    }
    [Fact] public void CaseAndUnderscoreOptionsAreIndependent()
    {
        var text="theValue_name";var start=text.IndexOf("Value")+1;
        var casing=CppSelection.Expand(text,start,0,false,new SmartSelectionOptions{SplitCase=true});
        Assert.Equal("Value_name",text.Substring(casing.Start,casing.Length));
        var underscore=CppSelection.Expand(text,start,0,false,new SmartSelectionOptions{SplitUnderscores=true});
        Assert.Equal("theValue",text.Substring(underscore.Start,underscore.Length));
    }
    [Fact] public void SmallIncrementsIncludeInnerExpressionBeforeParentheses()
    {
        const string text="Call(Left + Right);";var start=text.IndexOf("Right");
        var small=CppSelection.Expand(text,start,5,false,new SmartSelectionOptions{SmallIncrements=true});
        var large=CppSelection.Expand(text,start,5,false,new SmartSelectionOptions{SmallIncrements=false});
        Assert.Equal("Left + Right",text.Substring(small.Start,small.Length));Assert.Equal("(Left + Right)",text.Substring(large.Start,large.Length));
    }
    [Fact] public void StartWithoutCurrentWordMovesToStructuralSelection()
    {
        const string text="Use(Value);";var range=CppSelection.Expand(text,6,0,false,new SmartSelectionOptions{StartWithWord=false,SmallIncrements=false});
        Assert.Equal("(Value)",text.Substring(range.Start,range.Length));
    }
    [Fact] public void HugeSelectionLengthIsClampedWithoutIntegerOverflow()
    {var selection=CppSelection.Expand("Word",2,int.MaxValue,false);Assert.Equal(0,selection.Start);Assert.Equal(4,selection.Length);}
    [Fact] public void LegacyCombinedPreferenceMigratesAndIndependentOptionsPersist()
    {
        var original=new StartupPreferences{SmartSelectSplitWords=true};var migrated=original.Copy();Assert.True(migrated.SmartSelectSplitCase);Assert.True(migrated.SmartSelectSplitUnderscores);
        original.SmartSelectSplitCase=false;var independent=original.Copy();Assert.False(independent.SmartSelectSplitCase);Assert.True(independent.SmartSelectSplitUnderscores);
    }
}