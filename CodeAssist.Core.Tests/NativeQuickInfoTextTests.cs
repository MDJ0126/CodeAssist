using CodeAssist.Core;
using Xunit;
public class NativeQuickInfoTextTests
{
 [Fact]public void CapturedCppSignatureNormalizesInvisibleSemanticMarkers(){
  Assert.Equal("int PaletteProbe142::Read()",NativeQuickInfoText.Normalize("int \u200B\u200C\u200B\u200C\u200CPaletteProbe142\u200B::\u200B\u200C\u200B\u200CRead\u200B()"));
 }
 [Theory]
 [InlineData("cppFunction","\u200B\u200CRead\u200B","Read",true)]
 [InlineData("cppLocalVariable","Health","Health",true)]
 [InlineData("text","function Read may be static","Read",false)]
 [InlineData("cppFunction","ReadMore","Read",false)]
 [InlineData("cppNamespace","네임스페이스","네임스페이스",true)]
 public void NativeSymbolRequiresSemanticClassificationAndExactDisplayedName(string classification,string text,string name,bool expected){Assert.Equal(expected,NativeQuickInfoText.IsSymbolRun(classification,text,name));}
}
