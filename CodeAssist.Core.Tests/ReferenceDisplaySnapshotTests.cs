using CodeAssist.Core;
using Xunit;
public class ReferenceDisplaySnapshotTests
{
    private const string Source="class Actor\r\n{\r\n int GetHealth() { return 10; }\r\n};\r\nint main() { Actor a; return a.GetHealth(); }\r\n";
    [Fact]
    public void ReadsEachDocumentOnceAndPreservesDistinctSameLineColumns()
    {
        var reads=0;var input=new[]{new ReferenceLocation("a.cpp",3,6),new ReferenceLocation("A.cpp",5,30),new ReferenceLocation("a.cpp",5,31)};
        var snapshot=ReferenceDisplaySnapshot.Create(input,_=>{reads++;return Source;});
        Assert.Equal(1,reads);Assert.Equal(1,snapshot.DocumentCount);Assert.Equal(3,snapshot.Rows.Count);
        Assert.StartsWith("Actor (3):",snapshot.Rows[0].Caption);Assert.StartsWith("main (5):",snapshot.Rows[1].Caption);
        Assert.NotEqual(snapshot.Rows[1].Column,snapshot.Rows[2].Column);
    }
    [Fact]
    public void ExistingSnapshotIsUnaffectedByDocumentOrInputChangesUntilRebuilt()
    {
        var source=Source;var locations=new List<ReferenceLocation>{new("a.cpp",5,30)};var reads=0;
        string Read(string _) {reads++;return source;}
        var old=ReferenceDisplaySnapshot.Create(locations,Read);source=Source.Replace("GetHealth","GetEnergy");locations.Clear();
        var query=ReferenceFilter.Compile("main GetHealth");for(var n=0;n<50;n++)Assert.True(query.Matches(old.Rows[0].Caption));
        Assert.Equal(1,reads);var newer=ReferenceDisplaySnapshot.Create(new[]{new ReferenceLocation("a.cpp",5,30)},Read);
        Assert.False(query.Matches(newer.Rows[0].Caption));Assert.Equal(2,reads);
    }
    [Fact]
    public void InvalidLocationUsesFallbackAndEmptyInputDoesNotReadDocuments()
    {
        var empty=ReferenceDisplaySnapshot.Create(Array.Empty<ReferenceLocation>(),_=>throw new Exception());Assert.Empty(empty.Rows);
        var snapshot=ReferenceDisplaySnapshot.Create(new[]{new ReferenceLocation("missing.cpp",42,1,"symbol")},_=>null!);
        Assert.Equal("(42): symbol",snapshot.Rows[0].Caption);
    }
    [Theory]
    [InlineData("main GetHealth","main (5): return a.GetHealth();",true)]
    [InlineData("main GetHealth","Actor (3): int GetHealth() { return 10; }",false)]
    [InlineData("Actor,main","main (5): return a.GetHealth();",true)]
    [InlineData("  ","anything",true)]
    [InlineData(",,","anything",false)]
    public void CompiledQueryCanBeReused(string query,string text,bool matches)
    {var compiled=ReferenceFilter.Compile(query);Assert.Equal(matches,compiled.Matches(text));Assert.Equal(matches,compiled.Matches(text));}
}
