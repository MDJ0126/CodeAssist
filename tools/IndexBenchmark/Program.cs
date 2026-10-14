using System.Text;
using System.Text.Json;
using CodeAssist.Indexing;

if (args.Length != 2) throw new ArgumentException("Usage: IndexBenchmark <worker exe> <report json>");
var root = Path.Combine(Path.GetTempPath(), "CodeAssistBenchmark-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    for (var file=0; file < 120; file++)
    {
        var source = new StringBuilder();
        for (var type=0; type < 20; type++)
        {
            source.AppendLine($"class AType_{file}_{type} {{");
            source.AppendLine("public:");
            for (var member=0; member < 8; member++) source.AppendLine($" int GetValue{member}(int input);");
            source.AppendLine("};");
        }
        File.WriteAllText(Path.Combine(root, $"Test{file}.h"), source.ToString());
    }
    var measurements = new List<object>();
    string[]? expected = null;
    for (var run=0; run < 3; run++)
    foreach (var worker in new[] { false, true })
    {
        var index = new PersistentSymbolIndex();
        var result = IndexBuildRunner.Build(index, "project", root, root, Path.GetFullPath(args[0]), worker);
        if (worker && !result.UsedWorker) throw new Exception("Worker integration failed: " + result.WorkerFailure);
        var identities = index.SearchSymbols("", limit:int.MaxValue).Select(item => item.Identity).OrderBy(value => value).ToArray();
        if (expected == null) expected=identities;
        if (!expected.SequenceEqual(identities)) throw new Exception("Worker and in-process symbols differ");
        if (index.SourceFiles().Count != 120) throw new Exception("Source file count mismatch");
        measurements.Add(new { Run=run+1, Mode=worker ? "worker" : "in-process", result.ElapsedMilliseconds, Symbols=index.Count });
    }
    var report = new { Date="2026-10-14 Asia/Seoul", Files=120, ClassesPerFile=20, MethodsPerClass=8,
        EqualSymbolIdentities=true, Scope="Synthetic project parse + lookup construction; worker includes process start and cache roundtrip. Not a VA or IDE UI benchmark.",
        Measurements=measurements };
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
    File.WriteAllText(args[1], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented=true }));
    Console.WriteLine(JsonSerializer.Serialize(report));
}
finally { Directory.Delete(root, true); }
