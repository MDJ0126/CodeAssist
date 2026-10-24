using System;using System.Collections.Generic;using System.Linq;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Controls.Primitives;using System.Windows.Interop;using System.Windows.Media;using System.Windows.Threading;using System.IO;using System.Web.Script.Serialization;
namespace CodeAssist.ParityRunner {
internal static class InProcessOptionsProbe {
 static IEnumerable<DependencyObject> Walk(DependencyObject root){var seen=new HashSet<DependencyObject>();var todo=new Stack<DependencyObject>();todo.Push(root);while(todo.Count>0){var n=todo.Pop();if(!seen.Add(n))continue;yield return n;try{for(int i=0;i<VisualTreeHelper.GetChildrenCount(n);i++)todo.Push(VisualTreeHelper.GetChild(n,i));}catch{}foreach(var child in LogicalTreeHelper.GetChildren(n)){var d=child as DependencyObject;if(d!=null)todo.Push(d);}}}
 static string Label(object value){if(value==null)return "";var text=value as string;if(text!=null)return text;var block=value as TextBlock;if(block!=null)return block.Text;var node=value as DependencyObject;if(node!=null)return string.Join(" ",Walk(node).OfType<TextBlock>().Select(b=>b.Text));return value.ToString();}
 static string TreeLabel(TreeViewItem item){var presenter=Walk(item).OfType<ContentPresenter>().FirstOrDefault(p=>ReferenceEquals(p.Content,item.Header));if(presenter!=null){var text=Label(presenter);if(!string.IsNullOrWhiteSpace(text))return text.Trim();}if(item.Header!=null){foreach(var key in new[]{"DisplayName","Name","Title","Text","Header","PageName"}){var prop=item.Header.GetType().GetProperty(key);if(prop!=null&&prop.PropertyType==typeof(string)){var text=prop.GetValue(item.Header,null) as string;if(!string.IsNullOrEmpty(text))return text;}}}return Label(item.Header);}
 internal static Task<object> Run(string root,string role,Case item){var done=new TaskCompletionSource<object>();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(70)};var started=DateTime.UtcNow;Window target=null;TreeViewItem selected=null;int stage=0;var seen=new List<string>();
 timer.Tick+=(s,e)=>{try{
  if((DateTime.UtcNow-started).TotalSeconds>8){timer.Stop();done.TrySetResult(new {status="inprocess-options-timeout",requestedPage=item.page,treeHeaders=seen});return;}
  if(target==null){foreach(HwndSource source in PresentationSource.CurrentSources.OfType<HwndSource>()){var w=source.RootVisual as Window;if(w!=null&&w.Title=="Visual Assist Options"&&w.IsVisible){target=w;break;}}if(target==null)return;}
  if(stage==0){foreach(var tree in Walk(target).OfType<TreeViewItem>().ToArray())tree.IsExpanded=true;target.UpdateLayout();var trees=Walk(target).OfType<TreeViewItem>().ToArray();seen=trees.Select(t=>TreeLabel(t)).Distinct().ToList();selected=trees.FirstOrDefault(t=>TreeLabel(t).Equals(item.page,StringComparison.OrdinalIgnoreCase));if(selected!=null){selected.IsSelected=true;selected.BringIntoView();}stage=1;return;}
  var controls=new List<object>();foreach(var n in Walk(target)){var element=n as UIElement;if(element!=null&&!element.IsVisible)continue;
   var toggle=n as ToggleButton;if(toggle!=null){controls.Add(new{name=Label(toggle.Content),type=n.GetType().FullName,value=toggle.IsChecked});continue;}
   var text=n as TextBlock;if(text!=null&&!string.IsNullOrWhiteSpace(text.Text)){controls.Add(new{name=text.Text,type="TextBlock",value=(string)null});continue;}
   var edit=n as TextBox;if(edit!=null){controls.Add(new{name=Label(edit.Tag),type="TextBox",value=edit.Text});continue;}
   var combo=n as ComboBox;if(combo!=null)controls.Add(new{name=Label(combo.Tag),type="ComboBox",value=Label(combo.SelectedItem)});
  }
  var headings=Walk(target).OfType<TextBlock>().Where(t=>t.IsVisible&&t.FontSize>=16).Select(t=>t.Text.Trim()).Where(t=>!string.IsNullOrWhiteSpace(t)).Distinct().ToArray();
  bool titleMatches=headings.Any(t=>t.Equals(item.page,StringComparison.OrdinalIgnoreCase)||t.EndsWith("| "+item.page,StringComparison.OrdinalIgnoreCase));
  var current=Walk(target).OfType<TreeViewItem>().FirstOrDefault(t=>t.IsSelected&&TreeLabel(t).Equals(item.page,StringComparison.OrdinalIgnoreCase));
  bool verified=selected!=null&&titleMatches;var handle=new WindowInteropHelper(target).Handle;
  ScreenCheckpoint.Capture(root,role,item.id,"dialog",handle).GetAwaiter().GetResult();
  var result=new {status="dialog-observed-and-cancelled",title=target.Title,pageSelected=verified,requestedPage=item.page,selectedHeader=current==null?null:TreeLabel(current),visibleHeadings=headings,treeHeaders=seen,controls,observationMethod="in-process WPF visual/logical tree"};
  timer.Stop();File.WriteAllText(Path.Combine(root,role+"-"+item.id+".wpf.json"),new JavaScriptSerializer{MaxJsonLength=4*1024*1024}.Serialize(result));target.Close();done.TrySetResult(result);
 }catch(Exception error){timer.Stop();try{target?.Close();}catch{}done.TrySetResult(new {status="inprocess-options-error",error=error.ToString(),treeHeaders=seen});}};
 timer.Start();return done.Task;}
}
}