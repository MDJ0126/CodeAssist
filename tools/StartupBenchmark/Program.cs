using System.Diagnostics;
using System.Text.Json;
using CodeAssist.Indexing;
var project = Path.GetFullPath(args[0]); var engine = Path.GetFullPath(args[1]); var report = Path.GetFullPath(args[2]);
var stages = new List<object>(); var process = Process.GetCurrentProcess();
T Measure<T>(string name, Func<T> work) { var cpu=process.TotalProcessorTime;var watch=Stopwatch.StartNew();var value=work();stages.Add(new {Name=name,ElapsedMs=watch.ElapsedMilliseconds,CpuMs=(process.TotalProcessorTime-cpu).TotalMilliseconds});Console.WriteLine(name+": "+watch.ElapsedMilliseconds+"ms");return value; }
var files=Measure("ProjectInputEnumeration",()=>PersistentSymbolIndex.InputFiles(project));
var model=Measure("CompilationModel",()=>ProjectCompilationModel.Load(project));
var index = new PersistentSymbolIndex(); Measure("ProjectColdParse",()=>{index.BuildProject(project);return index.SourceFiles().Count;});
var cache=Path.ChangeExtension(report,"index"); Measure("ProjectSave",()=>{index.Save(cache);return true;});
var warm=new PersistentSymbolIndex();Measure("ProjectWarmLoad",()=>warm.Load(cache));
var engineFiles=Measure("EngineInputEnumeration",()=>PersistentSymbolIndex.InputFiles(engine,project));
var headerCount=Measure("ConfiguredHeaderCandidates",()=>model.HeaderCandidates(engineFiles).Count);
var before=Measure("EngineInputFingerprint",()=>IndexInputFingerprint.Create(engineFiles));
var after=Measure("EngineWarmFingerprint",()=>IndexInputFingerprint.Create(PersistentSymbolIndex.InputFiles(engine,project)));
if(before!=after) throw new Exception("Inputs changed during measurement");
var facts=new {Project=project,Engine=engine,ProjectFiles=files.Length,EngineFiles=engineFiles.Length,CompilationTargets=model.Targets.Count,InputFingerprintStable=true,Stages=stages,Scope="Headless actual Unreal project input/cache processing; not a Visual Studio UI CPU trace. Engine parsing and cache load are not measured here."};
Directory.CreateDirectory(Path.GetDirectoryName(report)!);File.WriteAllText(report,JsonSerializer.Serialize(facts,new JsonSerializerOptions{WriteIndented=true}));
