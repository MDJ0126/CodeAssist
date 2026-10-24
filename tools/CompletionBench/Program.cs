using CodeAssist.Indexing;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
if (args.Length != 2) throw new ArgumentException("<output.json> <index source path>");
var fixture = Path.Combine(Path.GetTempPath(), "CodeAssist-CompletionBench-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixture);
try
{
    var source = new StringBuilder();
    for (var i = 0; i < 3000; i++) source.AppendLine($"void ComputeRouteValue{i:D4}() {{}}");
    source.AppendLine("class CompletionOwner { public:");
    for (var i = 0; i < 1500; i++) source.AppendLine($"void ComputeMemberValue{i:D4}() {{}}");
    source.AppendLine("};");
    var fixtureText = source.ToString();
    File.WriteAllText(Path.Combine(fixture, "Completion.cpp"), fixtureText);
    var index = new PersistentSymbolIndex();
    index.BuildProject(fixture);
    var queries = new[] { "Compute", "CRV", "CmpRtVal", "ComputeRouteValue1" };
    var outputs = queries.Select(q => new { query=q, global=index.Complete(q,200,true,true).Select(x=>x.Name).ToArray(), member=index.CompleteMembers("CompletionOwner",q,200,true,true).Select(x=>x.Name).ToArray() }).ToArray();
    var serialized = JsonSerializer.Serialize(outputs);
    long checksum = 0;
    void Batch()
    {
        for (var n=0;n<20;n++) foreach(var q in queries)
        {
            checksum += index.Complete(q,200,true,true).Count;
            checksum += index.CompleteMembers("CompletionOwner",q,200,true,true).Count;
        }
    }
    Batch();
    var timings = new List<double>(); var allocations = new List<long>();
    for (var sample=0;sample<7;sample++)
    {
        var allocated=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();Batch();timer.Stop();
        timings.Add(timer.Elapsed.TotalMilliseconds);allocations.Add(GC.GetAllocatedBytesForCurrentThread()-allocated);
    }
    var result=new { scope="Index candidate ranking only; excludes parsing, VS, compiler, WPF and keyboard latency", fixtureSHA256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixtureText))), indexSourceSHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))), queryBatchCalls=160,samples=7, medianMs=timings.OrderBy(x=>x).ElementAt(3),meanAllocatedBytes=allocations.Average(),timingsMs=timings,allocatedBytes=allocations,outputSHA256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized))),outputs,checksum};
    File.WriteAllText(args[0],JsonSerializer.Serialize(result,new JsonSerializerOptions { WriteIndented=true }));
    Console.WriteLine($"Median {result.medianMs:F2} ms; allocated {result.meanAllocatedBytes:F0}; output {result.outputSHA256}");
}
finally { Directory.Delete(fixture,true); }
