"""Single cancellation owner, scoped to a diagnostic token and one target PID."""
import ctypes,json,os,threading,time
from pathlib import Path
from ctypes import wintypes
class CaseCloser:
 def __init__(self,pid,token,role_out,enabled,deadline):self.pid=pid;self.token=token;self.out=Path(role_out);self.deadline=deadline;self.stop=threading.Event();self.thread=None;self.enabled=enabled
 def start(self):
  if self.enabled and os.name=='nt':self.thread=threading.Thread(target=self._run,daemon=True);self.thread.start()
 def close(self):
  self.stop.set()
  if self.thread:self.thread.join(.05)
 def _run(self):
  user=ctypes.WinDLL('user32',use_last_error=True);CB=ctypes.WINFUNCTYPE(wintypes.BOOL,wintypes.HWND,wintypes.LPARAM);user.EnumWindows.argtypes=[CB,wintypes.LPARAM];user.GetWindowThreadProcessId.argtypes=[wintypes.HWND,ctypes.POINTER(wintypes.DWORD)];user.GetWindowTextW.argtypes=[wintypes.HWND,wintypes.LPWSTR,ctypes.c_int];user.IsWindowVisible.argtypes=[wintypes.HWND];user.PostMessageW.argtypes=[wintypes.HWND,wintypes.UINT,wintypes.WPARAM,wintypes.LPARAM]
  began=time.perf_counter()
  while not self.stop.wait(.025) and time.perf_counter()<self.deadline:
   candidates=[];ready=self.out/'dialog-ready.json'
   try:
    data=json.loads(ready.read_text(encoding='utf-8-sig'))
    if data['token']==self.token and data['pid']==self.pid:candidates=[data['hwnd']]
   except (OSError,ValueError,KeyError):pass
   if not candidates and time.perf_counter()-began>.45:
    def collect(h,_):
     pid=wintypes.DWORD();user.GetWindowThreadProcessId(h,ctypes.byref(pid))
     if pid.value!=self.pid or not user.IsWindowVisible(h):return True
     b=ctypes.create_unicode_buffer(600);user.GetWindowTextW(h,b,600);title=b.value.lower()
     if any(x in title for x in ['microsoft visual studio','license','sign in','security','va_minihelp','welcome']):return True
     if any(x in title for x in ['visual assist options','codeassist','find symbol','members of','open file in solution','refactor','declaration','implementation','snippet','signature','rename','encapsulate','extract','using','include','brace','create file']):candidates.append(h)
     return True
    user.EnumWindows(CB(collect),0)
   if len(candidates)!=1:continue
   h=candidates[0];pid=wintypes.DWORD();user.GetWindowThreadProcessId(h,ctypes.byref(pid))
   if pid.value!=self.pid or self.stop.is_set():continue
   # Only this owner sends a single WM_CLOSE. The observer never invokes Cancel.
   if user.PostMessageW(h,0x10,0,0):
    (self.out/'cancel-owner.json').write_text(json.dumps(dict(pid=self.pid,token=self.token,hwnd=int(h),requests=1)),encoding='utf-8')
   return
