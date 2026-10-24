using Newtonsoft.Json.Linq;
using CodeAssist.Semantics;
namespace CodeAssist.Core.Tests;
public class SemanticCompletionTests
{
    private static JObject Item(int start, int end) => new() { ["label"] = " GetHealth() const", ["kind"] = 2,
        ["textEdit"] = new JObject { ["newText"] = "GetHealth", ["range"] = new JObject {
            ["start"] = new JObject { ["line"] = 0, ["character"] = start }, ["end"] = new JObject { ["line"] = 0, ["character"] = end } } } };
    [Fact]
    public void OnlyAcceptsExactPlainReplacementWithoutOtherEdits()
    {
        var safe = Item(6, 9); var wrong = Item(5, 9); var snippet = Item(6, 9); snippet["insertTextFormat"] = 2;
        var includes = Item(6, 9); includes["additionalTextEdits"] = new JArray(new JObject());
        var results = SemanticCompletions.Read(new JObject { ["items"] = new JArray(safe, wrong, snippet, includes, Item(6, 999)) }, "Actor.Get", 6, 9);
        Assert.Single(results); Assert.Equal("GetHealth", results[0].Name); Assert.Contains("const", results[0].Detail);
    }
    [Fact]
    public void MemberContextRejectsCompilerGlobalFallbacksButKeepsReceiverMembers()
    {
        var kinds=new[]{1,2,3,5,6,7,8,10,13,14,15,17,20,21,22};
        var items=new JArray(kinds.Select(kind=>{var item=Item(6,9);item["kind"]=kind;return item;}));
        var actual=SemanticCompletions.Read(items,"Actor.Get",6,9,true);
        Assert.Equal(new[]{2,3,5,7,8,10,13,20,21,22},actual.Select(item=>item.Kind));
        Assert.Equal(kinds.Length,SemanticCompletions.Read(items,"Actor.Get",6,9).Length);
    }
}
