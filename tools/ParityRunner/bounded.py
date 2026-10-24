"""One 5-second budget per paired item. Only diagnostic children may be terminated."""
import argparse,hashlib,json,os,subprocess,sys,time,uuid
from pathlib import Path
from case_closer import CaseCloser
ROOT=Path(__file__).resolve().parents[2]
HARD_SECONDS=5.0
WORK_SECONDS=4.30 # Reserve time for terminating the diagnostic child and committing its result.
FIELDS=('text','caret','selectionStart','selectionLength','selectedText')
def write(path,data):
 path=Path(path);path.parent.mkdir(parents=True,exist_ok=True);tmp=path.with_suffix(path.suffix+'.tmp');tmp.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');replace_atomic(tmp,path)
def replace_atomic(tmp,path):
 for attempt in range(8):
  try:
   os.replace(tmp,path)
   return
  except PermissionError:
   if attempt==7:raise
   time.sleep(.005)
def compare(case,roles):
 if case.get('action')=='find-file':
  if not all(roles.get(r,{}).get('status')=='complete' for r in ['VA','CA']):return 'unverified','File-navigation action did not finish in both hosts'
  if case.get('expectNoMatch') and not all(roles[r].get('driver',{}).get('resultCount')==0 and roles[r].get('driver',{}).get('query')==case['query'] for r in ['VA','CA']):return 'different','Zero-result search assertion failed'
  return ('representative-match','Both expected full paths and contents actually opened') if all(roles[r].get('expectedFileOpen') is True for r in ['VA','CA']) else ('different','A submitted search did not open the expected file/content')
 if not case.get('compareFields') or tuple(case['compareFields'])!=FIELDS:return 'observed-only','No complete output assertion adapter; never a functional pass'
 def eq(a,b):return isinstance(a,dict) and isinstance(b,dict) and all(k in a and k in b and a[k]==b[k] for k in FIELDS)
 v,a=roles.get('VA'),roles.get('CA')
 if not v or not a or v.get('status')!='complete' or a.get('status')!='complete':return 'unverified','Both completed role outputs required'
 expected={'text':case['text'],'caret':case['start']+case['length'],'selectionStart':case['start'],'selectionLength':case['length'],'selectedText':case['text'][case['start']:case['start']+case['length']]}
 if not eq(v.get('before'),expected) or not eq(a.get('before'),expected):return 'unverified','Input setup mismatch'
 if not all(isinstance(x.get('after'),dict) and all(k in x['after'] for k in FIELDS) for x in [v,a]):return 'unverified','Output snapshot missing'
 if eq(v['after'],v['before']):return 'unverified','VA feature did not activate'
 if not eq(v['after'],a['after']):return 'different','Observed text/caret/selection differs'
 if case.get('verifyUndo'):
  if not v.get('undo') or not a.get('undo'):return 'unverified','Required Undo missing'
  if not eq(v['undo'],a['undo']):return 'different','Undo differs'
 return 'representative-match','Only this explicit representative behavior matches'
