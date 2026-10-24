using System.Diagnostics;
using System.Text.Json;
using CodeAssist.Core;
var text=string.Join("\r\n",Enumerable.Range(0,150).Select(n=>$"int Function{n}() {{ int value={n}; return value; }}"));
var locations=Enumerable.Range(0,1200).Select(n=>new ReferenceLocation("fixture.cpp",n%150+1,20)).ToArray();
var queries=new[]{"", "F", "Function", "Function1", "return value", "missing", "Function3,value", "INT return"};
string[] Legacy(string query){var lines=text.Split(new[]{"\r\n"},StringSplitOptions.None);var normalized=string.Join("\n",lines);var map=new BeforeCore.ReferenceContextMap(normalized);var starts=new int[lines.Length];for(var n=1;n<lines.Length;n++)starts[n]=starts[n-1]+lines[n-1].Length+1;return locations.Select(location=>{var owner=map.At(starts[location.Line-1]+location.Column-1);return (owner.Length>0?owner+" ":"")+"("+location.Line+"): "+lines[location.Line-1].Trim();}).Where(caption=>BeforeCore.ReferenceFilter.Matches(caption,query)).ToArray();}
var prepRuns=new List<double>();
var prep=Stopwatch.StartNew();var snapshot=ReferenceDisplaySnapshot.Create(locations,_=>text);prep.Stop();
for(var n=0;n<7;n++){var watch=Stopwatch.StartNew();var prepared=ReferenceDisplaySnapshot.Create(locations,_=>text);watch.Stop();prepRuns.Add(watch.Elapsed.TotalMilliseconds);if(prepared.Rows.Count!=locations.Length)throw new Exception("Preparation mismatch");}
string[] Cached(string query){var compiled=ReferenceFilter.Compile(query);return snapshot.Rows.Where(row=>compiled.Matches(row.Caption)).Select(row=>row.Caption).ToArray();}
foreach(var query in queries)if(!Legacy(query).SequenceEqual(Cached(query)))throw new Exception("Result mismatch: "+query);
object Measure(Func<string,string[]> action){action("return value");var runs=new List<double>();long allocation=0;long checksum=0;for(var n=0;n<7;n++){var before=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.StartNew();foreach(var query in queries)checksum+=action(query).Length;watch.Stop();allocation+=GC.GetAllocatedBytesForCurrentThread()-before;runs.Add(watch.Elapsed.TotalMilliseconds);}return new{medianMs=runs.OrderBy(x=>x).ElementAt(3),meanAllocatedBytes=allocation/7,timingsMs=runs.ToArray(),checksum};}
var legacy=Measure(Legacy);var cached=Measure(Cached);var report=new{scope="Pure caption preparation and filtering only; excludes COM, compiler search and WPF layout",documents=1,sourceCharacters=text.Length,referenceRows=locations.Length,queriesPerSample=queries.Length,samples=7,preparationColdMs=prep.Elapsed.TotalMilliseconds,preparationWarmMedianMs=prepRuns.OrderBy(x=>x).ElementAt(3),preparationTimingsMs=prepRuns.ToArray(),resultsEqual=true,legacy,cached};
var json=JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true});Console.WriteLine(json);if(args.Length>0)File.WriteAllText(args[0],json);
