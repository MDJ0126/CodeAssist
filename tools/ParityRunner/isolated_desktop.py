"""Launch only on an isolated Win32 desktop; never switches the input desktop.
No SendInput, SwitchDesktop, or foreground APIs are used.
"""
import ctypes as c, ctypes.wintypes as w, json, os, sys, subprocess, time
from pathlib import Path
u=c.WinDLL('user32',use_last_error=True); k=c.WinDLL('kernel32',use_last_error=True)
u.CreateDesktopW.argtypes=[w.LPCWSTR,w.LPCWSTR,c.c_void_p,w.DWORD,w.DWORD,c.c_void_p]; u.CreateDesktopW.restype=w.HANDLE
u.OpenInputDesktop.argtypes=[w.DWORD,w.BOOL,w.DWORD];u.OpenInputDesktop.restype=w.HANDLE
u.GetThreadDesktop.argtypes=[w.DWORD];u.GetThreadDesktop.restype=w.HANDLE
u.GetUserObjectInformationW.argtypes=[w.HANDLE,c.c_int,c.c_void_p,w.DWORD,c.POINTER(w.DWORD)]
u.CloseDesktop.argtypes=[w.HANDLE]; k.GetCurrentThreadId.restype=w.DWORD
u.GetForegroundWindow.restype=w.HWND
class SI(c.Structure):
 _fields_=[('cb',w.DWORD),('reserved',w.LPWSTR),('desktop',w.LPWSTR),('title',w.LPWSTR),('x',w.DWORD),('y',w.DWORD),('cx',w.DWORD),('cy',w.DWORD),('xc',w.DWORD),('yc',w.DWORD),('fill',w.DWORD),('flags',w.DWORD),('show',w.WORD),('extra',w.WORD),('data',c.c_void_p),('stdin',w.HANDLE),('stdout',w.HANDLE),('stderr',w.HANDLE)]
class PI(c.Structure):_fields_=[('process',w.HANDLE),('thread',w.HANDLE),('pid',w.DWORD),('tid',w.DWORD)]
k.CreateProcessW.argtypes=[w.LPCWSTR,w.LPWSTR,c.c_void_p,c.c_void_p,w.BOOL,w.DWORD,c.c_void_p,w.LPCWSTR,c.POINTER(SI),c.POINTER(PI)]
k.CloseHandle.argtypes=[w.HANDLE];k.WaitForSingleObject.argtypes=[w.HANDLE,w.DWORD]
def name(h):
 b=c.create_unicode_buffer(512); n=w.DWORD()
 if not u.GetUserObjectInformationW(h,2,b,c.sizeof(b),c.byref(n)):raise c.WinError(c.get_last_error())
 return b.value
def state():
 h=u.OpenInputDesktop(0,False,1)
 if not h:raise c.WinError(c.get_last_error())
 try:return {'inputDesktop':name(h),'foregroundHwnd':int(u.GetForegroundWindow() or 0)}
 finally:u.CloseDesktop(h)
def launch(desktop,args,wait=False):
 if not desktop.startswith('CodeAssistVerify-'):raise ValueError('Owned desktop prefix required')
 h=u.CreateDesktopW(desktop,None,None,0,0x1ff,None)
 if not h:raise c.WinError(c.get_last_error())
 try:
  if name(h)==state()['inputDesktop']:raise RuntimeError('Refusing input desktop')
  si=SI();si.cb=c.sizeof(si);si.desktop='winsta0\\'+desktop;si.flags=0x81;si.show=0
  pi=PI();cmd=c.create_unicode_buffer(subprocess.list2cmdline(args))
  if not k.CreateProcessW(None,cmd,None,None,False,0x08000000,None,os.getcwd(),c.byref(si),c.byref(pi)):raise c.WinError(c.get_last_error())
  try:
   print(json.dumps({'launchedPid':pi.pid,'desktop':desktop}),flush=True)
   if wait and k.WaitForSingleObject(pi.process,8000)!=0:raise TimeoutError('Isolation probe exceeded eight seconds')
   if not wait:k.WaitForSingleObject(pi.process,0xffffffff)
   code=w.DWORD();k.GetExitCodeProcess.argtypes=[w.HANDLE,c.POINTER(w.DWORD)];k.GetExitCodeProcess(pi.process,c.byref(code));print(json.dumps({'exitCode':code.value,'pid':pi.pid}),flush=True)
   return pi.pid
  finally:k.CloseHandle(pi.thread);k.CloseHandle(pi.process)
 finally:u.CloseDesktop(h)
def child(out):
 thread=name(u.GetThreadDesktop(k.GetCurrentThreadId()));before=state()
 if thread==before['inputDesktop']:raise RuntimeError('Worker is on input desktop')
 u.CreateWindowExW.restype=w.HWND
 u.CreateWindowExW.argtypes=[w.DWORD,w.LPCWSTR,w.LPCWSTR,w.DWORD,c.c_int,c.c_int,c.c_int,c.c_int,w.HWND,w.HMENU,w.HINSTANCE,c.c_void_p]
 hwnd=u.CreateWindowExW(0,'STATIC','Owned background isolation probe',0x10000000,10,10,250,80,None,None,None,None)
 if not hwnd:raise c.WinError(c.get_last_error())
 time.sleep(.25)
 after=state();u.DestroyWindow.argtypes=[w.HWND];u.DestroyWindow(hwnd)
 Path(out).write_text(json.dumps({'threadDesktop':thread,'before':before,'after':after,'isolated':thread!=after['inputDesktop'],'pid':os.getpid()},indent=2))
if __name__=='__main__':
 if sys.argv[1]=='child':child(sys.argv[2])
 elif sys.argv[1]=='probe':
  out=Path(sys.argv[2]).resolve();out.parent.mkdir(parents=True,exist_ok=True);before=state();pid=launch('CodeAssistVerify-probe',[sys.executable,__file__,'child',str(out)],True);after=state();v=json.loads(out.read_text());v.update(parentBefore=before,parentAfter=after,foregroundUnchanged=before==after);out.write_text(json.dumps(v,indent=2));print(json.dumps(v))
 else:print(json.dumps({'pid':launch(sys.argv[1],sys.argv[2:]),'after':state()}))
