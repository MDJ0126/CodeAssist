using CodeAssist.Core;
using CodeAssist.Indexing;
using Xunit;
public class LiveMemberCompletionTests
{
    private static IReadOnlyDictionary<string,IReadOnlyList<IndexedSymbol>> Live(string source) => new Dictionary<string,IReadOnlyList<IndexedSymbol>>(StringComparer.OrdinalIgnoreCase) {{"open.h",PersistentSymbolIndex.ReadDocumentSymbols("open.h",source)}};
    [Fact] public void NewOpenTypeHasMembersBeforePersistedIndexRebuild()
    {
        var index=new PersistentSymbolIndex();
        var live=Live("struct NewType {\n int ReadField;\n void ReadMethod(int count);\n};\n");
        var actual=index.CompleteMembers("NewType","Read",100,true,true,live);
        Assert.Equal(new[]{"ReadField","ReadMethod"},actual.Select(item=>item.Name));
        Assert.Contains(actual,item=>item.Name=="ReadMethod"&&item.ValueType=="void");
    }
    [Fact] public void ChangedOpenFileRemovesPersistedMembersIncludingEmptyReplacement()
    {
        var folder=Path.Combine(Path.GetTempPath(),"CA-LiveMembers-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var file=Path.Combine(folder,"open.h");File.WriteAllText(file,"struct OldType {\n int Removed;\n};");
            var index=new PersistentSymbolIndex();index.BuildProject(folder);
            Assert.Contains(index.CompleteMembers("OldType",""),item=>item.Name=="Removed");
            var live=new Dictionary<string,IReadOnlyList<IndexedSymbol>>(StringComparer.OrdinalIgnoreCase){{file,Array.Empty<IndexedSymbol>()}};
            Assert.Empty(index.CompleteMembers("OldType","",100,true,true,live));
        }
        finally {Directory.Delete(folder,true);}
    }
    [Fact] public void LiveInheritanceAndOverloadsArePreservedWithoutForeignLocals()
    {
        var live=Live("struct Parent {\n int BaseMember;\n};\nstruct Child : public Parent {\n void Read(int x);\n void Read(double x);\n void Local() { int Hidden; }\n};\nstruct Foreign { int Other; };\n");
        var actual=new PersistentSymbolIndex().CompleteMembers("Child","",100,true,true,live);
        Assert.Contains(actual,item=>item.Name=="BaseMember");Assert.Equal(2,actual.Count(item=>item.Name=="Read"));
        Assert.DoesNotContain(actual,item=>item.Name=="Hidden"||item.Name=="Other");
    }
    [Theory] [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public void LiveMembersHonorMatchingSettings(bool acronyms,bool shorthand)
    {
        var index=new PersistentSymbolIndex();var live=Live("struct Match {\n void GetWorldLocation();\n};\n");
        Assert.Equal(acronyms||shorthand,index.CompleteMembers("Match","GWL",100,acronyms,shorthand,live).Any());
        Assert.Equal(shorthand,index.CompleteMembers("Match","Location",100,acronyms,shorthand,live).Any());
    }
    [Theory]
    [InlineData("")]
    [InlineData("public ")]
    [InlineData("protected ")]
    [InlineData("private ")]
    public void InheritanceWithOptionalAccessKeepsParentAndLocalMembers(string access)
    {
        var source="struct Base170 { int ABaseField; void BBaseMethod(); };\nstruct Derived170 : "+access+"Base170 { int ZLocalField; void YLocalMethod(); };\n";
        var actual=new PersistentSymbolIndex().CompleteMembers("Derived170","",100,true,true,Live(source));
        Assert.Equal(new[]{"ABaseField","BBaseMethod","YLocalMethod","ZLocalField"},actual.Select(item=>item.Name));
        Assert.Equal(new[]{"Base170","Base170","Derived170","Derived170"},actual.Select(item=>item.OwnerType));
    }
}
