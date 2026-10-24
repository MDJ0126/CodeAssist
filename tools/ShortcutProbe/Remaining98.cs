using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using System.Drawing;using System.Drawing.Imaging;using CodeAssist.ParityRunner;
partial class ShortcutProbe{
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool PrintWindow(IntPtr h,IntPtr dc,uint f);
 static void WindowShot(IntPtr h,string file){var el=System.Windows.Automation.AutomationElement.FromHandle(h);var r=el.Current.BoundingRectangle;if(r.Width<10||r.Height<10)throw new Exception("Window has no drawable size");using(var b=new Bitmap((int)r.Width,(int)r.Height)){using(var g=Graphics.FromImage(b)){var dc=g.GetHdc();try{if(!PrintWindow(h,dc,2))throw new Exception("PrintWindow failed");}finally{g.ReleaseHdc(dc);}}b.Save(file,ImageFormat.Png);}}
}
