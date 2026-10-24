import ctypes as c,ctypes.wintypes as w,json,collections
from pathlib import Path
p=json.loads(Path('.local/parity-runs/background-103/native-thread-start-input.json').read_text(encoding='utf-8-sig'));k=c.WinDLL('kernel32');n=c.WinDLL('ntdll');k.OpenThread.argtypes=[w.DWORD,w.BOOL,w.DWORD];k.OpenThread.restype=w.HANDLE;k.CloseHandle.argtypes=[w.HANDLE];n.NtQueryInformationThread.argtypes=[w.HANDLE,w.DWORD,c.c_void_p,w.ULONG,c.c_void_p];n.NtQueryInformationThread.restype=c.c_long;rows=[]
for t in p['threads']:
 h=k.OpenThread(0x0040,False,t['id'])
 if not h:continue
 try:
  address=c.c_void_p();r=n.NtQueryInformationThread(h,9,c.byref(address),c.sizeof(address),None)
  if r==0:
   v=address.value or 0;mod=next((m for m in p['modules'] if m['base']<=v<m['base']+m['size']),None);rows.append(dict(t,address=v,module=mod['name'] if mod else None,offset=hex(v-mod['base']) if mod else None))
 finally:k.CloseHandle(h)
out=Path('docs/validation/background-103/VA.parse-concurrency.baseline-thread-starts.json');out.write_text(json.dumps(rows,indent=2));print(collections.Counter((r['module'],r['offset']) for r in rows));print([r for r in rows if (r.get('module') or '').startswith('VA')])
