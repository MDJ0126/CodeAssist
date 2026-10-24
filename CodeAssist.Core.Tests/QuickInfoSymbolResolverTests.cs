using CodeAssist.Core;
using CodeAssist.Indexing;
using Xunit;
public class QuickInfoSymbolResolverTests
{
 [Fact]public void HoverOnDeclarationExcludesForeignSameNameFunction(){
  var current=new IndexedSymbol("Read",SymbolKind.Function,"probe.cpp",6,9,valueType:"int",signature:"Read()");
  var foreign=new IndexedSymbol("Read",SymbolKind.Function,"other.cpp",4,9,valueType:"void",signature:"Read()");
  Assert.Same(current,Assert.Single(QuickInfoSymbolResolver.Resolve(new[]{foreign,current},"Read","probe.cpp",6,null)));
  Assert.Equal("int Read()",QuickInfoSymbolResolver.DisplaySignature(current));
 }
 [Fact]public void AmbiguousForeignOverloadsDoNotProduceMisleadingTooltip(){
  var a=new IndexedSymbol("Read",SymbolKind.Function,"a.h",2,1,"A",signature:"Read(int)");
  var b=new IndexedSymbol("Read",SymbolKind.Function,"b.h",2,1,"B",signature:"Read(double)");
  Assert.Empty(QuickInfoSymbolResolver.Resolve(new[]{a,b},"Read","main.cpp",8,null));
 }
 [Fact]public void EnclosingOwnerSelectsMatchingHeaderDeclaration(){
  var a=new IndexedSymbol("Read",SymbolKind.Function,"a.h",2,1,"A",signature:"Read()");
  var b=new IndexedSymbol("Read",SymbolKind.Function,"b.h",2,1,"B",signature:"Read()");
  Assert.Same(b,Assert.Single(QuickInfoSymbolResolver.Resolve(new[]{a,b},"Read","main.cpp",8,"B")));
 }
}
