import sys,json,time,subprocess
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner')
from background_steps import launch
from background_client import RUN,send
O=Path('docs/validation/reexecution-108');B=Path('docs/validation/background-103');h=json.loads((RUN/'hosts.json').read_text());results=[]
def write(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def drive(role,label,spec):
 p=O/(role+'.'+label+'.driver.spec.json');write(p,spec);out=O/(role+'.'+label+'.driver.json');w=launch(['drive',h[role]['pid'],out,p]);w.wait(timeout=15);r=json.loads(out.read_text(encoding='utf-8-sig'));return r.get('acted')
plans=['add-include','hashtags','outline']
for name in plans:
 c=json.loads((B/(name+'.case.json')).read_text());c['undo']=False;row={'feature':c['feature'],'name':name,'roles':{}};n=c['feature']
 for role in ['VA','CA']:
  label=name;spec=O/(role+'.'+label+'.case.json');write(spec,c);out=O/(role+'.'+label+'.worker.json');w=launch(['case',h[role]['pid'],out,spec,role]);ds=c.get('drivers',{}).get(role,[])
  if n==17:ds=[dict(title='Convert Instance to Pointer',button='OK')] if role=='VA' else [dict(title='CodeAssist',button='확인')]
  if n==30 and role=='CA':ds=[]
  drivers=[]
  try:
   for i,d in enumerate(ds):drivers.append(drive(role,label+str(i),d))
   if n==8:
    pick=O/(role+'.methods.pick.spec.json');write(pick,dict(row='SecondListProbe',logicalKeys=role=='VA'));target=O/(role+'.methods.pick.json');q=launch(['pick',h[role]['pid'],target,pick]);q.wait(timeout=12);drivers.append(json.loads(target.read_text()).get('committed'))
   w.wait(timeout=12)
  except subprocess.TimeoutExpired:
   w.kill();w.wait();row['roles'][role]={'status':'tool-timeout','drivers':drivers};write(O/'core-final-progress.json',results+[row]);print(n,role,'TIMEOUT',flush=True);raise SystemExit(2)
  r=json.loads(out.read_text(encoding='utf-8-sig'));row['roles'][role]={'worker':r,'drivers':drivers}
  if r.get('status')=='complete':
   v,_=send(role,dict(kind='classification'));write(O/(role+'.'+label+'.observed.json'),v);row['roles'][role]['observed']=v
  else:print(n,role,'ERROR',str(r.get('error'))[:220],flush=True)
 results.append(row);write(O/'core-final-progress.json',results);print(n,name,[(r,x.get('worker',{}).get('status'))for r,x in row['roles'].items()],flush=True)
 if n==30:print('Pausing for exact generated enum prompt',flush=True);break
