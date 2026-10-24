using System;
using System.IO;
using System.Web.Script.Serialization;
namespace CodeAssist.ParityRunner
{
    internal static class ProbeHost
    {
        [MTAThread]
        private static int Main(string[] args)
        {
            try
            {
                if(args.Length!=5)return 2;
                var root=Environment.GetEnvironmentVariable("CODEASSIST_PARITY_ROOT");
                if(string.IsNullOrEmpty(root)||!File.Exists(Path.Combine(root,".codeassist-parity-owned")))return 3;
                var output=Path.GetFullPath(args[4]);
                if(!output.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return 4;
                DialogProbe.TargetProcessId=int.Parse(args[0]);
                var basename=Path.GetFileName(output);
                var role=basename.StartsWith("VA-",StringComparison.Ordinal)?"VA":basename.StartsWith("CA-",StringComparison.Ordinal)?"CA":null;
                if(role!=null&&basename.EndsWith(".dialog.json",StringComparison.Ordinal))
                {
                    var id=basename.Substring(3,basename.Length-3-".dialog.json".Length);
                    DialogProbe.StillCurrent=()=>{
                        try {string text;using(var stream=new FileStream(Path.Combine(root,role+".results.json"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new StreamReader(stream))text=reader.ReadToEnd();var state=(System.Collections.Generic.Dictionary<string,object>)new JavaScriptSerializer().DeserializeObject(text);return Convert.ToString(state["executing"])==id&&Convert.ToInt32(state["processId"])==DialogProbe.TargetProcessId;}
                        catch {return false;}
                    };
                }
                if(role!=null)DialogProbe.CaptureDialog=handle=>{Environment.SetEnvironmentVariable("CODEASSIST_PARITY_CAPTURE_TARGET_PID",DialogProbe.TargetProcessId.ToString());ScreenCheckpoint.Capture(root,role,basename.Substring(3,basename.Length-3-".dialog.json".Length),"dialog",handle).GetAwaiter().GetResult();};
                var page=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1]));
                var result=DialogProbe.RunAsync(page,int.Parse(args[2]),bool.Parse(args[3])).GetAwaiter().GetResult();
                var serializer=new JavaScriptSerializer {MaxJsonLength=4*1024*1024};
                var payload=(System.Collections.Generic.Dictionary<string,object>)serializer.DeserializeObject(serializer.Serialize(result));
                if(role!=null&&basename.EndsWith(".dialog.json",StringComparison.Ordinal))
                {
                    var plan=(System.Collections.Generic.Dictionary<string,object>)serializer.DeserializeObject(File.ReadAllText(Path.Combine(root,role+".plan.json")));
                    payload["evidenceRunId"]=plan["runId"];payload["evidenceRole"]=role;
                    payload["evidenceCaseId"]=basename.Substring(3,basename.Length-3-".dialog.json".Length);payload["evidenceProcessId"]=DialogProbe.TargetProcessId;
                }
                File.WriteAllText(output,serializer.Serialize(payload));
                return 0;
            }
            catch(Exception error){System.Console.Error.WriteLine(error);return 1;}
        }
    }
}
