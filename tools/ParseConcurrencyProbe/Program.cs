using CodeAssist.Indexing;using System.Text.Json;using System.Diagnostics;
class Program {
 static int Main(string[] args){
 var root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);var input=Path.Combine(root,"input");Directory.CreateDirectory(input);
 for(int f=0;f<160;f++){var b=new System.Text.StringBuilder();for(int n=0;n<160;n++)b.AppendLine($"int Parse105_{f}_{n}(int value) {{ int local = value; return local; }}");File.WriteAllText(Path.Combine(input,$"file{f}.cpp"),b.ToString());}
 ThreadPool.GetMinThreads(out var min,out var io);ThreadPool.SetMinThreads(4,io);var output=new List<object>();
 foreach(var concurrency in new[]{1,4,1}){
 var index=new PersistentSymbolIndex{ParseConcurrency=concurrency};int peak=0,samples=0;bool done=false;ThreadPool.GetMaxThreads(out var maximum,out _);
 var monitor=new Thread(()=>{while(!Volatile.Read(ref done)){ThreadPool.GetAvailableThreads(out var available,out _);InterlockedExtensions.Max(ref peak,maximum-available);Interlocked.Increment(ref samples);Thread.Sleep(1);}});monitor.IsBackground=true;
 var sw=Stopwatch.StartNew();monitor.Start();index.BuildProject(input);Volatile.Write(ref done,true);monitor.Join();sw.Stop();var count=index.SourceFiles().Count;var rows=index.FindDefinitions("Parse105_159_159").Count;
 output.Add(new{configured=concurrency,peakBusyThreadPoolWorkers=peak,samples,files=count,lastSymbolDefinitions=rows,elapsedMilliseconds=sw.ElapsedMilliseconds});
 if(count!=160||rows!=1||samples<3||concurrency==1&&peak!=0||concurrency==4&&peak==0)throw new Exception("Concurrency observation or output contract failed");
 Thread.Sleep(150);
 }
 File.WriteAllText(Path.Combine(root,"CA.parse-runtime.json"),JsonSerializer.Serialize(new{scope="Unmodified indexing assembly, synchronous BuildProject, dedicated monitor thread, A-B-A 1/4/1. No other ThreadPool workload in this process. Busy workers are runtime workers; not stack-attributed parser call count.",assembly=typeof(PersistentSymbolIndex).Assembly.Location,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(typeof(PersistentSymbolIndex).Assembly.Location))),rows=output},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(output));return 0;
 }
}
static class InterlockedExtensions { public static void Max(ref int target,int v){int old;do{old=Volatile.Read(ref target);if(old>=v)return;}while(Interlocked.CompareExchange(ref target,v,old)!=old);} }
