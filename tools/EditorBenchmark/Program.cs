using CodeAssist.Core;
using System.Diagnostics;
using System.Text.Json;
var text = string.Join("\n", Enumerable.Range(0, 2000).Select(i => $"int Value{i} = {i}; void Call{i}() {{ Value{i}++; }}"));
CppSemanticParser.Parse("int Warmup; void Tick() {}");
var samples = new List<double>(); int count=0;
for(int i=0;i<3;i++) { var sw=Stopwatch.StartNew();count=CppSemanticParser.Parse(text).Count;samples.Add(sw.Elapsed.TotalMilliseconds); }
File.WriteAllText(args[0], JsonSerializer.Serialize(new { Characters=text.Length, Variables=2000, Tokens=count, SamplesMs=samples, Scope="Headless .NET 8 parser benchmark; not VS UI or VA comparison." }, new JsonSerializerOptions {WriteIndented=true}));
Console.WriteLine($"Parser: {count} tokens, {string.Join(", ", samples.Select(x=>x.ToString("F1")))} ms");
