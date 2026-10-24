using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests;
public class ShorthandCandidateTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void NonPrefixNonAcronymQueryRequiresShorthand(bool acronyms, bool shorthand, bool expected)
    {
        var folder = Path.Combine(Path.GetTempPath(), "CodeAssist-Shorthand-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Shorthand.cpp"), "void GetWorldLocation() {}\r\nvoid GetWorldRotation() {}\r\n");
            var index = new PersistentSymbolIndex();
            index.BuildProject(folder);
            var names = index.Complete("GWrLoc", 100, acronyms, shorthand).Select(item => item.Name).ToArray();
            Assert.Equal(expected, names.Contains("GetWorldLocation"));
            Assert.DoesNotContain("GetWorldRotation", names);
            Assert.Contains(index.Complete("GetWorld", 100, acronyms, shorthand), item => item.Name == "GetWorldLocation");
        }
        finally { Directory.Delete(folder, true); }
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void MemberCompletionHonorsIndependentMatchingOptions(bool acronyms, bool shorthand)
    {
        var folder = Path.Combine(Path.GetTempPath(), "CodeAssist-MemberMatching-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Members.cpp"),
                "class Parent { public: void GetWorldLocation() {} };\r\nclass Child : public Parent { public: void GetWorldRotation() {} };\r\nclass Other { public: void GetWorldLocal() {} };\r\n");
            var index = new PersistentSymbolIndex();
            index.BuildProject(folder);
            var shorthandNames = index.CompleteMembers("Child", "GWrLoc", 100, acronyms, shorthand).Select(item => item.Name).ToArray();
            Assert.Equal(shorthand, shorthandNames.Contains("GetWorldLocation"));
            Assert.DoesNotContain("GetWorldLocal", shorthandNames);
            var acronymNames = index.CompleteMembers("Child", "GWL", 100, acronyms, shorthand).Select(item => item.Name).ToArray();
            Assert.Equal(acronyms || shorthand, acronymNames.Contains("GetWorldLocation"));
            Assert.Contains(index.CompleteMembers("Child", "GetWorld", 100, acronyms, shorthand), item => item.Name == "GetWorldLocation");
            Assert.Equal(shorthand, index.CompleteMembers("Child", "Location", 100, acronyms, shorthand).Any(item => item.Name == "GetWorldLocation"));
        }
        finally { Directory.Delete(folder, true); }
    }
}
