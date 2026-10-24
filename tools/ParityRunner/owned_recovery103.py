import ctypes as c,ctypes.wintypes as w,sys,json
u=c.WinDLL('user32',use_last_error=True);pid=int(sys.argv[1]);u.GetThreadDesktop.argtypes=[w.DWORD];u.GetThreadDesktop.restype=w.HANDLE;u.GetUserObjectInformationW.argtypes=[w.HANDLE,c.c_int,c.c_void_p,w.DWORD,c.POINTER(w.DWORD)];u.GetWindowThreadProcessId.argtypes=[w.HWND,c.POINTER(w.DWORD)];u.GetWindowTextW.argtypes=[w.HWND,w.LPWSTR,c.c_int];u.GetDlgCtrlID.argtypes=[w.HWND];u.PostMessageW.argtypes=[w.HWND,w.UINT,w.WPARAM,w.LPARAM];cb=c.WINFUNCTYPE(w.BOOL,w.HWND,w.LPARAM);rows=[]
def text(h):
 b=c.create_unicode_buffer(2048);u.GetWindowTextW(h,b,len(b));return b.value
@cb
def window(h,z):
 p=w.DWORD();tid=u.GetWindowThreadProcessId(h,c.byref(p))
 if p.value!=pid or '복구된 파일' not in text(h):return True
 b=c.create_unicode_buffer(256);n=w.DWORD();assert u.GetUserObjectInformationW(u.GetThreadDesktop(tid),2,b,c.sizeof(b),c.byref(n)) and b.value=='CodeAssistVerify-runtime'
 items=[]
 @cb
 def child(ch,_):items.append((ch,text(ch)));return True
 u.EnumChildWindows(h,child,0)
 assert any('C:\\Projects\\CodeAssist\\.local\\parity-runs\\background-103\\VA\\' in t for _,t in items),items
 button=next(ch for ch,t in items if t=='복구하지 않음(&D)');control=u.GetDlgCtrlID(button);u.PostMessageW(h,0x111,control,button);rows.append({'pid':pid,'dialog':text(h),'button':text(button),'controlId':control,'desktop':b.value,'physicalInput':False});return True
u.EnumWindows(window,0);print(json.dumps(rows,ensure_ascii=False))
