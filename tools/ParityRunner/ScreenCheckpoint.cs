using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace CodeAssist.ParityRunner {
internal static class ScreenCheckpoint {
 [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
 [DllImport("user32.dll")]private static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 internal static Task Capture(string root,string role,string id,string phase,IntPtr hwnd) {
  if(Environment.GetEnvironmentVariable("CODEASSIST_PARITY_SCREENSHOTS")!="1")return Task.CompletedTask;
  var folder=Path.Combine(root,"screenshots");Directory.CreateDirectory(folder);
  var name=role+"-"+id+"-"+phase;string status="capture-error",error=null;int width=0,height=0;uint targetPid=0;
  try {
   GetWindowThreadProcessId(hwnd,out targetPid);
   if(targetPid!=(uint)Process.GetCurrentProcess().Id&&Environment.GetEnvironmentVariable("CODEASSIST_PARITY_CAPTURE_TARGET_PID")!=targetPid.ToString())throw new InvalidOperationException("Refusing unrelated window");
   Rect rect;if(!GetWindowRect(hwnd,out rect))throw new InvalidOperationException("No target rectangle");
   width=rect.Right-rect.Left;height=rect.Bottom-rect.Top;if(width<20||height<20||width>10000||height>10000)throw new InvalidOperationException("Invalid capture dimensions");
   using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
    using(var graphics=Graphics.FromImage(bitmap)){var dc=graphics.GetHdc();try{if(!PrintWindow(hwnd,dc,2))throw new InvalidOperationException("PrintWindow failed");}finally{graphics.ReleaseHdc(dc);}}
    bitmap.Save(Path.Combine(folder,name+".png"),ImageFormat.Png);
   }
   status="captured";
  }catch(Exception ex){error=ex.ToString();}
  File.WriteAllText(Path.Combine(folder,name+".json"),new JavaScriptSerializer().Serialize(new {role,id,phase,status,error,width,height,targetPid,capturedAt=DateTimeOffset.Now.ToString("o"),method="PrintWindow PW_RENDERFULLCONTENT",file=name+".png"}));
  return Task.CompletedTask;
 }
}
}