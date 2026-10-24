using System;
using System.IO;
using System.Linq;
using CodeAssist.Indexing;
using Xunit;
public class QuickInfoSymbolLookupTests {
 [Fact]public void TooltipLookupReturnsExactSignatureWithoutNavigationQueryMarkers(){
  var root=Path.Combine(Path.GetTempPath(),"CodeAssistQuickInfo",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  try {var file=Path.Combine(root,"Probe.cpp");File.WriteAllText(file,"class Actor { public: int Health = 100; int GetHealth() const { return Health; } int GetHealthExtra() { return 1; } };\n");var index=new PersistentSymbolIndex();index.BuildProject(root);var found=index.FindSymbolDetails("GetHealth");Assert.NotEmpty(found);Assert.All(found,s=>Assert.Equal("GetHealth",s.Name));Assert.Contains(found,s=>s.Signature.Contains("GetHealth()"));Assert.Empty(index.FindSymbolDetails("gethealth"));Assert.Empty(index.FindSymbolDetails(null));}
  finally {Directory.Delete(root,true);}
 }
}
