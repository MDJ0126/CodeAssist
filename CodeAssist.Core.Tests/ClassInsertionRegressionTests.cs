using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class ClassInsertionRegressionTests
{
 [Fact] public void ClassNameCaretInsertsPublicMemberInClass(){var text="class Audit {\nprivate:\n int existing;\n};\n";var e=CppClassTransforms.InsertMember(text,8,"int addedField;");var after=text.Insert(e.Start,e.Text);Assert.True(after.IndexOf("int addedField;")<after.IndexOf("};"));Assert.Contains("public:\n    int addedField;",after);Assert.StartsWith("class Audit {",after);}
 [Fact] public void QualifiedDefinitionDeclarationReturnsToOwningClass(){var text="class Audit { };\nint Audit::Value() { return 3; }\n";var definition="int Audit::Value() { return 3; }";var e=CppClassTransforms.InsertMember(text,text.IndexOf("Value"),CppRefactoring.CreateDeclaration(definition),CppClassTransforms.MethodOwner(definition));var after=text.Insert(e.Start,e.Text);Assert.True(after.IndexOf("int Value();")<after.IndexOf("};"));}
 [Fact] public void NestedClassCaretDoesNotChooseOuterClass(){var text="class Outer { class Inner { }; };";var e=CppClassTransforms.InsertMember(text,text.IndexOf("Inner")+2,"int value;");Assert.True(e.Start<=text.IndexOf("};"));}
 [Fact] public void DuplicateOwnersAreRejected(){Assert.Throws<NotSupportedException>(()=>CppClassTransforms.InsertMember("namespace A { class Same {}; } namespace B { class Same {}; }",0,"int value;","Same"));}
 [Fact] public void MissingClassNeverInsertsIntoGlobalScope(){Assert.Throws<NotSupportedException>(()=>CppClassTransforms.InsertMember("int main() {}",0,"int value;"));}
 [Fact] public void InlineHeaderDefinitionsDoNotModifyCallsInsideBodies(){var s="void A::First()\n{\nCall();\n}\nvoid A::Second()\n{\n}\n";var r=CppClassTransforms.HeaderSafeImplementation(s);Assert.StartsWith("inline void A::First()",r);Assert.Contains("inline void A::Second()",r);Assert.Contains("\nCall();",r);Assert.DoesNotContain("inline Call",r);Assert.Equal(r,CppClassTransforms.HeaderSafeImplementation(r));}
 [Fact] public void SetterQualifiesFieldWhenParameterHasSameName(){Assert.Contains("this->value = value;",CppRefactoring.EncapsulateField("int value;","Value"));}
}
