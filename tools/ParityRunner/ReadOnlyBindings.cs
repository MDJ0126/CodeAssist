using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Web.Script.Serialization;
// Passive DTE inspection only: no UIA, focus, key injection, command execution or edits.
class ReadOnlyBindings {
 [DllImport("ole32.dll")] static extern int GetRunningObjectTable(int reserved,out IRunningObjectTable table);
 [DllImport("ole32.dll")] static extern int CreateBindCtx(int reserved,out IBindCtx context);
 static EnvDTE.DTE Attach(int pid) {
  IRunningObjectTable table;Marshal.ThrowExceptionForHR(GetRunningObjectTable(0,out table));IEnumMoniker items;table.EnumRunning(out items);var monikers=new IMoniker[1];
  while(items.Next(1,monikers,IntPtr.Zero)==0){IBindCtx context;Marshal.ThrowExceptionForHR(CreateBindCtx(0,out context));string name;monikers[0].GetDisplayName(context,null,out name);if(name=="!VisualStudio.DTE.17.0:"+pid){object value;table.GetObject(monikers[0],out value);return (EnvDTE.DTE)value;}}
  throw new InvalidOperationException("Requested PID has no DTE entry");
 }
 [STAThread]static int Main(string[] args){try{
  int pid=int.Parse(args[0]);var dte=Attach(pid);var root=Path.GetFullPath(args[1]).TrimEnd('\\')+"\\";
  if(!Path.GetFullPath(dte.Solution.FullName).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Not the owned fixture solution");
  var commands=new List<object>();
  foreach(EnvDTE.Command command in dte.Commands)
  {
   string name=command.Name??"";
   if(CodeAssist.ParityRunner.NativeCommandNames.IsVa(name)||command.Guid.Equals("{f9d77864-fac8-457e-bd4f-552715151b6c}",StringComparison.OrdinalIgnoreCase))
     commands.Add(new{name,guid=command.Guid,id=command.ID,bindings=command.Bindings});
  }
  var result=new{readOnly=true,pid,solution=dte.Solution.FullName,commands,capturedAt=DateTimeOffset.Now.ToString("o"),scope="Registered command bindings only; no key input, commands, editing or capture"};
  File.WriteAllText(args[2],new JavaScriptSerializer().Serialize(result));Console.WriteLine("Read-only VS selection and bindings saved");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
