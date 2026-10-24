using CodeAssist.Core;
using Xunit;
public class SymbolDefinitionDisplayTests {
 [Theory]
 [InlineData(" class ParityActor109", "class ParityActor109")]
 [InlineData(" int Health = 100;", "int Health = 100")]
 [InlineData("int GetHealth() const { return Health; }", "int GetHealth() const{...}")]
 [InlineData("void Tick(float DeltaTime) { Health--; }", "void Tick(float DeltaTime){...}")]
 [InlineData("const char* value = \"{safe}\";", "const char* value = \"{safe}\"")]
 [InlineData("int value; // note", "int value")]
 public void PreviewKeepsDeclarationAndHidesBody(string source,string expected)=>Assert.Equal(expected,SymbolDefinitionDisplay.FromLine(source,"fallback"));
 [Fact]public void MissingSourceUsesFallback()=>Assert.Equal("int Method()",SymbolDefinitionDisplay.FromLine(null,"int Method()"));
 [Fact]public void SeparateBodyBraceIsShown()=>Assert.Equal("class Actor{...}",SymbolDefinitionDisplay.FromLine("class Actor","fallback","{"));
 [Fact]public void ADeclarationDoesNotBorrowFollowingBody()=>Assert.Equal("int Method()",SymbolDefinitionDisplay.FromLine("int Method();","fallback","{"));
 [Fact]public void LiteralCommentMarkerIsNotRemoved()=>Assert.Equal("\"//\"",SymbolDefinitionDisplay.FromLine("\"//\"","fallback"));
}
