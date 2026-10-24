using System;
using System.Linq;
using CodeAssist.Core;
using Xunit;
public class MissingEditorOptionsTests {
 [Theory][InlineData(true,true)][InlineData(true,false)][InlineData(false,true)][InlineData(false,false)]
 public void ArrowConversionRespectsOverloadToggle(bool overloaded,bool enabled) {
  var change=CppAutomaticEdits.DotToArrow("smart",5,"Smart",overloaded,enabled);
  Assert.Equal(overloaded&&enabled,change!=null);
  Assert.NotNull(CppAutomaticEdits.DotToArrow("raw",3,"Actor *",overloaded,enabled));
 }
 [Fact]public void ArrowOperatorMustBelongToResolvedTypeAndIgnoreComments(){
  var source="struct Smart { Actor* operator->() const; }; struct Plain {}; // operator->()";
  Assert.True(CppAutomaticEdits.HasArrowOperator(source,"Smart"));
  Assert.False(CppAutomaticEdits.HasArrowOperator(source,"Plain"));
  Assert.False(CppAutomaticEdits.HasArrowOperator(source,"Unknown"));
 }
 [Fact]public void NewPreferencesSurviveCopyAndOldDefaultsRemainUsable(){
  var p=new StartupPreferences {SurroundHash=false,SurroundWithSnippets=false,DoubleKeyRepeat=true,ConvertOverloadedArrow=false,ColorTextEditors=false,ColorListboxes=false,ColorTooltips=false,ColorViews=false,ColorNavigationBar=false,ColorObjectBrowser=false,NamespaceColor="#123456",EnumMemberColor="#654321"};
  var copy=p.Copy();Assert.False(copy.SurroundHash);Assert.False(copy.SurroundWithSnippets);Assert.True(copy.DoubleKeyRepeat);Assert.False(copy.ConvertOverloadedArrow);Assert.False(copy.ColorTextEditors);Assert.False(copy.ColorListboxes);Assert.False(copy.ColorTooltips);Assert.False(copy.ColorViews);Assert.False(copy.ColorNavigationBar);Assert.False(copy.ColorObjectBrowser);Assert.Equal("#123456",copy.NamespaceColor);Assert.Equal("#654321",copy.EnumMemberColor);
 }
 [Fact]public void NamespaceAndEnumClassificationExcludeCommentsAndStrings(){
  var code="namespace World { enum class State { Idle, Running = 2 }; } World::State s = World::State::Idle; // Idle\nconst char* t=\"Running World\";";
  var tokens=CppSemanticParser.Parse(code);Assert.Equal(2,tokens.Count(t=>t.Name=="Idle"&&t.Kind==SemanticTokenKind.EnumMember));Assert.Equal(3,tokens.Count(t=>t.Name=="World"&&t.Kind==SemanticTokenKind.Namespace));Assert.Single(tokens.Where(t=>t.Name=="Running"&&t.Kind==SemanticTokenKind.EnumMember));
 }
 [Fact]public void NestedNamespacesClassifyEachComponent(){var tokens=CppColorCategories.Parse("namespace A::B { enum E { X }; } A::B::X;");Assert.Equal(2,tokens.Count(t=>t.Name=="A"));Assert.Equal(2,tokens.Count(t=>t.Name=="B"));Assert.Equal(2,tokens.Count(t=>t.Kind==SemanticTokenKind.EnumMember));}
 [Fact]public void DirectiveMatchesRecordedVaOutputAndSelectsLinkedCondition(){
  var text="int value = 1;\r\n";var change=CppEditing.SurroundDirective(text,0,text.Length);
  Assert.Equal("#ifdef _DEBUG\r\n"+text+"#endif // _DEBUG\r\n",change.Text);Assert.Equal(7,change.SelectionStart);Assert.Equal(6,change.SelectionLength);
 }
 [Theory][InlineData("#B88020","#795000")][InlineData("#B9D7A2","#355D18")][InlineData("#569CD6","#0000FF")]
 public void AddedDefaultColorsRemainReadableInLightTheme(string dark,string light){Assert.Equal(light,DefaultSemanticPalette.Resolve(dark,dark,true));Assert.Equal(dark,DefaultSemanticPalette.Resolve(dark,dark,false));}
}
