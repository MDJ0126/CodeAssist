using CodeAssist.Semantics;
using Newtonsoft.Json.Linq;
if(args.Length!=2)throw new ArgumentException("clangd executable, output file");
var root=Path.Combine(Path.GetTempPath(),"CodeAssistActionProbe",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
File.WriteAllText(Path.Combine(root,"compile_flags.txt"),"-std=c++17\n");
using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
using var client=await ClangdClient.StartAsync(args[0],root,timeout.Token);
var results=new JArray();
var inputs=new[]{
 (Name:"Extract expression",Text:"int Calculate(int a, int b) {\n    int value = a + b;\n    return value * 2;\n}\n",Select:"a + b"),
 (Name:"Extract statements",Text:"int Calculate(int a, int b) {\n    int value = a + b;\n    value *= 2;\n    return value;\n}\n",Select:"int value = a + b;\n    value *= 2;"),
 (Name:"Outline definition",Text:"struct Widget {\n    int Value() { return 1; }\n};\n",Select:"Value"),
 (Name:"Outline header",Text:"#pragma once\nstruct Widget {\n    int Value() { return 1; }\n};\n",Select:"Value"),
 (Name:"Inline source",Text:"#include \"Inline_source.h\"\nint Widget::Value() { return 1; }\n",Select:"Widget::Value"),
 (Name:"Missing definition header",Text:"#pragma once\nstruct Widget {\n    int Value();\n};\n",Select:"Value"),
 (Name:"Expand auto",Text:"int Calculate() {\n    auto value = 1;\n    return value;\n}\n",Select:"auto"),
 (Name:"Add braces",Text:"int Calculate(int value) {\n    if (value > 0) return value;\n    return 0;\n}\n",Select:"if (value > 0) return value;"),
 (Name:"Remove braces",Text:"int Calculate(int value) {\n    if (value > 0) { return value; }\n    return 0;\n}\n",Select:"if (value > 0) { return value; }")};
foreach(var input in inputs)
{
 var file=Path.Combine(root,input.Name.Replace(' ','_')+(input.Name.Contains("header")?".h":".cpp"));File.WriteAllText(file,input.Text);
 if(input.Name=="Outline header"){var other=Path.ChangeExtension(file,".cpp");var body="#include \""+Path.GetFileName(file)+"\"\n";File.WriteAllText(other,body);await client.SyncAsync(other,body,timeout.Token);}
 if(input.Name=="Inline source"){var other=Path.ChangeExtension(file,".h");var body="#pragma once\nstruct Widget { int Value(); };\n";File.WriteAllText(other,body);await client.SyncAsync(other,body,timeout.Token);}
 await client.SyncAsync(file,input.Text,timeout.Token);
 var start=input.Text.IndexOf(input.Select,StringComparison.Ordinal);var end=start+input.Select.Length;
 JObject Position(int offset){var prefix=input.Text[..offset];var line=prefix.Count(c=>c=='\n');var newline=prefix.LastIndexOf('\n');return new JObject {["line"]=line,["character"]=offset-newline-1};}
 var actions=await client.AtAsync("textDocument/codeAction",file,(int)Position(start)["line"]!,(int)Position(start)["character"]!,timeout.Token,new JObject {["range"]=new JObject {["start"]=Position(start),["end"]=Position(end)},["context"]=new JObject {["diagnostics"]=new JArray(),["only"]=new JArray("refactor")}});
 var observed=new JArray();
 foreach(var action in (actions as JArray??new JArray()).OfType<JObject>())
 {
  var resolved=action;
  if(action["edit"]==null&&action["data"]!=null)resolved=(JObject)(await client.RequestAsync("codeAction/resolve",action.DeepClone(),timeout.Token));
  if(resolved["edit"]==null&&ClangdClient.SupportsPreviewCommand(resolved["command"]))resolved["edit"]=await client.PreviewCommandAsync((JObject)resolved["command"]!,timeout.Token);
  var row=new JObject {["title"]=resolved["title"]?.DeepClone(),["kind"]=resolved["kind"]?.DeepClone(),["resolved"]=resolved.DeepClone()};
  if(resolved["edit"]!=null)
  {
   try{var files=WorkspaceEdits.Files(resolved["edit"]);var changed=new JObject();foreach(var pair in files){var original=File.ReadAllText(pair.Key);changed[Path.GetFileName(pair.Key)]=WorkspaceEdits.Apply(original,pair.Value);}row["appliedPreview"]=changed;row["supportedTextEdit"]=true;if(File.ReadAllText(file)!=input.Text)throw new Exception("Preview unexpectedly modified a source file");}
   catch(Exception e){row["unsupported"]=e.Message;}
  }
  observed.Add(row);
 }
 results.Add(new JObject {["scenario"]=input.Name,["before"]=input.Text,["selectionStart"]=start,["selectionLength"]=input.Select.Length,["actions"]=observed});
 Console.WriteLine(input.Name+": "+string.Join(", ",observed.Select(a=>(string?)a["title"])));
}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
File.WriteAllText(args[1],new JObject {["scope"]="Actual clangd standalone C++ refactoring query and resolved text-edit previews; not IDE execution or VA parity",["clangd"]=args[0],["cases"]=results}.ToString());
if(!results.SelectMany(r=>(JArray)r["actions"]!).Any(a=>(bool?)a["supportedTextEdit"]==true))throw new Exception("No supported compiler edits");
