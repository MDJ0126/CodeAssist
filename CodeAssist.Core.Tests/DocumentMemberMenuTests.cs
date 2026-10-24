using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests;
public class DocumentMemberMenuTests
{
    private const string Fixture="class ParityActor109\r\n{\r\npublic:\r\n\tint Health = 100;\r\n\tint GetHealth() const { return Health; }\r\n\tvoid Tick(float DeltaTime) { Health -= static_cast<int>(DeltaTime); }\r\n};\r\n\r\nint main()\r\n{\r\n\tParityActor109 Actor;\r\n\treturn Actor.GetHealth();\r\n}\r\n";
    [Fact]public void IncludesObservedClassAndFunctionsInNativeOrder()
    {
        var rows=DocumentMemberMenu.Build("Bootstrap.cpp",Fixture,true);
        Assert.Equal(new[]{"main()","ParityActor109","ParityActor109::GetHealth() const","ParityActor109::Tick(float DeltaTime)"},rows.Select(r=>r.Caption));
        var method=Assert.Single(rows.Where(r=>r.Symbol.Name=="GetHealth"));
        Assert.Equal(Fixture.IndexOf("GetHealth",StringComparison.Ordinal)+"GetHealth".Length,method.NavigationOffset);
        var type=Assert.Single(rows.Where(r=>r.Caption=="ParityActor109"));
        Assert.Equal(Fixture.IndexOf("ParityActor109",StringComparison.Ordinal)+"ParityActor109".Length,type.NavigationOffset);
    }
    [Fact]public void KeepsOverloadsAndOwnersRatherThanGroupingOnlyByName()
    {
        var code="class One {\npublic:\n int Run(int value);\n double Run(double value);\n};\nclass Two {\npublic:\n int Run(int value);\n};\n";
        var rows=DocumentMemberMenu.Build("overloads.h",code,false);
        Assert.Equal(3,rows.Count(r=>r.Symbol.Name=="Run"));
        Assert.Contains(rows,r=>r.Caption=="One::Run(int value)");Assert.Contains(rows,r=>r.Caption=="One::Run(double value)");Assert.Contains(rows,r=>r.Caption=="Two::Run(int value)");
    }
    [Fact]public void IgnoresCommentAndStringLookalikesAndPreservesCrLfOffsets()
    {
        var code="// class Fake { int Pretend(); };\r\nconst char* text=\"class Ghost { void Nope(); };\";\r\nvoid Real() {}\r\n";
        var rows=DocumentMemberMenu.Build("mask.cpp",code,false);var row=Assert.Single(rows);Assert.Equal("Real()",row.Caption);Assert.Equal(code.IndexOf("Real",StringComparison.Ordinal),row.NameStart);
    }
}
