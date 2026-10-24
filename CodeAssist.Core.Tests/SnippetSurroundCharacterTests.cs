using CodeAssist.Core;
using System.Collections.Generic;
using Xunit;
public class SnippetSurroundCharacterTests {
 [Theory]
 [InlineData("SurroundWith=n","",'n')]
 [InlineData("wrap SurroundWith=n","wrap",'n')]
 [InlineData("assert SurroundWith=A","assert",'A')]
 [InlineData("SurroundWith=@","",'@')]
 public void MetadataIsIndependentOfNormalShortcut(string header,string shortcut,char key){var t=new SnippetTemplate("Audit",header,"AUDIT($selected$)$end$");Assert.Equal(shortcut,t.Shortcut);Assert.Equal(key,t.SurroundingCharacter);Assert.True(t.CanSurround(key));Assert.Equal("AUDIT(value)",t.Expand(new Dictionary<string,string>{{"selected","value"}}).Text);}
 [Theory][InlineData("n")][InlineData("SurroundWith=nn")][InlineData("fooSurroundWith=n")]
 public void AShortcutAloneDoesNotEnableSurrounding(string header){Assert.Null(new SnippetTemplate("Audit",header,"$selected$").SurroundingCharacter);}
 [Fact]public void NoSelectionPlaceholderDoesNotSurround(){Assert.False(new SnippetTemplate("Audit","SurroundWith=n","value").CanSurround('n'));}
 [Fact]public void EnvironmentExpansionRetainsSurroundMetadata(){var t=new SnippetTemplate("Audit","wrap SurroundWith=n","%PREFIX%($selected$)").WithEnvironment(_=>"AUDIT");Assert.True(t.CanSurround('n'));Assert.Equal("wrap",t.Shortcut);}
 [Fact]public void FileParserRetainsMetadata(){var t=Assert.Single(SnippetTemplate.Parse("a:Audit:wrap SurroundWith=n:\nAUDIT($selected$)$end$\f"));Assert.True(t.CanSurround('n'));}
}
