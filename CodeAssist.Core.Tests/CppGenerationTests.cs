using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class CppGenerationTests
{
    [Fact] public void DocumentsNamedParametersAndReturn()
    {var text=CppGeneration.DocumentMethod("int Actor::Get(int count, const Thing& item) const;");Assert.Contains("@param count",text);Assert.Contains("@param item",text);Assert.Contains("@return",text);}
    [Theory] [InlineData("Widget();")] [InlineData("void Set(int);")]
    public void ConstructorsAndVoidDoNotEmitReturn(string declaration)
    {Assert.DoesNotContain("@return",CppGeneration.DocumentMethod(declaration));}
    [Fact] public void DoesNotInventUnnamedParameters()
    {var text=CppGeneration.DocumentMethod("void F(int, std::string, void (*callback)(int));");Assert.Contains("@param callback",text);Assert.DoesNotContain("@param int",text);Assert.DoesNotContain("@param string",text);}
    [Fact] public void SupportsTemplateCommaAndLiteralComma()
    {var text=CppGeneration.DocumentMethod("void F(std::pair<int,int> point, const char* label = \"a,b\");");Assert.Contains("@param point",text);Assert.Contains("@param label",text);}
    [Theory] [InlineData("if (x)")] [InlineData("not a method")]
    public void RejectsNonDeclarations(string input){Assert.Null(CppGeneration.DocumentMethod(input));}
    [Fact] public void IncludePreservesGuardAndCrLf()
    {var text="#ifndef HEADER_H\r\n#define HEADER_H\r\nclass A {};\r\n#endif\r\n";var change=CppGeneration.Include(text,"vector",true)!;var changed=text.Insert(change.Start,change.Text);Assert.StartsWith("#ifndef HEADER_H\r\n#define HEADER_H\r\n#include <vector>\r\n",changed);}
    [Fact] public void IncludeDoesNotDuplicate()
    {Assert.Null(CppGeneration.Include("#include \"Actor.h\"\n","Actor.h",false));}
    [Theory] [InlineData("bad\nfile")] [InlineData("bad\"file")]
    public void IncludeRejectsInjectedDirectives(string name){Assert.Throws<ArgumentException>(()=>CppGeneration.Include("",name,false));}
    [Fact] public void NamespaceForwardDeclarationIsExplicit()
    {Assert.Equal("namespace Game { namespace UI { struct Widget; } }",CppGeneration.ForwardDeclaration("Game::UI::Widget",true));}
    [Fact] public void SelectedSnippetPreservesLiteralDollars()
    {var serialized=CppGeneration.SnippetFromSelection("Literal","lit","return \"$value$\";");var template=SnippetTemplate.Parse(serialized).Single();Assert.Equal("return \"$value$\";",template.Expand(new Dictionary<string,string>()).Text);}
    [Theory] [InlineData("https://example.com/issues/4",true)] [InlineData("file:///C:/secret",false)] [InlineData("javascript:alert(1)",false)]
    public void HyperlinksOnlyOpenWebSchemes(string text,bool expected)
    {Assert.Equal(expected,CppGeneration.HyperlinkAt(text,4)!=null);}
}
