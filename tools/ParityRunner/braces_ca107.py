import sys,json,subprocess,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner')
from background_client import module,send,RUN
from background_steps import setup,focus_existing,launch
def setup_raw(role,label,text,start,length=0):
 h=json.loads((RUN/'hosts.json').read_text());case=P/(role+'.'+label+'.case.json');out=P/(role+'.'+label+'.prepared.json');case.write_text(json.dumps(dict(id=label,file=label+'.cpp',text=text,start=start,length=length,inspectionOnly=True,undo=False,vaCommand='',caCommand='')));q=launch(['case',h[role]['pid'],out,case,role]);q.wait(timeout=20);v=json.loads(out.read_text(encoding='utf-8-sig'));assert v['status']=='complete',v;return v
P=Path('.local/parity-runs/remaining-107')
def save(name,value):
 (P/name).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
def physical(role,label,action,**kwargs):
 h=json.loads((RUN/'hosts.json').read_text());request=P/(role+'.'+label+'.input.case.json');output=P/(role+'.'+label+'.input.json');request.write_text(json.dumps(dict(pid=h[role]['pid'],action=action,output=str(output.resolve()),**kwargs),ensure_ascii=False));r=subprocess.run(['tools/ShortcutProbe/SingleCaseProbe.exe','physical-step106',str(request)],capture_output=True,timeout=20,creationflags=0x08000000);save(role+'.'+label+'.worker.json',dict(exitCode=r.returncode,stdout=r.stdout.decode(errors='replace'),stderr=r.stderr.decode(errors='replace')));assert r.returncode==0,(role,label,r.stdout.decode(errors='replace'));return json.loads(output.read_text())
def observed(role,label):
 v,_=module(role,'marker-state',windowTitle='');save(role+'.'+label+'.markers.json',v);c,_=module(role,'view-classifications105',windowTitle='');save(role+'.'+label+'.classes.json',c);v,_=send(role,{});save(role+'.'+label+'.render.json',v);print(role,label,'markers',len((json.loads((P/(role+'.'+label+'.markers.json')).read_text()).get('result')or{}).get('markers',[])),'brace-tags',[x for x in (c.get('result')or{}).get('tags',[]) if 'brace'in x['type']],flush=True)
def option(role,op,**kw):
 for attempt in range(30):
  v,_=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
  if v.get('error'):raise RuntimeError(v['error'])
  if (v.get('result')or{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
if __name__=='__main__':
 for role in ['CA']:
  for case,text in [('matched','void BracePhysical106() {\r\n    return;\r\n}\r\n'),('mismatch','void BracePhysical106() {\r\n    return;\r\n}\r\n}\r\n')]:
   pos=text.index('{') if case=='matched' else text.rindex('}')
   setup(role,'physical-'+case+'106',text,pos);physical(role,case+'-focus','focus');module(role,'arm-markers105',windowTitle='');physical(role,case+'-move','keys',keys='RIGHT');time.sleep(.7);observed(role,case+'-active');physical(role,case+'-away','keys',keys='UP');observed(role,case+'-away')
