import sys,json,hashlib,time,subprocess,os
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import RUN,module;from background_steps import OUT,save
sha=hashlib.sha256((RUN/'debug-artifacts/ParityDebug103.exe').read_bytes()).hexdigest();hosts=json.loads((OUT/'debuggee103.hosts.json').read_text());folder=Path(os.environ['APPDATA'])/'VisualAssist/DebuggerTools';backup={p:p.read_bytes() for p in folder.glob('*.vadbg')};backupDir=RUN/'step103-setting-backups';backupDir.mkdir(exist_ok=True)
for p,b in backup.items():(backupDir/p.name).write_bytes(b)
j={'nativeFiles':{p.name:hashlib.sha256(b).hexdigest() for p,b in backup.items()}}
def call(role,op,**kw):
 v,_=module(role,op,windowTitle='CodeAssist Options' if op.startswith('options-') else '',**kw)
 if v.get('error'):raise RuntimeError(v['error'])
 if op.startswith('options-') and (v.get('result') or {}).get('status')!='observed':raise RuntimeError(v)
 return v
try:
 v=call('CA','ca-step-runtime',enable=True);save('CA.step103.enabled-runtime.json',v);assert v['result']['enabled'] and v['result']['patterns']=='StepSkip103'
 call('VA','native-step-open');v=call('VA','native-step-toggle',value=True);save('VA.step103.enabled.json',v);assert v['result']['skipEnabled'] is True
 for role in ['VA','CA']:
  try:call(role,'owned-debug-stop',pid=hosts[role]['pid'],fileSha256=sha)
  except RuntimeError as error:
   if 'GetProcessById' not in str(error):raise
 time.sleep(.5)
 for role in ['VA','CA']:
  log=OUT/(role+'.step103.filtered-launch.log');f=log.open('wb');broker=subprocess.Popen([sys.executable,'tools/ParityRunner/isolated_desktop.py','CodeAssistVerify-runtime',str(RUN/'debug-artifacts/ParityDebug103.exe')],stdout=f,stderr=subprocess.STDOUT,creationflags=0x08000000);f.close();receipt=None
  for n in range(120):
   lines=log.read_text(errors='replace').splitlines();receipt=next((json.loads(x) for x in lines if x.startswith('{') and 'launchedPid' in x),None)
   if receipt:break
   time.sleep(.05)
  assert receipt;hosts[role]={'pid':receipt['launchedPid'],'broker':broker.pid,'desktop':receipt['desktop']};save('debuggee103.hosts.json',hosts)
  kw=dict(pid=hosts[role]['pid'],fileSha256=sha);save(role+'.step103.filtered-attach.json',call(role,'owned-debug-attach',**kw));call(role,'owned-debug-signal',**kw);time.sleep(.4)
  save(role+'.step103.filtered-start.json',call(role,'owned-debug-state',**kw));call(role,'owned-debug-step-over',**kw);time.sleep(.25);call(role,'owned-debug-step-into',**kw);time.sleep(.6);v=call(role,'owned-debug-state',**kw);save(role+'.step103.filtered-frame.json',v);print(role,v,flush=True)
finally:
 for role in ['VA','CA']:
  try:save(role+'.step103.stopped.json',call(role,'owned-debug-stop',pid=hosts[role]['pid'],fileSha256=sha))
  except Exception as e:j[role+'StopError']=str(e)
 try:
  v=call('CA','ca-step-runtime',restore=True);save('CA.step103.restored-runtime.json',v);j['caSettingsRestored']=v['result']['enabled'] is False and v['result']['patterns']=='std::*'
 except Exception as e:j['caRestoreError']=str(e)
 try:call('VA','native-step-open');call('VA','native-step-toggle',value=False)
 except Exception as e:j['nativeRuntimeRestoreError']=str(e)
 for p,b in backup.items():p.write_bytes(b)
 j['nativeFileBytesRestored']=all(p.read_bytes()==b for p,b in backup.items());save('step103.transaction.json',j);print(j,flush=True)
