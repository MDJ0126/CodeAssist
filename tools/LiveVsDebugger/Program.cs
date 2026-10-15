using Microsoft.Diagnostics.Runtime;
using System.Diagnostics;
using System.Text.Json;
if(args.Length != 2) throw new ArgumentException("Expected VS process ID and output path");
var process = Process.GetProcessById(int.Parse(args[0]));
if(process.ProcessName != "devenv") throw new InvalidOperationException("Target must be Visual Studio");
using var target = DataTarget.CreateSnapshotAndAttach(process.Id);
var results = new List<object>();
foreach(var info in target.ClrVersions)
{
    using var runtime = info.CreateRuntime();
    var modules = runtime.EnumerateModules().Where(m => (m.Name??"").Contains("CodeAssist", StringComparison.OrdinalIgnoreCase)).Select(m => m.Name).ToArray();
    var threads = new List<object>();
    foreach(var thread in runtime.Threads)
    {
        var frames = thread.EnumerateStackTrace().Take(250).Select(f => f.ToString() ?? "<native frame>").ToArray();
        if(frames.Any(f => f.Contains("CodeAssist") || f.Contains("OpenSpecificEditor") || f.Contains("MapLogicalView")))
            threads.Add(new { thread.OSThreadId, thread.ManagedThreadId, Frames=frames });
    }
    results.Add(new { Clr=info.Version.ToString(), Modules=modules, RelevantThreads=threads });
}
var report = new { ObservedUtc=DateTime.UtcNow, ProcessId=process.Id, Scope="Read-only process snapshot; no breakpoints, UI automation, heap values or full dump exported", Runtimes=results };
File.WriteAllText(args[1],JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented=true }));
Console.WriteLine(JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented=true }));

