using System;using System.IO;using System.Reflection;using System.Threading.Tasks;using System.Collections.Generic;using System.Security.Cryptography;using Microsoft.VisualStudio.Shell;
namespace CodeAssist.ParityRunner {
internal static class LiveModule {
internal static async Task RunAsync(AsyncPackage package,string root,string role,Dictionary<string,object> item,string id){
 var json=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=32*1024*1024};object result=null;string error=null,sha=null;
 try{string allowed=Path.Combine(Path.GetFullPath(root),"modules")+Path.DirectorySeparatorChar,path=Path.GetFullPath((string)item["module"]);if(!path.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||!File.Exists(path)||Path.GetExtension(path)!=".dll")throw new InvalidOperationException("Module outside owned module folder");using(var hash=SHA256.Create())sha=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();if(sha!=(string)item["sha256"])throw new InvalidOperationException("Module hash mismatch");var assembly=Assembly.LoadFrom(path);var type=assembly.GetType("CodeAssist.ParityExperiments.LiveProbe",true);var method=type.GetMethod("RunAsync",BindingFlags.Public|BindingFlags.Static);if(method==null)throw new InvalidOperationException("Missing probe entry point");await package.JoinableTaskFactory.SwitchToMainThreadAsync();result=await (Task<object>)method.Invoke(null,new object[]{package,root,role,item});}
 catch(Exception ex){error=ex.ToString();}
 File.WriteAllText(Path.Combine(root,"background-capture",role+"-"+id+"-module.json"),json.Serialize(new{id,role,pid=System.Diagnostics.Process.GetCurrentProcess().Id,status=error==null?"complete":"error",sha256=sha,result,error,at=DateTimeOffset.Now.ToString("o")}));
}
}
}
