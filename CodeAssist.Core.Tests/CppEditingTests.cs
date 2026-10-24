using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class CppEditingTests
{
    [Fact] public void SlashTogglesIndentedLinesWithMixedLineEndingsAndSelectionAtNextLineStart()
    {
        var source="first\r\n    One();\r\n\tTwo();\nlast";var start=source.IndexOf("    One");var end=source.IndexOf("last");
        var change=CppEditing.Surround(source,start,end-start,'/',true)!;
        var result=source.Remove(change.Start,change.Length).Insert(change.Start,change.Text);
        Assert.Equal("first\r\n    // One();\r\n\t// Two();\nlast",result);
        var reverse=CppEditing.Surround(result,change.SelectionStart,change.SelectionLength,'/',true)!;
        Assert.Equal(source,result.Remove(reverse.Start,reverse.Length).Insert(reverse.Start,reverse.Text));
    }
    [Theory]
    [InlineData('/',"/*Value*/")]
    [InlineData('*',"/*Value*/")]
    [InlineData('(',"(Value)")]
    [InlineData('{',"{Value}")]
    public void PartialLineSelectionUsesRequestedSurround(char key,string expected)
    {
        var edit=CppEditing.Surround("Use(Value);",4,5,key,true)!;Assert.Equal(expected,edit.Text);Assert.Equal(4,edit.SelectionStart);Assert.Equal(expected.Length,edit.SelectionLength);
    }
    [Fact] public void BlockTogglePreservesSelectionContents()
    {var e=CppEditing.Surround("/* One */",0,9,'*',false)!;Assert.Equal(" One ",e.Text);}
    [Fact] public void MixedCommentedLinesAreCommentedWithoutLosingExistingMarkers()
    {var e=CppEditing.Surround("// One\nTwo",0,10,'/',false)!;Assert.Equal("//// One\n//Two",e.Text);}
    [Theory]
    [InlineData("    // text","    // ")]
    [InlineData("\t/// docs","\t/// ")]
    [InlineData("  /* text","   * ")]
    [InlineData("  /* text\n   * more","   * ")]
    [InlineData("const char* s=\"/* text\";",null)]
    [InlineData("/* done */",null)]
    [InlineData("Call(); // tail",null)]
    public void ContinuationUsesActualCommentContext(string text,string? expected)
    {Assert.Equal(expected,CppEditing.CommentContinuation(text,text.Length));}
    [Fact] public void InvalidOrEmptySelectionsDoNotCreateEdits()
    {Assert.Null(CppEditing.Surround("A",0,0,'/',true));Assert.Null(CppEditing.Surround("A",0,int.MaxValue,'/',true));Assert.Null(CppEditing.Surround("A",0,1,'x',true));}
}