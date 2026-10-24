using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
namespace CodeAssist.Extension
{
    // Standard common-control drawing only. No input synthesis or global hooks.
    internal sealed class NativeBrowserColors:IDisposable
    {
        private readonly ProjectIndexService index;private readonly HashSet<IntPtr> controls=new HashSet<IntPtr>(),parents=new HashSet<IntPtr>();
        private readonly SubclassProc callback;private bool enabled;private readonly LiteTreeColors lite;
        internal NativeBrowserColors(ProjectIndexService index){this.index=index;callback=Draw;lite=new LiteTreeColors(index);}
        internal void Observe(IntPtr frame,bool enabled)
        {
            this.enabled=enabled;if(frame==IntPtr.Zero)return;
            var classes=new HashSet<string>();
            EnumChildWindows(frame,(child,data)=>{var name=new StringBuilder(256);GetClassName(child,name,name.Capacity);var type=name.ToString();classes.Add(type);
                if(type=="LiteTreeView32"){lite.Observe(child);InvalidateRect(child,IntPtr.Zero,false);}
                if(type=="SysTreeView32"){controls.Add(child);var parent=GetParent(child);if(parent!=IntPtr.Zero&&parents.Add(parent))SetWindowSubclass(parent,callback,new UIntPtr(0xCA129),UIntPtr.Zero);InvalidateRect(child,IntPtr.Zero,false);}return true;},IntPtr.Zero);
            if(classes.Count>0&&lastClasses!=string.Join(";",classes)){lastClasses=string.Join(";",classes);WelcomeDocumentWindow.RecordEvent("ObjectBrowserNative controls="+controls.Count+" classes="+lastClasses);}
        }
        private string lastClasses;
        [StructLayout(LayoutKind.Sequential)]private struct Header {internal IntPtr Window;internal UIntPtr Id;internal int Code;}
        [StructLayout(LayoutKind.Sequential)]private struct Rect {internal int Left,Top,Right,Bottom;}
        [StructLayout(LayoutKind.Sequential)]private struct CustomDraw {internal Header Header;internal uint Stage;internal IntPtr Dc;internal Rect Bounds;internal UIntPtr Item;internal uint State;internal IntPtr Param;internal uint Foreground,Background;internal int Level;}
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct TreeItem {internal uint Mask;internal IntPtr Item;internal uint State,StateMask;internal IntPtr Text;internal int TextCapacity,Image,SelectedImage,Children;internal IntPtr Param;}
        private IntPtr Draw(IntPtr window,uint message,UIntPtr wparam,IntPtr lparam,UIntPtr id,UIntPtr data)
        {
            var result=DefSubclassProc(window,message,wparam,lparam);
            if(message==0x82){RemoveWindowSubclass(window,callback,id);parents.Remove(window);return result;}
            if(message!=0x4E||lparam==IntPtr.Zero||!enabled||!StartupUi.Preferences.ColoringEnabled||!StartupUi.Preferences.ColorObjectBrowser)return result;
            try{
                var header=(Header)Marshal.PtrToStructure(lparam,typeof(Header));if(header.Code!=-12||!controls.Contains(header.Window))return result;
                var draw=(CustomDraw)Marshal.PtrToStructure(lparam,typeof(CustomDraw));if(draw.Stage==1)return new IntPtr(result.ToInt64()|0x20);
                if(draw.Stage!=0x10001||(draw.State&1)!=0)return result;
                var text=Marshal.AllocHGlobal(1024);try{var item=new TreeItem {Mask=1,Item=(IntPtr)draw.Item.ToUInt64(),Text=text,TextCapacity=512};
                    if(SendMessage(header.Window,0x113E,IntPtr.Zero,ref item)==IntPtr.Zero)return result;
                    var name=Marshal.PtrToStringUni(text)??"";var symbols=index?.Index.FindDefinitions(name);
                    if(symbols==null||symbols.Count==0)return result;var brush=RecommendedColors.SymbolBrush(symbols[0].Kind) as SolidColorBrush;if(brush==null)return result;
                    var color=brush.Color;draw.Foreground=(uint)(color.R|(color.G<<8)|(color.B<<16));Marshal.StructureToPtr(draw,lparam,false);return new IntPtr(result.ToInt64()|2);
                }finally{Marshal.FreeHGlobal(text);}
            }catch(Exception error){WelcomeDocumentWindow.RecordEvent("ObjectBrowserNative draw="+error.GetType().Name);return result;}
        }
        public void Dispose(){lite.Dispose();enabled=false;foreach(var parent in parents)RemoveWindowSubclass(parent,callback,new UIntPtr(0xCA129));foreach(var child in controls)InvalidateRect(child,IntPtr.Zero,false);parents.Clear();controls.Clear();}
        private delegate bool EnumProc(IntPtr child,IntPtr data);
        private delegate IntPtr SubclassProc(IntPtr window,uint message,UIntPtr wparam,IntPtr lparam,UIntPtr id,UIntPtr data);
        [DllImport("user32.dll")]private static extern bool EnumChildWindows(IntPtr parent,EnumProc proc,IntPtr data);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr window,StringBuilder name,int count);
        [DllImport("user32.dll")]private static extern IntPtr GetParent(IntPtr window);
        [DllImport("user32.dll")]private static extern bool InvalidateRect(IntPtr window,IntPtr rect,bool erase);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wparam,ref TreeItem item);
        [DllImport("comctl32.dll")]private static extern bool SetWindowSubclass(IntPtr window,SubclassProc proc,UIntPtr id,UIntPtr data);
        [DllImport("comctl32.dll")]private static extern bool RemoveWindowSubclass(IntPtr window,SubclassProc proc,UIntPtr id);
        [DllImport("comctl32.dll")]private static extern IntPtr DefSubclassProc(IntPtr window,uint message,UIntPtr wparam,IntPtr lparam);
    }
}
