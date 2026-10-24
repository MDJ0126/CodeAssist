using CodeAssist.Core;
using System.Diagnostics;
using System.Text.Json;
var root=Path.Combine(Path.GetTempPath(),"CodeAssistCppSyntax",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
var instance="struct Widget { void Run() {} };\nvoid F() { Widget value; value.Run(); }";
var pointer="struct Widget { void Run() {} };\nvoid F() { Widget* value = new Widget(); value->Run(); }";
var source="void Work(int& value, int count) {\n    value += count;\n}\n";
var cases=new Dictionary<string,string>{
 ["definition"]="struct Widget { int Value(int index = 2) const; };\n"+CppRefactoring.CreateImplementation("int Value(int index = 2) const;","Widget"),
 ["overrides"]="struct Base { virtual void Update() = 0; };\nstruct Derived: Base {\n"+CppRefactoring.CreateImplementations("virtual void Update() = 0;","",overrides:true)+"};\n",
 ["encapsulate"]="struct Widget {\n"+CppRefactoring.EncapsulateField("int health;","Health")+"};\n",
 ["instance_to_pointer"]=CppDocumentTransforms.ConvertLocalPointer(instance,instance.IndexOf("Widget value"),"Widget value;".Length),
 ["pointer_to_instance"]=CppDocumentTransforms.ConvertLocalPointer(pointer,pointer.IndexOf("Widget* value"),"Widget* value = new Widget();".Length),
 ["extract"]=CppDocumentTransforms.ExtractGlobalMethod(source,source.IndexOf("value +="),"value += count;".Length,"void Extracted(int& value, int count)")};
var rows=new List<object>();var failed=false;
foreach(var pair in cases)
{
 var file=Path.Combine(root,pair.Key+".cpp");File.WriteAllText(file,pair.Value);
 using var process=new Process {StartInfo=new ProcessStartInfo(args[0]){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};
 foreach(var arg in new[]{"-std=c++17","-fsyntax-only",file})process.StartInfo.ArgumentList.Add(arg);
 process.Start();try{process.PriorityClass=ProcessPriorityClass.BelowNormal;}catch{}var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));await process.WaitForExitAsync(timeout.Token);
 var diagnostic=await errors;var passed=process.ExitCode==0;failed|=!passed;rows.Add(new {name=pair.Key,passed,exitCode=process.ExitCode,source=pair.Value,diagnostic});Console.WriteLine(pair.Key+": "+passed);
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new {scope="Actual clang++ syntax validation of generated C++; no IDE execution or VA parity",compiler=args[0],cases=rows},new JsonSerializerOptions{WriteIndented=true}));
if(failed)Environment.Exit(1);
