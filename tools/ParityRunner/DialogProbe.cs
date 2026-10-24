using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using System.Text;

namespace CodeAssist.ParityRunner
{
    // Test-only dialog adapter, confined to this explicitly launched fixture host.
    // Reads a named page and cancels; never submits edits, file pickers or network actions.
    internal static class DialogProbe
    {
        internal static int TargetProcessId;
        internal static bool ObserveOnly;
        internal static Func<bool> StillCurrent;
        private delegate bool WindowVisitor(IntPtr hwnd,IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor callback,IntPtr data);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd,StringBuilder text,int size);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd,StringBuilder text,int size);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr wparam,IntPtr lparam);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hwnd,uint command);
        internal static Action<IntPtr> CaptureDialog=handle=>{};
        internal static Task<object> RunAsync(string page,int milliseconds,bool filePicker)
        {
            return Task.Run<object>(()=>
            {
                var clock=Stopwatch.StartNew();var pid=TargetProcessId;
                while(clock.ElapsedMilliseconds<milliseconds)
                {
                    if(StillCurrent!=null&&!StillCurrent())return new {status="stale-case"};
                    var windows=new List<AutomationElement>();
                    var handles=new Dictionary<AutomationElement,IntPtr>();
                    EnumWindows((hwnd,data)=>{uint owner;GetWindowThreadProcessId(hwnd,out owner);if(owner==(uint)pid&&IsWindowVisible(hwnd)){try{var element=AutomationElement.FromHandle(hwnd);windows.Add(element);handles[element]=hwnd;}catch{}}return true;},IntPtr.Zero);
                    foreach(AutomationElement window in windows)
                    {
                        try
                        {
                            if(page=="@surface" && clock.ElapsedMilliseconds>=900) {
                                var observedTitle=window.Current.Name??"";
                                if(observedTitle.Contains("Microsoft Visual Studio")) {
                                    CaptureDialog(handles[window]);
                                    return new {status="surface-captured-only",title=observedTitle,pageSelected=false,note="Main IDE surface only; no functional assertion"};
                                }
                            }
                            object pattern;
                            var hasWindowPattern=window.TryGetCurrentPattern(WindowPattern.Pattern,out pattern);
                            var wp=hasWindowPattern?(WindowPattern)pattern:null;
                            var title=window.Current.Name??"";
                            var handle=handles[window];
                            var className=new StringBuilder(128);GetClassName(handle,className,className.Capacity);
                            if(className.ToString()=="#32768")
                            {
                                if(StillCurrent!=null&&!StillCurrent())return new {status="stale-case"};
                                CaptureDialog(handle);
                                var labels=new List<string>();
                                foreach(AutomationElement entry in window.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.MenuItem)))labels.Add(entry.Current.Name);
                                if(ObserveOnly)return new {status="popup-observed-only",title="Native popup menu",pageSelected=false};
                                PostMessage(handle,0x001f,IntPtr.Zero,IntPtr.Zero);
                                var owner=GetWindow(handle,4);if(owner!=IntPtr.Zero)PostMessage(owner,0x001f,IntPtr.Zero,IntPtr.Zero);
                                PostMessage(handle,0x0100,(IntPtr)0x1b,IntPtr.Zero);
                                PostMessage(handle,0x0101,(IntPtr)0x1b,IntPtr.Zero);
                                return new {status="popup-menu-observed-and-cancelled",title="Native popup menu",pageSelected=true,controls=labels};
                            }
                            var nativeTitle=new StringBuilder(512);GetWindowText(handle,nativeTitle,nativeTitle.Capacity);
                            if(string.IsNullOrEmpty(title))title=nativeTitle.ToString();
                            var lowerTitle=title.ToLowerInvariant();
                            if(lowerTitle.Contains("microsoft visual studio")||lowerTitle.Contains("outline")||lowerTitle.Contains("workspace")||lowerTitle=="codeassist tools"||lowerTitle=="va view"||lowerTitle.Contains("welcome"))continue;
                            if((wp==null||!wp.Current.IsModal)&&!Allowed(title,filePicker))continue;
                            if(!Allowed(title,filePicker))return new {status="blocked-unexpected-dialog",title};
                            bool pageSelected=string.IsNullOrEmpty(page);
                            if(StillCurrent!=null&&!StillCurrent())return new {status="stale-case"};
                            if(!pageSelected)
                            {
                                foreach(AutomationElement node in window.FindAll(TreeScope.Descendants,Condition.TrueCondition))
                                {
                                    var name=node.Current.Name??"";
                                    if(!MatchesPage(name,page))continue;
                                    object selection;
                                    if(node.TryGetCurrentPattern(SelectionItemPattern.Pattern,out selection)){((SelectionItemPattern)selection).Select();pageSelected=true;break;}
                                }
                            }
                            Thread.Sleep(100);
                            var controls=new List<object>();AutomationElement cancel=null,okay=null;bool writable=false;
                            foreach(AutomationElement node in window.FindAll(TreeScope.Descendants,Condition.TrueCondition))
                            {
                                if(controls.Count>=600)break;
                                var name=node.Current.Name??"";
                                if(node.Current.IsPassword)continue;
                                string value=null;object vp;
                                if(node.TryGetCurrentPattern(ValuePattern.Pattern,out vp))value=((ValuePattern)vp).Current.Value;
                                if(node.Current.ControlType==ControlType.Edit&&vp!=null&&!((ValuePattern)vp).Current.IsReadOnly)writable=true;
                                object toggle;if(node.TryGetCurrentPattern(TogglePattern.Pattern,out toggle))value=((TogglePattern)toggle).Current.ToggleState.ToString();
                                if(value!=null&&value.Length>2000)value=value.Substring(0,2000);
                                controls.Add(new {name,type=node.Current.ControlType.ProgrammaticName,value});
                                var compact=name.Replace("&","").Trim();
                                if(node.Current.ControlType==ControlType.Button&&(compact=="Cancel"||compact=="취소"||compact.StartsWith("취소(",StringComparison.Ordinal)))cancel=node;
                                if(node.Current.ControlType==ControlType.Button&&(compact=="OK"||compact=="확인"))okay=node;
                            }
                            CaptureDialog(handle);
                            if(ObserveOnly)return new {status="dialog-observed-only",title,pageSelected,controls};
                            bool errorDialog=cancel==null&&!writable&&okay!=null;
                            if(StillCurrent!=null&&!StillCurrent())return new {status="stale-case"};
                            if(errorDialog)cancel=okay;
                            if(cancel==null)
                            {
                                if(wp!=null)wp.Close();else PostMessage(handle,0x0010,IntPtr.Zero,IntPtr.Zero);
                                return new {status="dialog-closed-without-cancel-control",title,pageSelected,controls};
                            }
                            object invoke;
                            if(!cancel.TryGetCurrentPattern(InvokePattern.Pattern,out invoke))return new {status="blocked-no-invoke",title};
                            ((InvokePattern)invoke).Invoke();
                            return new {status=errorDialog?"product-error-dialog":"dialog-observed-and-cancelled",title,pageSelected,controls};
                        }
                        catch(ElementNotAvailableException){ }
                        catch(InvalidOperationException){ }
                    }
                    Thread.Sleep(40);
                }
                return new {status="no-modal-observed"};
            });
        }
        private static bool MatchesPage(string actual,string requested)
        {
            if(actual.Equals(requested,StringComparison.OrdinalIgnoreCase))return true;
            try {
                var directory=new System.IO.DirectoryInfo(Environment.GetEnvironmentVariable("CODEASSIST_PARITY_ROOT"));
                while(directory!=null){var path=System.IO.Path.Combine(directory.FullName,"CodeAssist.Core","Localization","ui.xml");if(System.IO.File.Exists(path)){
                    var document=new System.Xml.XmlDocument();document.XmlResolver=null;
                    using(var reader=System.Xml.XmlReader.Create(path,new System.Xml.XmlReaderSettings{DtdProcessing=System.Xml.DtdProcessing.Prohibit,XmlResolver=null}))document.Load(reader);
                    foreach(System.Xml.XmlElement entry in document.SelectNodes("/translations/entry")){
                        bool key=entry.GetAttribute("source").Equals(requested,StringComparison.OrdinalIgnoreCase);
                        foreach(System.Xml.XmlElement text in entry.SelectNodes("text"))if(text.InnerText.Equals(requested,StringComparison.OrdinalIgnoreCase))key=true;
                        if(key){if(entry.GetAttribute("source").Equals(actual,StringComparison.OrdinalIgnoreCase))return true;foreach(System.Xml.XmlElement text in entry.SelectNodes("text"))if(text.InnerText.Equals(actual,StringComparison.OrdinalIgnoreCase))return true;}
                    }break;
                }directory=directory.Parent;}
            }catch(System.IO.IOException){}catch(System.Xml.XmlException){}
            return false;
        }
        private static bool Allowed(string title,bool filePicker)
        {
            var lower=title.ToLowerInvariant();
            if(new[]{"license","licens","checkout","sign in","security","인증","라이선스","로그인","보안"}.Any(lower.Contains))return false;
            return (filePicker&&(lower.Contains("open")||lower.Contains("save")||lower.Contains("file")||lower.Contains("print")||lower.Contains("열기")||lower.Contains("파일")||lower.Contains("인쇄")||lower.Contains("저장")))||lower.Contains("codeassist")||lower.Contains("visual assist")||lower.Contains("refactor")||
                   lower.Contains("rename")||lower.Contains("signature")||lower.Contains("extract")||
                   lower.Contains("encapsulate")||lower.Contains("snippet")||lower.Contains("member")||
                   lower.Contains("implementation")||lower.Contains("declaration")||lower.Contains("find symbol")||
                   lower.Contains("open file in solution")||lower.Contains("paste")||lower.Contains("sort")||
                   lower.Contains("goto")||lower.Contains("go to")||lower.Contains("corresponding")||
                   lower.Contains("이름 바꾸기")||lower.Contains("시그니처")||lower.Contains("추출")||lower.Contains("캡슐화")||lower.Contains("관련")||lower.Contains("심볼")||lower.Contains("파일 찾기")||
                   lower.Contains("address")||lower.Contains("pdb")||lower.Contains("memory")||lower.Contains("preview")||
                   lower.Contains("variable")||lower.Contains("forward")||lower.Contains("using")||lower.Contains("include")||lower.Contains("brace");
        }
    }
}
