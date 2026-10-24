using CodeAssist.Core;
using Xunit;
public class ReferenceContextMapTests
{
 [Theory]
 [InlineData("int main() { Hit(); }", "Hit", "main")]
 [InlineData("class Actor { int Get() { return Health; } };", "Health", "Actor")]
 [InlineData("int main() { if (true) { Hit(); } }", "Hit", "main")]
 [InlineData("void Actor::Tick() { Hit(); }", "Hit", "Actor")]
 [InlineData("void One() { Hit(); } int value;", "value", "")]
 [InlineData("void One() { /* } */ Hit(); }", "Hit", "One")]
 [InlineData("void One() { auto s=\"}\"; Hit(); }", "Hit", "One")]
 public void ResolvesEnclosingDisplayScope(string code,string marker,string expected)
  => Assert.Equal(expected,new ReferenceContextMap(code).At(code.IndexOf(marker,System.StringComparison.Ordinal)));
}
