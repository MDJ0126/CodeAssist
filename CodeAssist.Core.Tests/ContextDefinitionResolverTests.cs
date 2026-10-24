using System.Linq;
using CodeAssist.Core;
using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests
{
 public class ContextDefinitionResolverTests
 {
  [Theory]
  [InlineData("struct AuditWidget {};", "auditwidget", "AuditWidget")]
  [InlineData("struct Name {}; struct NAME {};", "name", null)]
  [InlineData("// struct Ghost {};", "ghost", null)]
  public void CaseRepairUsesUniqueDeclaredType(string text,string word,string expected)
  {Assert.Equal(expected,CppAutomaticEdits.FindDeclaredTypeCasing(text,word));}
  [Theory]
  [InlineData("struct Audit { int Health; void Update(); };", "Update()")]
  [InlineData("struct Audit { void Update() const noexcept; int Other; };", "Update() const noexcept")]
  [InlineData("void Update(int (*callback)(int)) { callback(1); }", "Update(int (*callback)(int))")]
  public void NavigationSignatureStopsAtDeclarationBoundary(string text,string expected)
  {
   var found=PersistentSymbolIndex.ReadDocumentSymbols("a.h",text);
   var function=Assert.Single(found.Where(x=>x.Kind==SymbolKind.Function&&x.Name=="Update"));
   Assert.Equal(expected,function.Signature);
  }
  [Fact] public void CurrentDeclarationWinsAgainstForeignOverload()
  {
   var a=new IndexedSymbol("Select",SymbolKind.Function,"actor.cpp",14,20,"Actor","int",signature:"Select(int Value)");
   var b=new IndexedSymbol("Select",SymbolKind.Function,"engine.h",2,4,"Engine","char*",signature:"Select(char* Value)");
   Assert.Same(a,ContextDefinitionResolver.Resolve(new[]{b,a},"Select","actor.cpp",14,""));
  }
  [Fact] public void AmbiguousOverloadsAreNotGuessed()
  {
   var a=new IndexedSymbol("Select",SymbolKind.Function,"actor.h",2,4,"Actor",signature:"Select(int)");
   var b=new IndexedSymbol("Select",SymbolKind.Function,"actor.h",3,4,"Actor",signature:"Select(double)");
   Assert.Null(ContextDefinitionResolver.Resolve(new[]{a,b},"Select","main.cpp",10,"Actor"));
  }
  [Fact] public void InlineFieldAndMacroSurviveFunctionOnSameLine()
  {
   var found=PersistentSymbolIndex.ReadDocumentSymbols("a.h","#define CUSTOM(...)\nstruct Ptr { T* Value = nullptr; T* operator->() const { return Value; } };");
   Assert.Contains(found,s=>s.Name=="CUSTOM" && s.Kind==SymbolKind.Macro);
   Assert.Contains(found,s=>s.Name=="Value" && s.OwnerType=="Ptr" && !s.IsLocal);
  }
  [Fact] public void InlineArrowOperatorKeepsOwnerAndSignature()
  {
   var found=PersistentSymbolIndex.ReadDocumentSymbols("a.h","template<typename T> struct TObjectPtr { T* operator->() const { return Value; } };");
   var op=Assert.Single(found.Where(s=>s.Kind==SymbolKind.Function));
   Assert.Equal("operator->",op.Name);Assert.Equal("TObjectPtr",op.OwnerType);Assert.Equal("operator->() const",op.Signature);
  }
 }
}
