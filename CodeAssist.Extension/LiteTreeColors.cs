using CodeAssist.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
namespace CodeAssist.Extension
{
    // In-process import-table adapters for the module registering LiteTreeView32.
    // No remote patching, code detours, input hooks, or changes to other controls.
    internal sealed class LiteTreeColors : IDisposable
    {
        private readonly ProjectIndexService index;
        private readonly HashSet<IntPtr> windows=new HashSet<IntPtr>();
        private readonly HashSet<IntPtr> modules=new HashSet<IntPtr>();
        private readonly List<ImportSlot> slots=new List<ImportSlot>();
        private readonly Dictionary<IntPtr,Dictionary<ushort,char>> glyphs=new Dictionary<IntPtr,Dictionary<ushort,char>>();
        private readonly uint thread=GetCurrentThreadId();
        private bool disposed;private readonly HashSet<string> traced=new HashSet<string>();
        private sealed class ImportSlot {internal IntPtr Address,Original,Replacement;internal Delegate Callback;}
        // Keep retired callbacks alive for a renderer already inside an adapter.
        private static readonly List<Delegate> retired=new List<Delegate>();
        internal LiteTreeColors(ProjectIndexService index){this.index=index;}
        internal void Observe(IntPtr window)
        {
            if(disposed||!IsWindow(window))return;
            windows.Add(window);
            var module=IntPtr.Size==8?GetClassLongPtr64(window,-16):new IntPtr(unchecked((long)GetClassLong32(window,-16)));
            if(module==IntPtr.Zero||!modules.Add(module))return;
            try{Install(module);WelcomeDocumentWindow.RecordEvent("LiteTreeColors module="+module+" importSlots="+slots.Count);}
            catch(Exception error){WelcomeDocumentWindow.RecordEvent("LiteTreeColors install="+error.GetType().Name);}
        }
        private void Install(IntPtr module)
        {
            if(Marshal.ReadInt16(module)!=0x5A4D)return;
            var nt=IntPtr.Add(module,Marshal.ReadInt32(module,0x3c));if(Marshal.ReadInt32(nt)!=0x4550)return;
            var optional=IntPtr.Add(nt,24);var magic=(ushort)Marshal.ReadInt16(optional);
            if(magic!=0x20b&&magic!=0x10b)return;
            var importRva=Marshal.ReadInt32(optional,(magic==0x20b?112:96)+8);if(importRva==0)return;
            var descriptor=IntPtr.Add(module,importRva);
            for(var d=0;d<512;d++,descriptor=IntPtr.Add(descriptor,20)){
                var nameRva=Marshal.ReadInt32(descriptor,12);if(nameRva==0)break;
                var library=Marshal.PtrToStringAnsi(IntPtr.Add(module,nameRva))??"";
                if(library.IndexOf("gdi",StringComparison.OrdinalIgnoreCase)<0)continue;
                var namesRva=Marshal.ReadInt32(descriptor);var iatRva=Marshal.ReadInt32(descriptor,16);if(namesRva==0||iatRva==0)continue;
                for(var n=0;n<16384;n++){
                    var entry=Marshal.ReadIntPtr(IntPtr.Add(module,namesRva+n*IntPtr.Size));var value=unchecked((ulong)entry.ToInt64());if(value==0)break;
                    if((value&(IntPtr.Size==8?0x8000000000000000UL:0x80000000UL))!=0||value>int.MaxValue)continue;
                    var name=Marshal.PtrToStringAnsi(IntPtr.Add(module,(int)value+2));var address=IntPtr.Add(module,iatRva+n*IntPtr.Size);
                    if(name=="TextOutW"){
                        var original=(TextOut)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(address),typeof(TextOut));
                        TextOut callback=(dc,x,y,text,count)=>Paint(dc,text,count,false,x,y,IntPtr.Zero,false,(offset,length,dx,dy,first)=>original(dc,dx,dy,IntPtr.Add(text,offset*2),length));Hook(address,callback);
                    }else if(name=="ExtTextOutW"){
                        var original=(ExtTextOut)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(address),typeof(ExtTextOut));
                        ExtTextOut callback=(dc,x,y,options,rect,text,count,widths)=>Paint(dc,text,unchecked((int)count),(options&0x10)!=0,x,y,widths,(options&0x2000)!=0,(offset,length,dx,dy,first)=>original(dc,dx,dy,first?options:options&~2u,rect,IntPtr.Add(text,offset*2),(uint)length,widths==IntPtr.Zero?IntPtr.Zero:IntPtr.Add(widths,offset*((options&0x2000)!=0?8:4))));Hook(address,callback);
                    }
                }
            }
        }
        private void Hook(IntPtr address,Delegate callback)
        {
            var slot=new ImportSlot {Address=address,Original=Marshal.ReadIntPtr(address),Callback=callback,Replacement=Marshal.GetFunctionPointerForDelegate(callback)};
            uint protection;if(!VirtualProtect(address,(UIntPtr)IntPtr.Size,0x04,out protection))return;
            try{Marshal.WriteIntPtr(address,slot.Replacement);slots.Add(slot);}finally{uint ignored;VirtualProtect(address,(UIntPtr)IntPtr.Size,protection,out ignored);}
        }
        private bool Paint(IntPtr dc,IntPtr text,int count,bool glyphIndices,int x,int y,IntPtr widths,bool pairedWidths,Func<int,int,int,int,bool,bool> render)
        {
            var baseColor=GetTextColor(dc);
            try{
                if(disposed||GetCurrentThreadId()!=thread||!StartupUi.Preferences.ColoringEnabled||!StartupUi.Preferences.ColorObjectBrowser||count<=0||count>512||text==IntPtr.Zero||!windows.Contains(WindowFromDC(dc))||(GetTextAlign(dc)&0x107)!=0)return render(0,count,x,y,true);
                var value=glyphIndices?Decode(dc,text,count):Marshal.PtrToStringUni(text,count);if(value==null)return render(0,count,x,y,true);
                var tokens=SymbolDisplayTokens.Parse(value,resolve:name=>{var definitions=index?.Index.FindDefinitions(name);return definitions!=null&&definitions.Count>0?definitions[0].Kind:SymbolKind.Unknown;});
                if(tokens.Count==0)return render(0,count,x,y,true);
                var pieces=new List<Tuple<int,int,Brush>>();var cursor=0;
                foreach(var token in tokens){if(token.Start>cursor)pieces.Add(Tuple.Create(cursor,token.Start-cursor,(Brush)null));pieces.Add(Tuple.Create(token.Start,token.Length,RecommendedColors.DisplayBrush(token.Ink)));cursor=token.Start+token.Length;}
                if(cursor<count)pieces.Add(Tuple.Create(cursor,count-cursor,(Brush)null));
                // Calculate every position before emitting text; unsupported metrics fall back intact.
                var positions=new List<Size>();foreach(var piece in pieces){Size position=new Size();if(widths!=IntPtr.Zero){for(int i=0;i<piece.Item1;i++){position.X+=Marshal.ReadInt32(widths,i*(pairedWidths?8:4));if(pairedWidths)position.Y+=Marshal.ReadInt32(widths,i*8+4);}}else if(piece.Item1>0){bool ok=glyphIndices?GetTextExtentPointI(dc,text,piece.Item1,out position):GetTextExtentPoint32W(dc,text,piece.Item1,out position);if(!ok)return render(0,count,x,y,true);position.Y=0;}positions.Add(position);}
                if(traced.Count<20&&traced.Add(value)){var log=new StringBuilder("LiteTreeSegments text="+value+" glyph="+glyphIndices+" widths="+(widths!=IntPtr.Zero)+" xy="+x+","+y+" align="+GetTextAlign(dc)+" base="+baseColor.ToString("X"));for(int i=0;i<pieces.Count;i++)log.Append(" ["+pieces[i].Item1+","+pieces[i].Item2+" @"+positions[i].X+","+positions[i].Y+"]");WelcomeDocumentWindow.RecordEvent(log.ToString());}
                var success=true;for(int i=0;i<pieces.Count;i++){var piece=pieces[i];var brush=piece.Item3 as SolidColorBrush;uint color=baseColor;if(brush!=null){var c=brush.Color;color=(uint)(c.R|(c.G<<8)|(c.B<<16));}SetTextColor(dc,color);success=render(piece.Item1,piece.Item2,x+positions[i].X,y+positions[i].Y,i==0)&&success;}
                return success;
            }catch(Exception){return render(0,count,x,y,true);}
            finally{if(baseColor!=0xffffffff)SetTextColor(dc,baseColor);}
        }
        [StructLayout(LayoutKind.Sequential)]private struct Size {internal int X,Y;}
        private string Decode(IntPtr dc,IntPtr text,int count)
        {
            var font=GetCurrentObject(dc,6);Dictionary<ushort,char> map;
            if(!glyphs.TryGetValue(font,out map)){
                if(glyphs.Count>=64)glyphs.Clear();map=new Dictionary<ushort,char>();
                var ascii=new StringBuilder();for(var c=32;c<127;c++)ascii.Append((char)c);var characters=ascii.ToString();var indices=new ushort[characters.Length];
                if(GetGlyphIndicesW(dc,characters,characters.Length,indices,1)==0xffffffff)return null;
                for(var i=0;i<indices.Length;i++)if(indices[i]!=0xffff&&!map.ContainsKey(indices[i]))map.Add(indices[i],characters[i]);glyphs[font]=map;
            }
            var result=new StringBuilder(count);for(var i=0;i<count;i++){char character;if(!map.TryGetValue(unchecked((ushort)Marshal.ReadInt16(text,i*2)),out character))return null;result.Append(character);}return result.ToString();
        }
        public void Dispose()
        {
            disposed=true;foreach(var slot in slots){uint protection;if(Marshal.ReadIntPtr(slot.Address)==slot.Replacement&&VirtualProtect(slot.Address,(UIntPtr)IntPtr.Size,0x04,out protection)){try{Marshal.WriteIntPtr(slot.Address,slot.Original);}finally{uint ignored;VirtualProtect(slot.Address,(UIntPtr)IntPtr.Size,protection,out ignored);}}retired.Add(slot.Callback);}slots.Clear();foreach(var window in windows)if(IsWindow(window))InvalidateRect(window,IntPtr.Zero,false);windows.Clear();glyphs.Clear();
        }
        [UnmanagedFunctionPointer(CallingConvention.Winapi)][return:MarshalAs(UnmanagedType.Bool)]private delegate bool TextOut(IntPtr dc,int x,int y,IntPtr text,int count);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)][return:MarshalAs(UnmanagedType.Bool)]private delegate bool ExtTextOut(IntPtr dc,int x,int y,uint options,IntPtr rect,IntPtr text,uint count,IntPtr widths);
        [DllImport("kernel32.dll")]private static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll")]private static extern bool VirtualProtect(IntPtr address,UIntPtr size,uint protection,out uint old);
        [DllImport("user32.dll",EntryPoint="GetClassLongPtrW")]private static extern IntPtr GetClassLongPtr64(IntPtr window,int index);
        [DllImport("user32.dll",EntryPoint="GetClassLongW")]private static extern uint GetClassLong32(IntPtr window,int index);
        [DllImport("user32.dll")]private static extern IntPtr WindowFromDC(IntPtr dc);
        [DllImport("user32.dll")]private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")]private static extern bool InvalidateRect(IntPtr window,IntPtr rect,bool erase);
        [DllImport("gdi32.dll")]private static extern uint GetTextColor(IntPtr dc);
        [DllImport("gdi32.dll")]private static extern uint GetTextAlign(IntPtr dc);
        [DllImport("gdi32.dll")]private static extern bool GetTextExtentPointI(IntPtr dc,IntPtr glyphs,int count,out Size size);
        [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]private static extern bool GetTextExtentPoint32W(IntPtr dc,IntPtr text,int count,out Size size);
        [DllImport("gdi32.dll")]private static extern uint SetTextColor(IntPtr dc,uint color);
        [DllImport("gdi32.dll")]private static extern IntPtr GetCurrentObject(IntPtr dc,uint kind);
        [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]private static extern uint GetGlyphIndicesW(IntPtr dc,string text,int count,[Out]ushort[] glyphs,uint flags);
    }
}
