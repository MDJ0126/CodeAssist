using System.Linq;
using CodeAssist.Core;
using Xunit;
public class SymbolDisplayTokensTests {
 [Fact]public void QualifiedMemberDoesNotColorItsOwnerAsFunction(){var s="Actor::GetHealth";var t=SymbolDisplayTokens.Parse(s,SymbolKind.Function);Assert.Equal(new[]{DisplayInk.Type,DisplayInk.Function},t.Select(x=>x.Ink));Assert.Equal(new[]{"Actor","GetHealth"},t.Select(x=>s.Substring(x.Start,x.Length)));}
 [Fact]public void DottedOwnerUsesTypeInk(){var t=SymbolDisplayTokens.Parse("Actor.Health",SymbolKind.Variable);Assert.Equal(new[]{DisplayInk.Type,DisplayInk.Variable},t.Select(x=>x.Ink));}
 [Fact]public void FunctionSignatureSeparatesKeywordsAndParameters(){var t=SymbolDisplayTokens.Parse("Tick(float DeltaTime) const");Assert.Equal(new[]{DisplayInk.Function,DisplayInk.Keyword,DisplayInk.Variable,DisplayInk.Keyword},t.Select(x=>x.Ink));}
 [Fact]public void UnknownPlainCaptionInheritsDefault(){Assert.Empty(SymbolDisplayTokens.Parse("Solution Files"));}
 [Fact]public void CustomTypeParameterHasIndependentInk(){var t=SymbolDisplayTokens.Parse("Set(const Widget& value)");Assert.Equal(new[]{DisplayInk.Function,DisplayInk.Keyword,DisplayInk.Type,DisplayInk.Variable},t.Select(x=>x.Ink));}
 [Fact]public void ResolverProvidesStandaloneSymbolKind(){Assert.Equal(DisplayInk.Type,Assert.Single(SymbolDisplayTokens.Parse("Widget",resolve:n=>SymbolKind.Type)).Ink);}
 [Fact]public void EmptyTextIsSafe(){Assert.Empty(SymbolDisplayTokens.Parse(null));}
}
