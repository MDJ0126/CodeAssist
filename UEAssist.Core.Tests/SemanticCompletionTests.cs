using Newtonsoft.Json.Linq;
using UEAssist.Semantics;
namespace UEAssist.Core.Tests;
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
}