def run_case(case,targets,commands,out,quarantined=None,work_seconds=WORK_SECONDS):
 if work_seconds>WORK_SECONDS or work_seconds<=0:raise ValueError('Cannot exceed the work budget')
 started=time.perf_counter();deadline=started+work_seconds;token=uuid.uuid4().hex;out=Path(out);out.mkdir(parents=True,exist_ok=True);lease=out/'lease.json';write(lease,{'token':token,'active':True});roles={};quarantined=quarantined if quarantined is not None else set()
 for role in ('VA','CA'): # Serialize physical input and modal cancellation; no cross-role input races.
  pid=targets[role]
  if pid in quarantined:roles[role]={'status':'host-quarantined','pid':pid};continue
  remaining=deadline-time.perf_counter()
  if remaining<=0:roles[role]={'status':'item-budget-exhausted','pid':pid};continue
  role_out=out/role;role_out.mkdir(exist_ok=True);request=role_out/'request.json';result=role_out/'result.json'
  write(request,dict(case=case,role=role,pid=pid,token=token,lease=str(lease.resolve()),result=str(result.resolve()),root=str(role_out.resolve()),repo=str(ROOT)))
  proc=None;closer=CaseCloser(pid,token,role_out,case.get('action')=='dialog-probe',deadline)
  try:
   closer.start()
   # Structured argv only. A fresh child isolates a blocked COM/UIA/PrintWindow call.
   proc=subprocess.Popen([*commands[role],str(request.resolve())],stdin=subprocess.DEVNULL,stdout=subprocess.PIPE,stderr=subprocess.PIPE,creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
   stdout,stderr=proc.communicate(timeout=max(.001,deadline-time.perf_counter()))
   (role_out/'stdout.log').write_bytes(stdout);(role_out/'stderr.log').write_bytes(stderr)
   if proc.returncode!=0:
    if result.exists():
     failed=json.loads(result.read_text(encoding='utf-8-sig'))
     if failed.get('token')==token and failed.get('role')==role and failed.get('pid')==pid:
      roles[role]=dict(failed,status='action-error',exitCode=proc.returncode)
      if failed.get('hostUnsafe',True):quarantined.add(pid)
      continue
    roles[role]={'status':'worker-error','pid':pid,'exitCode':proc.returncode};quarantined.add(pid);continue
   evidence=json.loads(result.read_text(encoding='utf-8-sig'))
   if evidence.get('token')!=token or evidence.get('role')!=role or evidence.get('pid')!=pid:raise ValueError('Stale or wrong-host result')
   roles[role]=evidence
   if evidence.get('status')!='complete':quarantined.add(pid)
  except subprocess.TimeoutExpired:
   roles[role]={'status':'timeout','pid':pid};quarantined.add(pid)
   if proc is not None:
    proc.kill() # This Popen is exclusively the diagnostic child, never devenv.exe.
    try:proc.communicate(timeout=.15)
    except subprocess.TimeoutExpired:roles[role]['cleanupPending']=True
  except Exception as ex:
   roles[role]={'status':'worker-error','pid':pid,'error':str(ex)};quarantined.add(pid)
   if proc is not None and proc.poll() is None:proc.kill()
  finally:
   closer.close()
 write(lease,{'token':token,'active':False})
 status,reason=compare(case,roles)
 if any(r['status']=='timeout' for r in roles.values()):status,reason='timeout','Diagnostic worker exceeded shared item budget'
 elif any(r['status'] in ('host-quarantined','item-budget-exhausted','worker-error') for r in roles.values()):status,reason='unverified','Host/worker blocked; no pass inferred'
 elapsed=time.perf_counter()-started
 row={'id':case['id'],'status':status,'reason':reason,'roles':roles,'elapsedMs':round(elapsed*1000,2),'limitMs':5000,'withinBudget':elapsed<HARD_SECONDS,'fullFeatureCertified':False}
 write(out/'result.json',row)
 final_elapsed=time.perf_counter()-started
 row['elapsedMs']=round(final_elapsed*1000,2);row['withinBudget']=final_elapsed<HARD_SECONDS
 if not row['withinBudget']:row['status']='budget-violation';row['reason']='Measured 5-second limit exceeded; never silently accepted'
 write(out/'result.json',row);return row

def main():
 p=argparse.ArgumentParser();p.add_argument('--plan',type=Path,required=True);p.add_argument('--va-pid',type=int,required=True);p.add_argument('--ca-pid',type=int,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--worker',type=Path,default=ROOT/'tools/ShortcutProbe/SingleCaseProbe.exe');a=p.parse_args()
 if a.va_pid==a.ca_pid:raise ValueError('VA and CA must be different isolated hosts')
 plan=json.loads(a.plan.read_text(encoding='utf-8-sig'));cases=plan['cases'];assert len(cases)==len({c['id'] for c in cases})
 a.output.mkdir(parents=True,exist_ok=True);results=[];quarantined=set();commands={r:[str(a.worker.resolve()),"single-case"] for r in ('VA','CA')}
 for case in cases:
  row=run_case(case,{'VA':a.va_pid,'CA':a.ca_pid},commands,a.output/case['id'],quarantined);results.append(row)
  write(a.output/'summary.json',dict(results=results,quarantinedHostPids=sorted(quarantined),processed=len(results),total=len(cases),planHash=hashlib.sha256(a.plan.read_bytes()).hexdigest(),allWithinBudget=all(x['withinBudget'] for x in results)))
  print(case['id'],row['status'],row['elapsedMs'],flush=True)
 return 1 if any(not r['withinBudget'] for r in results) else 0
if __name__=='__main__':raise SystemExit(main())
