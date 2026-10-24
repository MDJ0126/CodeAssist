using System;using System.Text;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Web.Script.Serialization;
class InspectNative {
 delegate bool Callback(IntPtr h,IntPtr p);
 [DllImport("user32.dll")] static extern bool EnumWindows(Callback cb,IntPtr p);
 [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h,Callback cb,IntPtr p);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder b,int n);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder b,int n);
 [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
 static int Main(string[] a){uint pid=uint.Parse(a[0]);var rows=new List<object>();Callback add=(h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id!=pid)return true;var c=new StringBuilder(256);var t=new StringBuilder(1024);GetClassName(h,c,256);GetWindowText(h,t,1024);if(a.Length==2&&a[1]=="close-options"&&t.ToString()=="Visual Assist Options")PostMessage(h,0x10,IntPtr.Zero,IntPtr.Zero);rows.Add(new{hwnd=h.ToInt64(),cls=c.ToString(),title=t.ToString(),visible=IsWindowVisible(h)});return true;};EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==pid){add(h,p);EnumChildWindows(h,add,p);}return true;},IntPtr.Zero);Console.WriteLine(new JavaScriptSerializer().Serialize(rows));return 0;}
}