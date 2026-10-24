using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class DeclarationBlockSelectionTests {
 [Theory]
 [InlineData("class Widget\r\n{\r\n int value;\r\n};\r\n", "\r\n")]
 [InlineData("struct Widget : Base { int value; };\n", "\n")]
 public void HeaderSelectionStopsAfterDeclarationRatherThanSelectingNextFunction(string declaration,string newline) {
  var text=declaration+newline+"int main() { return 0; }"+newline;
  var caret=text.IndexOf("Widget",StringComparison.Ordinal);var selected=CppSelection.Expand(text,caret,0,true);
  Assert.Equal(declaration,text.Substring(selected.Start,selected.Length));
  var history=new SelectionHistory();var forward=history.Extend(text,caret,0,true);var back=history.Shrink(text,forward.Start,forward.Length);Assert.Equal(caret,back.Start);Assert.Equal(0,back.Length);
 }
 [Fact]public void NestedFunctionBodyStillChoosesItsSmallerBraceBlock() {
  var text="class Widget { void Run() { Call(); } };\nint main() {}";
  var selected=CppSelection.Expand(text,text.IndexOf("Call",StringComparison.Ordinal),0,true);
  Assert.Equal("{ Call(); }",text.Substring(selected.Start,selected.Length));
 }
 [Fact]public void RawLiteralLookalikesCannotCreateDeclarationBlocks() {
  var text="const char* sample = R\"x(class Fake { })x\";\nclass Real\n{\n int value;\n};\nint main() {}";
  var selected=CppSelection.Expand(text,text.IndexOf("Real",StringComparison.Ordinal),0,true);
  Assert.Equal("class Real\n{\n int value;\n};\n",text.Substring(selected.Start,selected.Length));
 }
}
