using System;
using System.Collections.Generic;
using CodeAssist.Core;
using CodeAssist.Indexing;
using Xunit;
public class QuickInfoLiveDeclarationsTests
{
 [Fact]public void OpenHeaderIsAvailableWithoutPersistedProjectEntry(){
  var symbol=new IndexedSymbol("ReadHidden",SymbolKind.Function,"open.h",2,1,valueType:"int",signature:"ReadHidden()");
  var live=new Dictionary<string,IReadOnlyList<IndexedSymbol>>{{"open.h",new[]{symbol}}};
  Assert.Same(symbol,Assert.Single(QuickInfoSymbolResolver.MergeLiveDeclarations(Array.Empty<IndexedSymbol>(),live,"ReadHidden")));
 }
 [Fact]public void EmptyEditedHeaderRemovesPersistedDeclaration(){
  var old=new IndexedSymbol("Read",SymbolKind.Function,"OPEN.h",2,1);
  var live=new Dictionary<string,IReadOnlyList<IndexedSymbol>>{{"open.h",Array.Empty<IndexedSymbol>()}};
  Assert.Empty(QuickInfoSymbolResolver.MergeLiveDeclarations(new[]{old},live,"Read"));
 }
 [Fact]public void LiveOverloadsAreRetainedRatherThanCollapsedByName(){
  var a=new IndexedSymbol("Read",SymbolKind.Function,"open.h",2,1,signature:"Read(int)");
  var b=new IndexedSymbol("Read",SymbolKind.Function,"open.h",3,1,signature:"Read(double)");
  var live=new Dictionary<string,IReadOnlyList<IndexedSymbol>>{{"open.h",new[]{a,b}}};
  Assert.Equal(2,QuickInfoSymbolResolver.MergeLiveDeclarations(Array.Empty<IndexedSymbol>(),live,"Read").Length);
 }
 [Fact]public void ForeignLocalAndDifferentCaseAreExcluded(){
  var local=new IndexedSymbol("Read",SymbolKind.Variable,"open.cpp",2,1,isLocal:true);
  var other=new IndexedSymbol("read",SymbolKind.Function,"open.h",3,1);
  var live=new Dictionary<string,IReadOnlyList<IndexedSymbol>>{{"open.cpp",new[]{local}},{"open.h",new[]{other}}};
  Assert.Empty(QuickInfoSymbolResolver.MergeLiveDeclarations(Array.Empty<IndexedSymbol>(),live,"Read"));
 }
}
