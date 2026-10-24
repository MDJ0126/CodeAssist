using CodeAssist.Core;
using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests;
public class FeatureBatchTests
{
    [Fact] public void SimpleIssueLinksResolveAllOccurrences()
    {
        var rule=new SourceLinkRule {Name="GitHub",Pattern="issue #{id}",Target="https://github.com/org/repo/issues/{id}"};
        var links=SourceLinks.Find("// issue #12 and issue #300",new[]{rule});Assert.Equal(2,links.Count);Assert.Equal(3,links[0].Start);Assert.Equal("https://github.com/org/repo/issues/300",links[1].Uri.AbsoluteUri);
    }
    [Fact] public void RegexCapturesAreEscapedInTarget()
    {
        var rule=new SourceLinkRule {Pattern=@"BUG-(?<project>[a-z/]+)-(?<id>[0-9]+)",RegularExpression=true,Target="https://example.com/{project}/{id}"};
        Assert.Equal("https://example.com/a%2Fb/9",SourceLinks.Find("BUG-a/b-9",new[]{rule}).Single().Uri.AbsoluteUri);
    }
    [Theory] [InlineData("file:///C:/secret")] [InlineData("javascript:alert(1)")] [InlineData("https://example.com/{missing}")]
    public void InvalidTargetsAreRejected(string target){Assert.Throws<ArgumentException>(()=>SourceLinks.Validate(new SourceLinkRule {Pattern="#{id}",Target=target}));}
    [Fact] public void DisabledAndInvalidRulesDoNotBlockValidRules()
    {
        var disabled=new SourceLinkRule {Pattern="#{id}",Target="https://example.com/{id}",Enabled=false};
        var broken=new SourceLinkRule {Pattern="(",RegularExpression=true};
        Assert.Empty(SourceLinks.Find("#9",new[]{disabled,broken}));
        disabled.Enabled=true;Assert.Single(SourceLinks.Find("#9",new[]{broken,disabled}));
    }
    [Fact] public void RepeatedAndMissingSimplePlaceholdersAreRejected()
    {foreach(var pattern in new[]{"abc","{id}{id}"})Assert.Throws<ArgumentException>(()=>SourceLinks.Validate(new SourceLinkRule {Pattern=pattern,Target="https://example.com"}));}
    [Fact] public void RegexTimeoutIsContained()
    {Assert.Empty(SourceLinks.Find(new string('a',8000)+"!",new[]{new SourceLinkRule {Pattern="^(a+)+$",RegularExpression=true,Target="https://example.com"}}));}
    [Fact] public void RulePersistenceDeepCopiesCollections()
    {var preferences=new StartupPreferences();preferences.SourceLinkRules.Add(new SourceLinkRule {Name="FogBugz",Pattern="case {id}",Target="https://team.fogbugz.com/f/cases/{id}"});var copy=preferences.Copy();copy.SourceLinkRules[0].Name="Changed";Assert.Equal("FogBugz",preferences.SourceLinkRules[0].Name);Assert.Single(SourceLinks.Find("case 20",copy.SourceLinkRules));}
    [Fact] public void BraceMatchingIgnoresCommentsAndStrings()
    {var text="""void F() { /* ] */ const char* s = ")"; }""";var map=new BraceMap(text);Assert.True(map.At(text.IndexOf('{'),out var close));Assert.Equal(text.LastIndexOf('}'),close);Assert.False(map.At(text.IndexOf(']'),out _));}
    [Fact] public void MismatchedNestedBracesAreMarked()
    {var map=new BraceMap("([)]");for(var i=0;i<4;i++){Assert.True(map.At(i,out var other));Assert.Equal(-1,other);}}
    [Fact] public void UnterminatedBraceIsMarked(){Assert.True(new BraceMap("{").At(0,out var partner));Assert.Equal(-1,partner);}
    [Fact] public void EmptyDocumentHasNoBraces(){Assert.False(new BraceMap("").At(0,out _));}
    [Fact] public void PreferenceLimitsConstrainBackgroundWork()
    {var p=new StartupPreferences {ParseConcurrency=999,ColumnIndicator=1};Assert.Equal(16,p.ParseConcurrency);Assert.Equal(20,p.ColumnIndicator);var index=new PersistentSymbolIndex {ParseConcurrency=-1};Assert.Equal(1,index.ParseConcurrency);}
    [Theory] [InlineData("GetWorldLocation","GWL")] [InlineData("get_world_location","gwl")] [InlineData("XMLParser","XP")]
    public void AcronymsMatchWordBoundaries(string name,string query){Assert.Equal(2,IdentifierMatching.Rank(name,query,true,false));}
    [Fact] public void ShorthandIsOptional(){Assert.Equal(3,IdentifierMatching.Rank("GetWorldLocation","GWrLoc",false,true));Assert.Equal(int.MaxValue,IdentifierMatching.Rank("GetWorldLocation","GWrLoc",false,false));}
    [Fact] public void PrefixRanksBeforeAcronym(){Assert.Equal(1,IdentifierMatching.Rank("GetWorldLocation","Get",true,true));Assert.Equal(int.MaxValue,IdentifierMatching.Rank("GetWorldLocation","W",true,true));}
}
