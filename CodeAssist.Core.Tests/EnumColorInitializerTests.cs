using System.Linq;
using CodeAssist.Core;
using Xunit;
public class EnumColorInitializerTests
{
 [Fact]public void NestedInitializerCommasDoNotCreateMembers(){
  var tokens=CppColorCategories.Parse("enum E { First = Pick(1, Bogus), Second = int{3}, Third }; Bogus();");
  Assert.Equal(new[]{"First","Second","Third"},tokens.Where(t=>t.Kind==SemanticTokenKind.EnumMember).Select(t=>t.Name));
 }
 [Fact]public void IncompleteEnumDoesNotColorFollowingIdentifiers(){Assert.Empty(CppColorCategories.Parse("enum E { First, Second"));}
 [Fact]public void SpacedNestedNamespaceComponentsAreRecognized(){var tokens=CppColorCategories.Parse("namespace A :: B { } A::B;");Assert.Equal(4,tokens.Count(t=>t.Kind==SemanticTokenKind.Namespace));}
}
