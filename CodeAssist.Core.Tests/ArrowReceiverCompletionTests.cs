using CodeAssist.Indexing;
using Xunit;

public sealed class ArrowReceiverCompletionTests
{
    [Fact]
    public void OverloadedArrowCompletesTargetMembersInsteadOfWrapperMembers()
    {
        WithIndex(@"struct Target { int Value; };
struct Smart { Target* operator->(); };
struct Chain { Smart operator->(); };
struct Cycle { Cycle operator->(); };
struct Raw { Smart* operator->(); };", index =>
        {
            Assert.Equal("Target", index.ResolveArrowTargetType("Smart"));
            Assert.Equal("Target", index.ResolveArrowTargetType("Chain"));
            Assert.Contains(index.CompleteMembers(index.ResolveArrowTargetType("Smart"), ""), s => s.Name == "Value");
            Assert.Equal("Smart", index.ResolveArrowTargetType("Smart*"));
            Assert.Equal("Smart", index.ResolveArrowTargetType("Raw"));
            Assert.Null(index.ResolveArrowTargetType("Cycle"));
            Assert.Null(index.ResolveArrowTargetType(null));
        });
    }

    [Theory]
    [InlineData(".")]
    [InlineData("->")]
    public void CompletionResolvesLocalAtReceiverEndBeforeMemberOperator(string memberOperator)
    {
        var source = "struct Target { int Value; };\nstruct Smart { Target* operator->(); };\nvoid Probe() {\n Smart pointer;\n pointer" + memberOperator + "\n}";
        var receiverEnd = source.LastIndexOf("pointer", StringComparison.Ordinal) + "pointer".Length;
        var context = new CodeAssist.Core.CppReferenceContext(source);
        Assert.Equal("Smart", context.ResolveVariableType("pointer", receiverEnd));
        Assert.Null(context.ResolveVariableType("pointer", receiverEnd + memberOperator.Length));
        WithIndex(source, index => Assert.Contains(
            index.CompleteMembers(index.ResolveArrowTargetType(context.ResolveVariableType("pointer", receiverEnd)), ""),
            symbol => symbol.Name == "Value"));
    }

    private static void WithIndex(string text, Action<PersistentSymbolIndex> check)
    {
        var root = Path.Combine(Path.GetTempPath(), "CodeAssistArrow", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "Probe.cpp"), text);
            var index = new PersistentSymbolIndex(); index.BuildProject(root); check(index);
        }
        finally { Directory.Delete(root, true); }
    }
}
