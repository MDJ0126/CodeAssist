import json,time,uuid,os,hashlib,argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];RUN=ROOT/'.local/parity-runs/background-103';RESP=RUN/'background-capture'
def send(role,data,timeout=15):
 hosts=json.loads((RUN/'hosts.json').read_text());id=data.get('id') or ('request-'+uuid.uuid4().hex[:16]);data=dict(data,id=id);kind=data.get('kind');suffix='-module.json' if kind=='module' else '-probe.json' if kind else '.json';path=RESP/(role+'-'+id+suffix)
 q=RUN/(role+'.capture-request.json');temp=q.with_suffix('.tmp');temp.write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8')
 for attempt in range(40):
  try:os.replace(temp,q);break
  except PermissionError:
   if attempt==39:raise
   time.sleep(.025)
 deadline=time.monotonic()+timeout
 while time.monotonic()<deadline:
  if path.exists():
   try:value=json.loads(path.read_text(encoding='utf-8-sig'))
   except (json.JSONDecodeError,PermissionError):time.sleep(.025);continue
   if value.get('id')!=id or value.get('pid')!=hosts[role]['pid']:raise RuntimeError('Stale or wrong-host response')
   return value,path
  error=RESP/(role+'-'+id+'.error.json')
  if error.exists():raise RuntimeError(error.read_text())
  time.sleep(.08)
 raise TimeoutError('No response for '+role+' '+id)
def module(role,operation,_timeout=15,**kwargs):
 p=RUN/'modules/CodeAssist.LiveProbe.108.dll';return send(role,dict(kind='module',module=str(p),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),operation=operation,**kwargs),timeout=_timeout)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('role',choices=['VA','CA']);p.add_argument('operation');p.add_argument('--title',required=True);p.add_argument('--page');p.add_argument('--output',type=Path,required=True);a=p.parse_args();kwargs={'windowTitle':a.title};
 if a.page:kwargs['page']=a.page
 value,path=module(a.role,a.operation,**kwargs);a.output.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({'status':value['status'],'response':str(path),'resultStatus':(value.get('result') or {}).get('status'),'error':value.get('error')},ensure_ascii=False))
