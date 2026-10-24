"""Serialized functional probes on a non-input desktop; never injects input."""
import json,sys,subprocess,time,ctypes,os
from pathlib import Path
from isolated_desktop import state,name,u,k
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'docs/validation/background-103';WORKER=ROOT/'tools/ShortcutProbe/BackgroundWorker4.exe'
def write(p,v):Path(p).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def execute(args,path,timeout=18):
 p=subprocess.Popen([str(WORKER),*args],creationflags=0x08000000,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 try:p.wait(timeout=timeout)
 except subprocess.TimeoutExpired:
  p.kill();p.wait();return {'status':'diagnostic-timeout','workerPid':p.pid,'hostNotKilled':True}
 return json.loads(path.read_text(encoding='utf-8-sig')) if path.exists() else {'status':'missing-result','workerExitCode':p.returncode}
def main():
 desktop=name(u.GetThreadDesktop(k.GetCurrentThreadId()))
 if desktop==state()['inputDesktop'] or not desktop.startswith('CodeAssistVerify-'):raise RuntimeError('Refusing user desktop')
 targets={'VA':41936,'CA':43684};cases=json.loads((OUT/'cases.json').read_text())['cases'];rows=[]
 for case in cases:
  if case['id'] in ('context-next','goto-implementation','braces-add','sort-lines','extract-method','create-implementation'):continue
  row={'id':case['id'],'feature':case['feature'],'roles':{}}
  for role,pid in targets.items():
   result=OUT/(role+'.'+case['id']+'.json');spec=OUT/(case['id']+'.case.json')
   driver=None
   if case['id']=='sort-lines':
    driver_spec=OUT/(role+'.sort-driver.json');write(driver_spec,{'title':'Sort' if role=='VA' else 'CodeAssist','button':'OK' if role=='VA' else '확인'})
    driver=subprocess.Popen([str(WORKER),'drive',str(pid),str(OUT/(role+'.sort-driver-result.json')),str(driver_spec)],creationflags=0x08000000)
   row['roles'][role]=execute(['case',str(pid),str(result),str(spec),role],result)
   if driver:
    try:driver.wait(timeout=1)
    except subprocess.TimeoutExpired:driver.kill();driver.wait()
   if row['roles'][role].get('status') not in ('complete','error'):
    row['roles'][role]['windows']=execute(['windows',str(pid),str(OUT/(role+'.'+case['id']+'.windows.json'))],OUT/(role+'.'+case['id']+'.windows.json'),3)
    write(OUT/'batch-status.json',{'desktop':desktop,'processed':len(rows),'active':case['id'],'haltedForModal':role,'rows':rows+[row]});return 2
  rows.append(row);write(OUT/'batch-status.json',{'desktop':desktop,'processed':len(rows),'active':None,'rows':rows})
 return 0
if __name__=='__main__':raise SystemExit(main())
