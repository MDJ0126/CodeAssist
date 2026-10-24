import ctypes as c,ctypes.wintypes as w,json
from pathlib import Path
root=Path('.local/parity-runs/background-103');h=json.loads((root/'hosts.json').read_text());k=c.WinDLL('kernel32',use_last_error=True);k.OpenThread.argtypes=[w.DWORD,w.BOOL,w.DWORD];k.OpenThread.restype=w.HANDLE;k.GetThreadDescription.argtypes=[w.HANDLE,c.POINTER(w.LPWSTR)];k.GetThreadDescription.restype=c.c_long;k.LocalFree.argtypes=[w.HLOCAL];k.CloseHandle.argtypes=[w.HANDLE];out={}
for role in ['VA','CA']:
 p=json.loads((root/'thread-query-input.json').read_text(encoding='utf-8-sig'))[role];rows=[]
 for t in p:
  handle=k.OpenThread(0x0800,False,t['id'])
  if not handle:continue
  name=w.LPWSTR()
  try:
   if k.GetThreadDescription(handle,c.byref(name))==0:
    text=name.value or ''
    if text:rows.append(dict(id=t['id'],name=text,userTime=t['user_time'],kernelTime=t['system_time']))
  finally:
   if name:k.LocalFree(c.cast(name,w.HLOCAL))
   k.CloseHandle(handle)
 out[role]=rows;print(role,[r for r in rows if any(x in r['name'].lower() for x in ['va','pars','codeassist'])])
Path('docs/validation/background-103/parse-threads.actual-thread-descriptions.json').write_text(json.dumps(out,ensure_ascii=False,indent=2))
