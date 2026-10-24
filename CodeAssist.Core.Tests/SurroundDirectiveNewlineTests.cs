using CodeAssist.Core;
using Xunit;
public class SurroundDirectiveNewlineTests {
 [Theory]
 [InlineData("\r\n")]
 [InlineData("\n")]
 public void ExistingFollowingLineBreakIsUsedExactlyOnce(string nl) {
  var source="class Actor"+nl+"{"+nl+"\tint Health = 100;"+nl+"\tint GetHealth();"+nl+"};";
  var start=source.IndexOf("int Health");var change=CppEditing.SurroundDirective(source,start,"int Health = 100;".Length);
  var actual=source.Remove(change.Start,change.Length).Insert(change.Start,change.Text);
  Assert.Equal("class Actor"+nl+"{"+nl+"\t#ifdef _DEBUG"+nl+"int Health = 100;"+nl+"#endif // _DEBUG"+nl+"\tint GetHealth();"+nl+"};",actual);
 }
 [Fact]public void TerminalSelectionRetainsOneEndingLineBreak(){var c=CppEditing.SurroundDirective("value",0,5);Assert.Equal("#ifdef _DEBUG\nvalue\n#endif // _DEBUG\n",c.Text);}
 [Fact]public void SelectionIncludingLineBreakDoesNotJoinNextDeclaration(){var c=CppEditing.SurroundDirective("value\nnext",0,6);Assert.Equal("#ifdef _DEBUG\nvalue\n#endif // _DEBUG\n",c.Text);}
}
