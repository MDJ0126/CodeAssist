using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Diagnostics.Runtime;

static object Inspect(Process process,long expectedStart,string moduleName)
{
    if(process.StartTime.ToUniversalTime().Ticks!=expectedStart)throw new InvalidOperationException("Process identity changed");
    var timer=Stopwatch.StartNew();
    // Snapshot reader: no input, commands, injected code or heap dump file.
    using var target=DataTarget.CreateSnapshotAndAttach(process.Id);
    var modules=new List<string>();
    foreach(var info in target.ClrVersions)
    {
        using var runtime=info.CreateRuntime();
        foreach(var module in runtime.EnumerateModules())
            if(string.Equals(Path.GetFileName(module.Name),moduleName,StringComparison.OrdinalIgnoreCase)&&module.Name!=null)modules.Add(module.Name);
    }
    var paths=modules.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    if(paths.Length!=1)throw new InvalidOperationException($"Expected one CLR module path, found {paths.Length}");
    var path=Path.GetFullPath(paths[0]);
    return new {pid=process.Id,startTicks=expectedStart,method="ClrMD process snapshot / CLR EnumerateModules",module=path,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),elapsedMilliseconds=timer.Elapsed.TotalMilliseconds};
}

try
{
    if(args.Length==1&&args[0]=="--fixture")
    {Console.WriteLine("READY");Console.Out.Flush();Console.ReadLine();return 0;}
    if(args.Length==2&&args[0]=="--self-test")
    {
        var executable=Environment.ProcessPath??throw new InvalidOperationException("No executable");
        var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
        if(Path.GetFileNameWithoutExtension(executable).Equals("dotnet",StringComparison.OrdinalIgnoreCase))start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--fixture");
        using var child=Process.Start(start)??throw new InvalidOperationException("Fixture not started");
        try
        {
            var ready=await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));if(ready!="READY")throw new InvalidOperationException("Fixture handshake failed");
            var result=Inspect(child,child.StartTime.ToUniversalTime().Ticks,Path.GetFileName(Assembly.GetExecutingAssembly().Location));
            var json=JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true});
            using var parsed=JsonDocument.Parse(json);var expected=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location)));
            if(parsed.RootElement.GetProperty("sha256").GetString()!=expected)throw new InvalidOperationException("Self-test hash mismatch");
            File.WriteAllText(args[1],json);Console.WriteLine("Self-test: CLR module path and hash match owned fixture");
        }
        finally{if(!child.HasExited){await child.StandardInput.WriteLineAsync("exit");if(!child.WaitForExit(10000))throw new InvalidOperationException("Owned fixture did not exit");}}
        return 0;
    }
    if(args.Length==5&&args[0]=="--owned-va")
    {
        var ownedPid=int.Parse(args[1]);var expectedTicks=long.Parse(args[2]);var ownedRoot=Path.GetFullPath(args[4]);
        using var ownedHosts=JsonDocument.Parse(File.ReadAllText(Path.Combine(ownedRoot,".local","parity-runs","background-103","hosts.json")));
        if(ownedHosts.RootElement.GetProperty("VA").GetProperty("pid").GetInt32()!=ownedPid)throw new InvalidOperationException("Not the owned VA host");
        using var ownedProcess=Process.GetProcessById(ownedPid);
        if(!ownedProcess.ProcessName.Equals("devenv",StringComparison.OrdinalIgnoreCase)||ownedProcess.StartTime.ToUniversalTime().Ticks!=expectedTicks)throw new InvalidOperationException("Owned VA identity changed");
        var found=new List<string>();
        using(var snapshot=DataTarget.CreateSnapshotAndAttach(ownedPid))
        foreach(var clr in snapshot.ClrVersions)
        {
            using var runtime=clr.CreateRuntime();
            found.AddRange(runtime.EnumerateModules().Where(m=>m.Name!=null&&Path.GetFileName(m.Name).Equals("CodeAssist.Extension.dll",StringComparison.OrdinalIgnoreCase)).Select(m=>m.Name!));
        }
        File.WriteAllText(args[3],JsonSerializer.Serialize(new {pid=ownedPid,startTicks=expectedTicks,method="ClrMD read-only module enumeration",codeAssistExtensionModules=found.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),isolated=found.Count==0},new JsonSerializerOptions{WriteIndented=true}));
        if(found.Count!=0)throw new InvalidOperationException("CA extension is loaded in VA host");
        Console.WriteLine("Owned VA: CA extension absent from CLR modules");return 0;
    }
    if(args.Length!=6||args[0]!="--owned-ca")throw new ArgumentException("Use --owned-ca <pid> <UTC start ticks> <expected installed DLL> <output> <owned root>");
    var pid=int.Parse(args[1]);var ticks=long.Parse(args[2]);var expectedPath=Path.GetFullPath(args[3]);var root=Path.GetFullPath(args[5]);
    var hostsPath=Path.Combine(root,".local","parity-runs","background-103","hosts.json");
    using var hosts=JsonDocument.Parse(File.ReadAllText(hostsPath));if(hosts.RootElement.GetProperty("CA").GetProperty("pid").GetInt32()!=pid)throw new InvalidOperationException("Not the owned CA host");
    using var targetProcess=Process.GetProcessById(pid);if(!targetProcess.ProcessName.Equals("devenv",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Not Visual Studio");
    if(!Path.GetFileName(expectedPath).Equals("CodeAssist.Extension.dll",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unexpected module");
    var probe=Inspect(targetProcess,ticks,"CodeAssist.Extension.dll");var output=JsonSerializer.Serialize(probe,new JsonSerializerOptions{WriteIndented=true});
    using var report=JsonDocument.Parse(output);if(!string.Equals(report.RootElement.GetProperty("module").GetString(),expectedPath,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Loaded CLR module differs from installed path");
    File.WriteAllText(args[4],output);Console.WriteLine("Owned CA CLR module verified");return 0;
}
catch(Exception error){Console.Error.WriteLine(error.Message);return 2;}
