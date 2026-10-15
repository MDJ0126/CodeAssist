using CodeAssist.Indexing;
using System.Diagnostics;
using System.Text.Json;
var root=Path.GetFullPath(args[0]);
if(!Directory.Exists(root))throw new DirectoryNotFoundException(root);
var files=PersistentSymbolIndex.InputFiles(root);
var watch=Stopwatch.StartNew();var findings=ProjectSyntaxInspection.Scan(root);watch.Stop();
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Root=root,Files=files.Length,Findings=findings,ElapsedMs=watch.Elapsed.TotalMilliseconds,Scope="Headless project basic syntax checks; not VA checker equivalence or IDE Error List routing."},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Inspected {files.Length} files, {findings.Count} findings, {watch.Elapsed.TotalMilliseconds:F1}ms");
