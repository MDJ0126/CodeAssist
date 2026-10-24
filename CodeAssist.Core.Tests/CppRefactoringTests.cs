using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class CppRefactoringTests
{
 [Fact] public void SimplifiesMatchingConstructor(){Assert.Equal("Widget value(2);",CppRefactoring.SimplifyInstance("Widget value = Widget(2);"));}
 [Theory][InlineData("Widget value = Other(2);")][InlineData("auto value = Widget(2);")][InlineData("Widget value = Widget();")]
 public void DoesNotGuessConstructorTypes(string input){Assert.Null(CppRefactoring.SimplifyInstance(input));}
 [Fact] public void CreatesDeclarationFromQualifiedDefinition(){Assert.Equal("int Value(int index) const;",CppRefactoring.CreateDeclaration("int Widget::Value(int index) const { return index; }"));}
 [Fact] public void CreatesOutOfClassImplementationWithoutDefaultArguments(){var result=CppRefactoring.CreateImplementation("virtual int Value(int index = 2) const override;","Widget")!;Assert.StartsWith("int Widget::Value(int index) const\n",result);Assert.Contains("throw \"Not implemented\";",result);Assert.DoesNotContain("override",result);}
 [Fact] public void ParsesNestedTemplateAndCallbackArguments(){Assert.Equal(2,CppRefactoring.SplitArguments("std::pair<int, int> value, void (*callback)(int, int)").Length);}
 [Fact] public void CreatesSeveralMethodsAndSkipsFields(){var result=CppRefactoring.CreateImplementations("int Value() const; int field; void Update(int n);","Widget")!;Assert.Contains("Widget::Value",result);Assert.Contains("Widget::Update",result);Assert.DoesNotContain("field",result);}
 [Fact] public void GeneratesInlineOverrides(){var result=CppRefactoring.CreateImplementations("virtual void Update() = 0;","",overrides:true)!;Assert.StartsWith("void Update() override",result);}
 [Fact] public void GeneratesNamedAccessors(){var result=CppRefactoring.EncapsulateField("int health;","Health")!;Assert.Contains("private:",result);Assert.Contains("GetHealth() const",result);Assert.Contains("SetHealth(int value)",result);}
 [Fact] public void ConstFieldDoesNotGenerateAssignment(){Assert.DoesNotContain("Set",CppRefactoring.EncapsulateField("const int health;","Health")!);}
 [Fact] public void SimilarMemberChangesOnlyMemberName(){Assert.Equal("int Max(int count) const;",CppRefactoring.SimilarMember("int Min(int count) const;","Max"));}
 [Theory][InlineData("return Run();")][InlineData("Execute();")]
 public void CanAddAndRemoveSimpleStatementBraces(string statement){Assert.Equal(statement,CppRefactoring.ToggleBraces(CppRefactoring.ToggleBraces(statement)));}
 [Theory][InlineData("{ int value = 2; }")][InlineData("{ if (condition) Run(); }")][InlineData("{ First(); Second(); }")]
 public void NeverRemovesScopeOrDanglingElseProtection(string statement){Assert.Null(CppRefactoring.ToggleBraces(statement));}
 [Fact] public void AddsOnlyMissingEnumCases(){var result=CppRefactoring.MissingCases("switch (s) { case State::Ready: break; }","enum class State { Ready, Running, Done };","State")!;Assert.Equal(1,result.Split("State::Ready").Length-1);Assert.Contains("case State::Running:",result);Assert.Contains("case State::Done:",result);}
 [Theory][InlineData("foo; delete x")][InlineData("std::")]
 public void RejectsInjectedNamespace(string value){Assert.Null(CppRefactoring.UsingNamespace(value));}
 [Fact] public void PointerDotConversionRequiresPointerType(){Assert.Equal("->",CppAutomaticEdits.DotToArrow("actor",5,"Actor*")!.Text);Assert.Null(CppAutomaticEdits.DotToArrow("actor",5,"Actor"));Assert.Null(CppAutomaticEdits.DotToArrow("1",1,"Actor*"));}
 [Fact] public void AutomaticEditsIgnoreCommentsAndStrings(){Assert.Null(CppAutomaticEdits.DotToArrow("// actor",8,"Actor*"));Assert.Equal("",CppAutomaticEdits.WordBefore("\"actor",6));}
 [Fact] public void CaseRepairIsOnlyCaseChange(){Assert.Equal("Widget",CppAutomaticEdits.RepairCase("widget",6,"Widget")!.Text);Assert.Null(CppAutomaticEdits.RepairCase("actor",5,"Widget"));}
}
