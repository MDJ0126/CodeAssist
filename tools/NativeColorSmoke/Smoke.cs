using CodeAssist.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media;
namespace CodeAssist.Extension {
 internal class ProjectIndexService {internal IndexStub Index=new IndexStub();}
 internal class IndexStub {internal List<SymbolStub> FindDefinitions(string name)=>name=="Fixture"?new List<SymbolStub>{new SymbolStub()}:new List<SymbolStub>();}
 internal class SymbolStub {internal SymbolKind Kind=SymbolKind.Type;}
 internal static class StartupUi {internal static Prefs Preferences=new Prefs();}
 internal class Prefs {internal bool ColoringEnabled=true,ColorObjectBrowser=true;}
 internal static class RecommendedColors {internal static Brush SymbolBrush(SymbolKind kind)=>Brushes.Gold;internal static Brush DisplayBrush(DisplayInk kind){switch(kind){case DisplayInk.Keyword:return Brushes.Blue;case DisplayInk.Function:return Brushes.DarkOrange;case DisplayInk.Variable:return Brushes.Olive;default:return Brushes.Gold;}}}
 internal static class WelcomeDocumentWindow {internal static void RecordEvent(string text){Console.WriteLine(text);}}
 class Smoke {
 [DllImport("NativeColorProbe.dll")]static extern IntPtr CreateProbe();
 [DllImport("NativeColorProbe.dll")]static extern uint DrawProbe(IntPtr window,int mode);
 [DllImport("NativeColorProbe.dll")]static extern uint GetSeen();
 [DllImport("NativeColorProbe.dll",CharSet=CharSet.Unicode)]static extern uint DrawNamed(IntPtr window,string text);
 [DllImport("NativeColorProbe.dll")]static extern void CloseProbe(IntPtr window);
 [DllImport("NativeColorProbe.dll")]static extern void ResetRecords();
 [DllImport("NativeColorProbe.dll")]static extern int RecordCount();
 [DllImport("NativeColorProbe.dll")]static extern uint RecordInk(int i);
 [DllImport("NativeColorProbe.dll")]static extern int RecordX(int i);
 [DllImport("NativeColorProbe.dll")]static extern int RecordY(int i);
 [DllImport("NativeColorProbe.dll")]static extern void DrawSignature(IntPtr window,int mode);
 [STAThread]static int Main(){int passed=0,total=0;var observed=CreateProbe();var other=CreateProbe();try{
 var adapter=new LiteTreeColors(new ProjectIndexService());adapter.Observe(observed);
 for(int mode=0;mode<3;mode++) {total++;var restored=DrawProbe(observed,mode);var seen=GetSeen();bool ok=seen==0x00d7ff&&restored==0x998877;if(ok)passed++;Console.WriteLine("mode="+mode+" seen="+seen.ToString("X")+" restored="+restored.ToString("X")+" pass="+ok);}
 for(int mode=0;mode<4;mode++){
  ResetRecords();DrawSignature(observed,mode);total++;bool ok=RecordCount()==7&&RecordInk(0)==0x008cff&&RecordInk(1)==0x998877&&RecordInk(2)==0xff0000&&RecordInk(3)==0x998877&&RecordInk(4)==0x008080&&RecordInk(5)==0x998877&&RecordInk(6)==0xff0000;
  if(ok&&mode>0)ok=RecordX(1)==38&&RecordX(2)==45&&RecordX(4)==87&&RecordX(6)==164;
  if(ok&&mode==3)ok=RecordY(1)==24&&RecordY(4)==31&&RecordY(6)==42;
  if(ok&&mode!=3)for(int i=0;i<RecordCount();i++)if(RecordY(i)!=20)ok=false;
  if(ok)passed++;Console.WriteLine("signature mode="+mode+" calls="+RecordCount()+" pass="+ok);
 }
 total++;DrawProbe(other,0);if(GetSeen()==0x998877)passed++;
 StartupUi.Preferences.ColorObjectBrowser=false;total++;DrawProbe(observed,0);if(GetSeen()==0x998877)passed++;
 StartupUi.Preferences.ColorObjectBrowser=true;StartupUi.Preferences.ColoringEnabled=false;total++;DrawProbe(observed,0);if(GetSeen()==0x998877)passed++;
 StartupUi.Preferences.ColoringEnabled=true;
 total++;DrawProbe(observed,1);if(GetSeen()==0x00d7ff)passed++;
 total++;DrawNamed(observed,"Unindexed");if(GetSeen()==0x998877)passed++;
 total++;DrawNamed(observed,"");if(GetSeen()==0x998877)passed++;
 total++;var worker=new System.Threading.Thread(()=>DrawProbe(observed,0));worker.Start();worker.Join();if(GetSeen()==0x998877)passed++;
 adapter.Observe(other);total++;DrawProbe(other,1);if(GetSeen()==0x00d7ff)passed++;
 adapter.Dispose();total++;DrawProbe(observed,0);if(GetSeen()==0x998877)passed++;
 Console.WriteLine("Adapter integration: "+passed+"/"+total+". Synthetic test control; not actual VA/VS verification.");return passed==total?0:1;
 }finally{CloseProbe(observed);CloseProbe(other);}}
 }
}
