using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Web.Script.Serialization;
// Passive DTE inspection only: no UIA, focus, key injection, command execution or edits.
class ReadOnlyVsState {
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
  var document=dte.ActiveDocument;if(document==null||!Path.GetFullPath(document.FullName).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Not an owned fixture document");
  var selection=(EnvDTE.TextSelection)document.Selection;
  var commands=new List<object>();bool hasNativeVa=false;foreach(EnvDTE.Command command in (args.Length>3&&args[3]=="--selection-only"?Enumerable.Empty<EnvDTE.Command>():dte.Commands.Cast<EnvDTE.Command>())){string name=command.Name??"";if(CodeAssist.ParityRunner.NativeCommandNames.IsVa(name))hasNativeVa=true;if(name.IndexOf("SmartSelect",StringComparison.OrdinalIgnoreCase)>=0||name.IndexOf("ListMethods",StringComparison.OrdinalIgnoreCase)>=0||(command.Guid.Equals("{f9d77864-fac8-457e-bd4f-552715151b6c}",StringComparison.OrdinalIgnoreCase)&&command.ID>=0x112&&command.ID<=0x115)){commands.Add(new{name,guid=command.Guid,id=command.ID,bindings=command.Bindings});}}
  var registeredAliases=new List<object>();
  if(hasNativeVa)foreach(var suffix in new[]{"SmartSelectExtend","SmartSelectShrink","SmartSelectExtendBlock","SmartSelectShrinkBlock"}){
   string requested="VAssistX."+suffix;
   var resolved=CodeAssist.ParityRunner.NativeCommandNames.ResolveRegistered(requested,name=>{try{return dte.Commands.Item(name,0).Name;}catch(ArgumentException){return null;}catch(COMException error){if(error.ErrorCode==unchecked((int)0x8002000B)||error.ErrorCode==unchecked((int)0x80070057))return null;throw;}});
   registeredAliases.Add(new{requested,resolved});
  }
  var textDocument=document.Object("TextDocument") as EnvDTE.TextDocument;
  var editorText=textDocument==null?null:textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
  var result=new{readOnly=true,pid,registeredAliases,solution=dte.Solution.FullName,file=document.FullName,documentSaved=document.Saved,editorText,selection=new{anchorLine=selection.AnchorPoint.Line,anchorColumn=selection.AnchorPoint.LineCharOffset,activeLine=selection.ActivePoint.Line,activeColumn=selection.ActivePoint.LineCharOffset,text=selection.Text},commands};
  File.WriteAllText(args[2],new JavaScriptSerializer().Serialize(result));Console.WriteLine("Read-only VS selection and bindings saved");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
