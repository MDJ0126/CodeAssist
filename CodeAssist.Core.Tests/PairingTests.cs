using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class PairingTests
{
    [Theory]
    [InlineData("Call",4,'(',true)]
    [InlineData("// comment",10,'(',false)]
    [InlineData("/* comment",10,'[',false)]
    [InlineData("\"literal",8,'{',false)]
    [InlineData("R\"tag(raw",9,'(',false)]
    [InlineData("1",1,'\'',false)]
    [InlineData("Word",0,'(',false)]
    [InlineData("",0,'\'',true)]
    public void OnlyPairsInSafeCodeContexts(string text,int caret,char key,bool expected)
    {Assert.Equal(expected,CppPairing.CanOpen(text,caret,key));}
    [Theory]
    [InlineData("Call(",5,'(',")")]
    [InlineData("Arr[",4,'[',"]")]
    [InlineData("\"",1,'"',"\"")]
    public void InsertsOnlyMissingClosingCharacter(string text,int caret,char key,string expected)
    {Assert.Equal(expected,CppPairing.AfterOpening(text,caret,key,ClosingBracePlacement.SameLine)!.Text);}
    [Fact] public void ExistingIdePairIsNeverDuplicated()
    {Assert.Null(CppPairing.AfterOpening("Call()",5,'(',ClosingBracePlacement.SameLine));}
    [Fact] public void BlankLineBraceKeepsCrLfAndTabIndentation()
    {
        var edit=CppPairing.AfterOpening("Header\r\n\t{",10,'{',ClosingBracePlacement.BlankLine,"\t")!;
        Assert.Equal("\r\n\t\t\r\n\t}",edit.Text);Assert.Equal('}',edit.Text[edit.ClosingOffset]);Assert.Equal(4,edit.CaretOffset);
    }
    [Theory]
    [InlineData("Actor.Method",12,"Method",true)]
    [InlineData("&Method",7,"Method",false)]
    [InlineData("Method()",7,"Method",false)]
    [InlineData("Method (x)",6,"Method",false)]
    [InlineData("#define Method",14,"Method",false)]
    public void CompletionCallInsertionPreservesExistingArgumentsAndAddressExpressions(string text,int caret,string name,bool expected)
    {Assert.Equal(expected,CppPairing.CanAddMethodCall(text,caret,name));}
    [Theory]
    [InlineData("Call",4,0,"Call()",5,'(',true)]
    [InlineData("Call)",4,0,"Call()",5,'(',false)]
    [InlineData("x",0,1,"()",1,'(',true)]
    [InlineData("x",0,0,"()x",1,'(',true)]
    [InlineData("Call",4,0,"Call( )",5,'(',false)]
    [InlineData("Call",4,0,"Call()",6,'(',false)]
    public void OnlyOwnsExactlyNewlyGeneratedNativePairs(string before,int start,int length,string after,int caret,char key,bool expected)
    {Assert.Equal(expected,CppPairing.IsFreshPair(before,start,length,after,caret,key));}
}