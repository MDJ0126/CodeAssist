import sys,json,time,subprocess,ctypes as c,ctypes.wintypes as w
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import launch
P=Path('.local/parity-runs/remaining-105');journal={}
def save(name,v):(P/name).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def opt(op,**kw):
 for n in range(30):
  v,_=module('VA',op,windowTitle='Visual Assist Options',**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
def count(label):
 pid=int(json.loads((RUN/'hosts.json').read_text())['VA']['pid'])
 ps='$p=Get-Process -Id '+str(pid)+';@{threads=@($p.Threads|ForEach-Object {$_.Id});modules=@($p.Modules|ForEach-Object {@{name=$_.ModuleName;base=$_.BaseAddress.ToInt64();size=$_.ModuleMemorySize}})}|ConvertTo-Json -Depth 4'
 a=json.loads(subprocess.check_output(['powershell.exe','-NoProfile','-Command',ps],creationflags=0x08000000));k=c.WinDLL('kernel32');nt=c.WinDLL('ntdll');k.OpenThread.argtypes=[w.DWORD,w.BOOL,w.DWORD];k.OpenThread.restype=w.HANDLE;k.CloseHandle.argtypes=[w.HANDLE];nt.NtQueryInformationThread.argtypes=[w.HANDLE,w.DWORD,c.c_void_p,w.ULONG,c.c_void_p];rows=[]
 for tid in a['threads']:
  h=k.OpenThread(0x40,False,tid)
  if not h:continue
  try:
   address=c.c_void_p();r=nt.NtQueryInformationThread(h,9,c.byref(address),c.sizeof(address),None)
   if r==0:
    v=address.value or 0;m=next((m for m in a['modules'] if m['base']<=v<m['base']+m['size']),None)
    if m and m['name'].lower()=='va_x64.dll':rows.append(dict(tid=tid,module=m['name'],offset=hex(v-m['base'])))
  finally:k.CloseHandle(h)
 v=dict(pid=pid,nativeThreads=rows);save('VA.parse-pool.'+label+'.json',v);print(label,rows,flush=True);return v
def setting(value):
 module('VA','cancel-menus',windowTitle='');module('VA','open-options',windowTitle='');v=opt('options-read',page='Performance');old=[str(x['value']) for x in v['result']['controls'] if x['type']=='TextBox' and str(x.get('value','')).isdigit()];assert len(old)==1,old
 if 'original' not in journal:journal['original']=old[0];save('parse-pool.transaction.json',journal)
 opt('options-edit',oldText=old[0],newText=str(value),setting='ParseConcurrency');opt('options-button',button='Save')
def restart(label):
 pid=json.loads((RUN/'hosts.json').read_text())['VA']['pid'];out=P/(label+'.quit.json');q=launch(['quit',pid,out]);q.wait(timeout=20)
 for n in range(80):
  chk=subprocess.run(['powershell.exe','-NoProfile','-Command','if(Get-Process -Id '+str(pid)+' -ErrorAction SilentlyContinue){exit 9}'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=0x08000000)
  if chk.returncode!=9:break
  time.sleep(.1)
 subprocess.run([sys.executable,'tools/ParityRunner/background_hosts.py','--roles','VA','--skip-configuration'],check=True,timeout=90,creationflags=0x08000000)
 pid=json.loads((RUN/'hosts.json').read_text())['VA']['pid']
 for mode in ['show-owned','load-capture-package']:
  out=P/(label+'.'+mode+'.json')
  for attempt in range(35):
   q=launch([mode,pid,out]);q.wait(timeout=20);ready=json.loads(out.read_text(encoding='utf-8-sig'))
   if ready.get('pid')==pid and (ready.get('visible') is True if mode=='show-owned' else ready.get('loaded') is True):break
   time.sleep(.5)
  else:raise RuntimeError('Owned host never became ready: '+mode)
 time.sleep(1)
if __name__=='__main__':
 try:
  count('before');setting(1);restart('one');count('one')
 finally:
  if 'original' in journal:
   setting(int(journal['original']));restart('restored');count('restored');journal['restored']=True;save('parse-pool.transaction.json',journal)
