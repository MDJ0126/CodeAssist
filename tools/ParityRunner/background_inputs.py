import json,subprocess,time,uuid,os
from pathlib import Path
from isolated_desktop import name,state,u,k
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'docs/validation/background-103';RUN=ROOT/'.local/parity-runs/background-103';WORKER=ROOT/'tools/ShortcutProbe/BackgroundInputSetup.exe';HOSTS={'VA':28796,'CA':35756}
def write(p,v):
 p=Path(p);tmp=p.with_suffix(p.suffix+'.tmp');tmp.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8');os.replace(tmp,p)
def request(role,id,data,timeout=12):
 data=dict(data,id=id);out=RUN/'background-capture'/(role+'-'+id+('-probe.json' if 'kind' in data else '.json'));write(RUN/(role+'.capture-request.json'),data);stop=time.monotonic()+timeout
 while time.monotonic()<stop:
  if out.exists():
   r=json.loads(out.read_text(encoding='utf-8-sig'))
   if r.get('pid')!=HOSTS[role] or r.get('id')!=id:raise RuntimeError('Wrong process/request reply')
   return r
  err=out.with_name(role+'-'+id+'.error.json')
  if err.exists():return {'status':'error','error':err.read_text()}
  time.sleep(.08)
 return {'status':'timeout','id':id}
def main():
 if not name(u.GetThreadDesktop(k.GetCurrentThreadId())).startswith('CodeAssistVerify-') or name(u.GetThreadDesktop(k.GetCurrentThreadId()))==state()['inputDesktop']:raise RuntimeError('Refusing input desktop')
 epoch=uuid.uuid4().hex[:8];rows=[]
 for c in json.loads((OUT/'input-cases.json').read_text())['cases']:
  row={'id':c['id'],'feature':c['feature'],'roles':{}}
  for role,pid in HOSTS.items():
   spec=OUT/(c['id']+'.case.json');setup=OUT/(role+'.'+c['id']+'.input-setup.json')
   p=subprocess.Popen([str(WORKER),'case',str(pid),str(setup),str(spec),role],creationflags=0x08000000,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
   try:p.wait(timeout=15)
   except subprocess.TimeoutExpired:p.kill();p.wait();raise RuntimeError('Setup worker blocked')
   s=json.loads(setup.read_text(encoding='utf-8-sig'))
   if s.get('status')!='complete' or s['after'].get('file')!=s['before'].get('file') or s['before']['text']!=c['text']:raise RuntimeError('Invalid input setup '+role+' '+c['id'])
   steps=[]
   for n,action in enumerate(c['actions']):
    data=dict(action,kind='input',expectedFile=s['before']['file']);id=epoch+'-'+c['id']+'-'+str(n);r=request(role,id,data);steps.append(r)
    if r.get('status')!='complete':
     row['roles'][role]={'setup':s,'steps':steps};rows.append(row);write(OUT/'input-status.json',{'rows':rows,'halted':role,'epoch':epoch});return 2
   row['roles'][role]={'setup':s,'steps':steps};write(OUT/(role+'.'+c['id']+'.input-result.json'),row['roles'][role])
  rows.append(row);write(OUT/'input-status.json',{'rows':rows,'epoch':epoch,'active':None});print(c['id'],'completed both',flush=True)
 return 0
if __name__=='__main__':raise SystemExit(main())
