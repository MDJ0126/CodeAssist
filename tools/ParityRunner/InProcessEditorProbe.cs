using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Web.Script.Serialization;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
namespace CodeAssist.ParityRunner {
internal static class InProcessEditorProbe {
static object Snapshot(IWpfTextView v,string file){var s=v.Selection.StreamSelectionSpan.SnapshotSpan;return new{file,text=v.TextSnapshot.GetText(),caret=v.Caret.Position.BufferPosition.Position,selectionStart=s.Start.Position,selectionLength=s.Length,selectedText=s.GetText()};}
static IEnumerable<DependencyObject> Walk(DependencyObject p){yield return p;for(int n=0;n<VisualTreeHelper.GetChildrenCount(p);n++)foreach(var c in Walk(VisualTreeHelper.GetChild(p,n)))yield return c;}
internal static async Task RunAsync(AsyncPackage package,string root,string role,Dictionary<string,object> item,string id){
 var json=new JavaScriptSerializer{MaxJsonLength=16000000};var output=Path.Combine(root,"background-capture",role+"-"+id+"-probe.json");object before=null,after=null;string error=null;object styles=null,markers=null,prefs=null;var restores=new Dictionary<PropertyInfo,object>();object preferences=null;
 try {
 await package.JoinableTaskFactory.SwitchToMainThreadAsync();
 var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;var model=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
 string owned=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,file=dte?.ActiveDocument?.FullName;
 if(file==null||!Path.GetFullPath(file).StartsWith(owned,StringComparison.OrdinalIgnoreCase)||item.ContainsKey("expectedFile")&&!string.Equals(file,(string)item["expectedFile"],StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Refusing non-owned or changed active document");
 IVsTextView adapter;if(manager==null||model==null||manager.GetActiveView(0,null,out adapter)<0)throw new InvalidOperationException("No active text view");var view=model.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(adapter);if(view==null||view.IsClosed)throw new InvalidOperationException("No live WPF view");
 if(role=="CA"){var asm=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="CodeAssist.Extension");preferences=asm.GetType("CodeAssist.Extension.StartupUi").GetProperty("Preferences",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);prefs=preferences.GetType().GetProperties().Where(p=>p.PropertyType==typeof(bool)).ToDictionary(p=>p.Name,p=>p.GetValue(preferences));
 if(item.ContainsKey("preferenceOverrides")){foreach(var kv in (Dictionary<string,object>)item["preferenceOverrides"]){var prop=preferences.GetType().GetProperty(kv.Key);if(prop==null||prop.PropertyType!=typeof(bool)||!new[]{"LocalSymbolsBold","SystemSymbolsItalic","RepairTypeCase","AcronymCompletionEnabled","ShorthandCompletionEnabled","SnippetShortcutsEnabled","ExtendCommentsEnabled","PairCompletionEnabled","ColoringEnabled"}.Contains(kv.Key))throw new InvalidOperationException("Preference not allowlisted");restores[prop]=prop.GetValue(preferences);prop.SetValue(preferences,Convert.ToBoolean(kv.Value));}}}
 if(item.ContainsKey("caret")){int caret=Convert.ToInt32(item["caret"]);view.Selection.Clear();view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot,caret));}
 before=Snapshot(view,file);
 if((string)item["kind"]=="input"){
 string action=(string)item["action"];VSConstants.VSStd2KCmdID key;char? ch=null;
 if(action=="typechar"){string value=(string)item["character"];if(value.Length!=1)throw new InvalidOperationException("One character required");key=VSConstants.VSStd2KCmdID.TYPECHAR;ch=value[0];}
 else if(action=="return")key=VSConstants.VSStd2KCmdID.RETURN;else if(action=="tab")key=VSConstants.VSStd2KCmdID.TAB;else if(action=="backspace")key=VSConstants.VSStd2KCmdID.BACKSPACE;else if(action=="complete")key=VSConstants.VSStd2KCmdID.COMPLETEWORD;else throw new InvalidOperationException("Input action not allowlisted");
 var group=VSConstants.VSStd2K;IntPtr variant=IntPtr.Zero;try{if(ch.HasValue){variant=Marshal.AllocCoTaskMem(32);Marshal.GetNativeVariantForObject((ushort)ch.Value,variant);}ErrorHandler.ThrowOnFailure(((Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget)adapter).Exec(ref group,(uint)key,0,variant,IntPtr.Zero));}finally{if(variant!=IntPtr.Zero)Marshal.FreeCoTaskMem(variant);}
 await Task.Delay(500);await package.JoinableTaskFactory.SwitchToMainThreadAsync();}
 if((string)item["kind"]=="tree-navigate"){
 if(role!="CA")throw new InvalidOperationException("WPF pane navigation is CA-only");string target=(string)item["target"],label=(string)item["label"];if(target!="outline"&&target!="workspace")throw new InvalidOperationException("Pane not allowlisted");var source=System.Windows.Interop.HwndSource.FromHwnd((IntPtr)dte.MainWindow.HWnd);var pane=Walk(source.RootVisual).First(p=>p.GetType().FullName=="CodeAssist.Extension.SymbolPane"&&(bool)p.GetType().GetField("workspace",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p)==(target=="workspace"));var tree=(TreeView)pane.GetType().GetField("tree",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pane);Func<ItemCollection,IEnumerable<TreeViewItem>> nodes=null;nodes=items=>items.Cast<object>().OfType<TreeViewItem>().SelectMany(t=>new[]{t}.Concat(nodes(t.Items)));var selected=nodes(tree.Items).First(t=>t.Header!=null&&t.Header.ToString().Contains(label));selected.IsSelected=true;tree.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=Control.MouseDoubleClickEvent,Source=selected});await Task.Delay(200);await package.JoinableTaskFactory.SwitchToMainThreadAsync();}
 after=Snapshot(view,dte.ActiveDocument.FullName);
 if((string)item["kind"]=="classification"){
 var span=new SnapshotSpan(view.TextSnapshot,0,view.TextSnapshot.Length);var classifier=model.GetService<IClassifierAggregatorService>().GetClassifier(view.TextBuffer);var formats=model.GetService<IClassificationFormatMapService>().GetClassificationFormatMap(view);
 styles=classifier.GetClassificationSpans(span).Select(s=>{var props=formats.GetTextProperties(s.ClassificationType);return new{start=s.Span.Start.Position,length=s.Span.Length,text=s.Span.GetText(),classification=s.ClassificationType.Classification,foreground=(props.ForegroundBrush as SolidColorBrush)?.Color.ToString(),bold=props.Typeface.Weight.ToOpenTypeWeight(),italic=props.Typeface.Style.ToString()};}).ToArray();
 using(var tags=model.GetService<IViewTagAggregatorFactoryService>().CreateTagAggregator<TextMarkerTag>(view))markers=tags.GetTags(span).Select(t=>new{type=t.Tag.Type,spans=t.Span.GetSpans(view.TextSnapshot).Select(s=>new{start=s.Start.Position,length=s.Length,text=s.GetText()}).ToArray()}).ToArray();
 }
 }catch(Exception ex){error=ex.ToString();}
 finally{await package.JoinableTaskFactory.SwitchToMainThreadAsync();foreach(var restore in restores)restore.Key.SetValue(preferences,restore.Value);}
 File.WriteAllText(output,json.Serialize(new{id,role,pid=System.Diagnostics.Process.GetCurrentProcess().Id,status=error==null?"complete":"error",before,after,styles,markers,preferences=prefs,temporaryPreferenceOverrides=item.ContainsKey("preferenceOverrides")?item["preferenceOverrides"]:null,restored=restores.Count,error,route="In-process VS editor command chain; no physical input"}));
}
}
}
