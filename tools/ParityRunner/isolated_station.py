"""Private window-station probe. Never switches a desktop or writes WinSta0 clipboard."""
import ctypes as c,ctypes.wintypes as w,json,sys,uuid,subprocess
from pathlib import Path
from isolated_desktop import SI,PI,k,u,name
u.GetProcessWindowStation.restype=w.HANDLE
u.CreateWindowStationW.argtypes=[w.LPCWSTR,w.DWORD,w.DWORD,c.c_void_p];u.CreateWindowStationW.restype=w.HANDLE
u.SetProcessWindowStation.argtypes=[w.HANDLE];u.SetProcessWindowStation.restype=w.BOOL
u.SetThreadDesktop.argtypes=[w.HANDLE];u.SetThreadDesktop.restype=w.BOOL
u.CloseWindowStation.argtypes=[w.HANDLE];u.CloseWindowStation.restype=w.BOOL
u.GetClipboardSequenceNumber.restype=w.DWORD
u.OpenClipboard.argtypes=[w.HWND];u.OpenClipboard.restype=w.BOOL
u.EmptyClipboard.restype=w.BOOL;u.CloseClipboard.restype=w.BOOL
u.SetClipboardData.argtypes=[w.UINT,w.HANDLE];u.SetClipboardData.restype=w.HANDLE
u.GetClipboardData.argtypes=[w.UINT];u.GetClipboardData.restype=w.HANDLE
k.GlobalAlloc.argtypes=[w.UINT,c.c_size_t];k.GlobalAlloc.restype=w.HANDLE
k.GlobalLock.argtypes=[w.HANDLE];k.GlobalLock.restype=c.c_void_p
k.GlobalUnlock.argtypes=[w.HANDLE];k.GlobalUnlock.restype=w.BOOL
u.CreateWindowExW.argtypes=[w.DWORD,w.LPCWSTR,w.LPCWSTR,w.DWORD,c.c_int,c.c_int,c.c_int,c.c_int,w.HWND,w.HMENU,w.HINSTANCE,c.c_void_p];u.CreateWindowExW.restype=w.HWND
u.DestroyWindow.argtypes=[w.HWND];u.DestroyWindow.restype=w.BOOL
VALUE='CodeAssist-owned-station-probe-103'
def station():return name(u.GetProcessWindowStation())
def guard_private():
 s=station()
 if not s.startswith('CodeAssistVerifyClip-') or s.lower()=='winsta0':raise RuntimeError('Refusing interactive station clipboard')
 return s

def child(out,clipboard):
 s=guard_private();r=dict(station=s,desktop=name(u.GetThreadDesktop(k.GetCurrentThreadId())),clipboardRead=False)
 if clipboard:
  if not u.OpenClipboard(None):raise c.WinError(c.get_last_error())
  try:
   data=u.GetClipboardData(13);assert data
   ptr=k.GlobalLock(data);assert ptr
   try:r['privateValue']=c.wstring_at(ptr, len(VALUE));assert r['privateValue']==VALUE
   finally:k.GlobalUnlock(data)
   r['clipboardRead']=True
  finally:u.CloseClipboard()
 Path(out).write_text(json.dumps(r,indent=2),encoding='utf-8')

def probe(out,clipboard=False):
 originalStation=u.GetProcessWindowStation();originalDesktop=u.GetThreadDesktop(k.GetCurrentThreadId());before=dict(station=station(),clipboardSequence=u.GetClipboardSequenceNumber());assert before['station'].lower()=='winsta0'
 token='CodeAssistVerifyClip-'+uuid.uuid4().hex[:10];ws=u.CreateWindowStationW(token,0,0x000f,None)
 if not ws:raise c.WinError(c.get_last_error())
 desktop=None;hwnd=None;report={};childFile=Path(out).with_suffix('.child.json')
 try:
  if not u.SetProcessWindowStation(ws):raise c.WinError(c.get_last_error())
  desktop=u.CreateDesktopW('CodeAssistVerifyClipboard',None,None,0,0x1ff,None)
  if not desktop or not u.SetThreadDesktop(desktop):raise c.WinError(c.get_last_error())
  assert guard_private()==token
  report=dict(privateStation=token,privateDesktop=name(u.GetThreadDesktop(k.GetCurrentThreadId())),clipboardWrite=False,neverSwitchesDesktop=True)
  if clipboard:
   hwnd=u.CreateWindowExW(0,'STATIC','Owned isolated clipboard probe',0,0,0,16,16,None,None,None,None)
   if not hwnd:raise c.WinError(c.get_last_error())
   if not u.OpenClipboard(hwnd):raise c.WinError(c.get_last_error())
   try:
    assert guard_private()==token
    if not u.EmptyClipboard():raise c.WinError(c.get_last_error())
    payload=(VALUE+'\0').encode('utf-16-le');memory=k.GlobalAlloc(2,len(payload));assert memory;ptr=k.GlobalLock(memory);assert ptr;c.memmove(ptr,payload,len(payload));k.GlobalUnlock(memory)
    if not u.SetClipboardData(13,memory):raise c.WinError(c.get_last_error())
    report['clipboardWrite']=True
   finally:u.CloseClipboard()
  si=SI();si.cb=c.sizeof(si);si.desktop=token+'\\CodeAssistVerifyClipboard';si.flags=0x81;si.show=0;pi=PI();args=[sys.executable,str(Path(__file__).resolve()),'child',str(childFile),str(int(clipboard))];cmd=c.create_unicode_buffer(subprocess.list2cmdline(args))
  if not k.CreateProcessW(None,cmd,None,None,False,0x08000000,None,str(Path.cwd()),c.byref(si),c.byref(pi)):raise c.WinError(c.get_last_error())
  try:
   if k.WaitForSingleObject(pi.process,10000)!=0:
    k.TerminateProcess.argtypes=[w.HANDLE,w.UINT];k.TerminateProcess(pi.process,98);raise TimeoutError('Owned station child timeout')
   code=w.DWORD();k.GetExitCodeProcess(pi.process,c.byref(code));assert code.value==0,code.value
  finally:k.CloseHandle(pi.thread);k.CloseHandle(pi.process)
  report['child']=json.loads(childFile.read_text(encoding='utf-8'));assert report['child']['station']==token
 finally:
  if hwnd:u.DestroyWindow(hwnd)
  if not u.SetProcessWindowStation(originalStation):raise c.WinError(c.get_last_error())
  if not u.SetThreadDesktop(originalDesktop):raise c.WinError(c.get_last_error())
  if desktop:u.CloseDesktop(desktop)
  u.CloseWindowStation(ws)
  report['callerBefore']=before;report['callerAfter']=dict(station=station(),clipboardSequence=u.GetClipboardSequenceNumber());report['interactiveClipboardSequenceUnchanged']=report['callerBefore']['clipboardSequence']==report['callerAfter']['clipboardSequence'];Path(out).write_text(json.dumps(report,indent=2),encoding='utf-8')
 return report
if __name__=='__main__':
 if sys.argv[1]=='child':child(sys.argv[2],bool(int(sys.argv[3])))
 else:print(json.dumps(probe(Path(sys.argv[2]),'--clipboard' in sys.argv)))
