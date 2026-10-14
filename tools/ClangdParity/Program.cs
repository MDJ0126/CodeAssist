using Newtonsoft.Json.Linq;
using UEAssist.Semantics;

if (args.Length != 3) throw new ArgumentException("clangd executable, fixture directory, output json");
var root = Path.GetFullPath(args[1]);
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(45));
using var client = await ClangdClient.StartAsync(args[0], root, cancellation.Token);
foreach (var name in new[] { "FixtureActor.h", "FixtureActor.cpp", "main.cpp" }) await client.SyncAsync(Path.Combine(root, name), File.ReadAllText(Path.Combine(root, name)), cancellation.Token);
var file = Path.Combine(root, "main.cpp"); var text = File.ReadAllText(file);
var lines = text.Split('\n');
var outputs = new JObject();
foreach (var query in new[] { ("GetHealth", 5), ("Select", 7), ("Select", 8) })
{
    var result = await client.AtAsync("textDocument/definition", file, query.Item2, lines[query.Item2].IndexOf(query.Item1) + 1, cancellation.Token);
    outputs[query.Item1 + "@" + (query.Item2 + 1)] = result;
    if (result is not JArray array || array.Count == 0) throw new Exception("No compiler definition: " + query);
}
var main = await client.AtAsync("textDocument/references", file, 5, lines[5].IndexOf("GetHealth") + 1, cancellation.Token,
    new JObject { ["context"] = new JObject { ["includeDeclaration"] = true } });
outputs["GetHealthReferences"] = main;
var completion = await client.AtAsync("textDocument/completion", file, 5, lines[5].IndexOf("Actor.GetHealth") + "Actor.".Length,
    cancellation.Token, new JObject { ["context"] = new JObject { ["triggerKind"] = 1 } });
outputs["completion"] = completion;
var items = completion is JArray itemArray ? itemArray : (JArray)completion["items"];
if (items == null || !items.Any(item => ((string)item["label"]).Contains("GetHealth"))) throw new Exception("Inherited member completion missing");
var rename = await client.AtAsync("textDocument/rename", file, 5, lines[5].IndexOf("GetHealth") + 1, cancellation.Token,
    new JObject { ["newName"] = "GetVitality" });
outputs["rename"] = rename;
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
File.WriteAllText(args[2] + ".partial.json", outputs.ToString());
var editFiles = WorkspaceEdits.Files(rename);
if (editFiles.Count < 3) throw new Exception("Rename did not include declaration, definition, and usage files");
foreach (var pair in editFiles)
{
    var changed = WorkspaceEdits.Apply(File.ReadAllText(pair.Key), pair.Value);
    if (!changed.Contains("GetVitality")) throw new Exception("Rename plan failed");
    if (Path.GetFileName(pair.Key) == "main.cpp" && (!changed.Contains("// GetHealth") || !changed.Contains("\"GetHealth\"")))
        throw new Exception("Rename changed comment or literal");
}
if (main is not JArray references || references.Any(item => (string)item["uri"] == new Uri(file).AbsoluteUri && (int)item["range"]["start"]["line"] >= 9))
    throw new Exception("Comments or literals returned as semantic references");
var intTarget = outputs["Select@8"][0].ToString(); var doubleTarget = outputs["Select@9"][0].ToString();
if (intTarget == doubleTarget) throw new Exception("Overloads resolved to same definition");
outputs["scope"] = "Real clangd 19 compiler definition/reference requests on standalone C++ fixture; not VA parity or Unreal configuration validation.";
outputs["clangd"] = args[0]; outputs["lastError"] = client.LastError;
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!); File.WriteAllText(args[2], outputs.ToString());
Console.WriteLine("Compiler verification passed: distinct overloads, semantic references, three-file rename preserves comment/literal.");
