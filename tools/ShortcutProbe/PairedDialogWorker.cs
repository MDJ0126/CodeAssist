using System;using System.IO;using System.Linq;using System.Threading;using System.Windows.Automation;using System.Diagnostics;
class PairedDialogWorker{
 static void Main(string[] a){int pid=int.Parse(a[0]);string id=a[1],log=a[2];var stop=DateTime.UtcNow.AddSeconds(8);bool acted=false;while(DateTime.UtcNow<stop){try{var root=AutomationElement.FromHandle(Process.GetProcessById(pid).MainWindowHandle);var windows=root.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Window));foreach(AutomationElement w in windows){var title=w.Current.Name;if(id=="sort-lines"&&(title.Contains("Sort")||title.Contains("정렬"))){Click(w,"OK");acted=true;}
 if(id=="create-implementation"&&title=="CodeAssist"){var e=w.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Edit));if(e!=null){((ValuePattern)e.GetCurrentPattern(ValuePattern.Pattern)).SetValue("Audit");Click(w,"Save");}}
 if((id=="create-implementation"||id=="toggle-braces")&&(title.Contains("Generate Method")||title.Contains("Braces"))){Click(w,"Apply changes");acted=true;}
 }if(acted){File.WriteAllText(log,"Expected generated-fixture dialog action applied");Thread.Sleep(350);return;}
 }catch(Exception ex){File.WriteAllText(log,ex.Message);}Thread.Sleep(100);}File.WriteAllText(log,"No expected dialog acted on");}
 static void Click(AutomationElement w,string name){var b=w.FindFirst(TreeScope.Descendants,new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button),new PropertyCondition(AutomationElement.NameProperty,name)));if(b==null)throw new Exception("Missing button "+name);((InvokePattern)b.GetCurrentPattern(InvokePattern.Pattern)).Invoke();}
}
