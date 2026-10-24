using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
namespace CodeAssist.Extension {
 internal static class KeyboardCompatibility {
  private static string EnglishKeys(string value)=>value.Replace("왼쪽 화살표","Left Arrow").Replace("오른쪽 화살표","Right Arrow").Replace("위쪽 화살표","Up Arrow").Replace("아래쪽 화살표","Down Arrow");
  private static string Canonical(string value){var parts=EnglishKeys(value).ToUpperInvariant().Split(new[]{"::"},StringSplitOptions.None);return parts[0]+"::"+string.Join("+",parts[parts.Length-1].Replace(" ARROW","").Split('+').OrderBy(v=>v));}
  internal static bool Apply(EnvDTE80.DTE2 dte) {
   ThreadHelper.ThrowIfNotOnUIThread();
   var report=new JArray();var success=true;
   var mappings=new[]{Tuple.Create(0x011a,"Global::Alt+K"),Tuple.Create(0x0116,"Global::Ctrl+Shift+V"),Tuple.Create(0x0110,"Global::Alt+Shift+H"),Tuple.Create(0x0112,"Global::Alt+Shift+]"),Tuple.Create(0x0113,"Global::Alt+Shift+["),Tuple.Create(0x0114,"Global::Alt+]"),Tuple.Create(0x0115,"Global::Alt+["),Tuple.Create(0x010f,"Global::Ctrl+0"),Tuple.Create(0x0100,"Global::F12"),Tuple.Create(0x0100,"Global::Alt+G"),Tuple.Create(0x0102,"Global::Shift+F12"),Tuple.Create(0x0102,"Global::Alt+Shift+F"),Tuple.Create(0x0103,"Global::Alt+Shift+O"),Tuple.Create(0x0104,"Global::Alt+Shift+S"),Tuple.Create(0x0105,"Global::Alt+O"),Tuple.Create(0x0106,"Global::Alt+M"),Tuple.Create(0x0107,"Global::Alt+Shift+G"),Tuple.Create(0x0108,"Global::Alt+Left Arrow"),Tuple.Create(0x0109,"Global::Alt+Right Arrow"),Tuple.Create(0x010a,"Global::Alt+Shift+R"),Tuple.Create(0x010b,"Global::Alt+Shift+Q"),Tuple.Create(0x010c,"Global::Alt+Shift+C"),Tuple.Create(0x010d,"Global::Alt+VK_DOWN"),Tuple.Create(0x010e,"Global::Alt+VK_UP")};
   string scope=dte.LocaleID==1042?"전역":"Global";
   try {var sample=(object[])dte.Commands.Item(GoToSymbolCommand.CommandSet.ToString("B"),0x11a).Bindings;scope=sample[0].ToString().Split(new[]{"::"},StringSplitOptions.None)[0];}catch(Exception){}
   var korean=scope=="전역";
   var editorScope=korean?"텍스트 편집기":"Text Editor";
   foreach(var group in mappings.GroupBy(m=>m.Item1)) {
    var row=new JObject {["id"]=group.Key};report.Add(row);
    try {
     var command=dte.Commands.Item(GoToSymbolCommand.CommandSet.ToString("B"),group.Key);
     var old=command.Bindings as object[]??new object[0];row["before"]=JArray.FromObject(old);
     row["scope"]=scope;
     var desired=group.Select(m=>m.Item2.Replace("Global::",scope+"::").Replace("VK_DOWN",korean?"아래쪽 화살표":"Down Arrow").Replace("VK_UP",korean?"위쪽 화살표":"Up Arrow").Replace("Left Arrow",korean?"왼쪽 화살표":"Left Arrow").Replace("Right Arrow",korean?"오른쪽 화살표":"Right Arrow")).SelectMany(k=>new[]{k,k.Replace(scope+"::",editorScope+"::")}).Distinct().ToArray();
     Exception failure=null;
     foreach(var variant in new[]{desired,desired.Select(EnglishKeys).ToArray(),desired.Select(k=>EnglishKeys(k).Replace(" Arrow","")).ToArray()}) {
        try {command.Bindings=variant.Cast<object>().ToArray();failure=null;break;}catch(Exception ex){failure=ex;}
     }
     if(failure!=null)throw failure;
     var actual=(object[])command.Bindings;row["after"]=JArray.FromObject(actual);
     row["passed"]=desired.All(k=>actual.Any(v=>Canonical(v.ToString())==Canonical(k)));
     if(!(bool)row["passed"])success=false;
    } catch(Exception ex){row["error"]=ex.Message;row["passed"]=false;success=false;}
   }
   var folder=Path.GetDirectoryName(StartupUi.StatePath);Directory.CreateDirectory(folder);
   var path=Path.Combine(folder,"keyboard-compatibility.json");
   if(File.Exists(path))File.Copy(path,path+"."+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".bak");
   File.WriteAllText(path,new JObject{["version"]=ExtensionPackage.ProductVersion,["profile"]=dte.RegistryRoot,["completedUtc"]=DateTime.UtcNow.ToString("O"),["passed"]=success,["commands"]=report}.ToString());
   return success;
  }
 }
}
