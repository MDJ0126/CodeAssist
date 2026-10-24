import sys,json,time,hashlib
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner')
from foreground106 import P
from background_client import RUN
from background_steps import launch
master=json.loads(Path('docs/validation/background-103/adjudicated.json').read_text());catalog=json.loads(Path('docs/validation/background-103/cases.json').read_text())['cases'];out=Path('docs/validation/full-108-107');out.mkdir(exist_ok=True);hosts=json.loads((RUN/'hosts.json').read_text());rows=json.loads((out/'core-replay.json').read_text()) if (out/'core-replay.json').exists() else []
for number in [30,34,35,40]:
 definition=next(x for x in master['newJudgments']if x['id']=='VA-F%03d'%number);old=definition['evidence']['VA']['data'];before=old['before'];c=next(x for x in catalog if x['feature']==number);record=dict(id=definition['id'],startedAt=time.time(),roles={})
 for role in ['VA','CA']:
  case=dict(id='full-recheck107-'+str(number),file=Path(before['file']).name,text=before['text'],start=before['selectionStart'],length=before['selectionLength'],inspectionOnly=False,undo=False,vaCommand=c['vaCommand'],caCommand=c['caCommand']);casePath=out/(role+'.'+str(number)+'.case.json');casePath.write_text(json.dumps(case));result=out/(role+'.'+str(number)+'.result.json');worker=launch(['case',hosts[role]['pid'],result,casePath,role]);drivers=[]
  if role=='CA'and number==17:
   spec=out/'CA.17.driver.case.json';spec.write_text(json.dumps(dict(title='CodeAssist',button='확인')));driver=launch(['drive',hosts[role]['pid'],out/'CA.17.driver.json',spec]);drivers.append(driver)
  try:worker.wait(timeout=20)
  except __import__('subprocess').TimeoutExpired:
   record['roles'][role]=dict(status='timeout',pid=hosts[role]['pid']);worker.kill();worker.wait()
  else:record['roles'][role]=json.loads(result.read_text(encoding='utf-8-sig')) if result.exists() else dict(status='missing-result')
  for driver in drivers:
   try:driver.wait(timeout=2)
   except __import__('subprocess').TimeoutExpired:driver.kill();driver.wait()
 a=record['roles'];record['sameInput']=all(x.get('before',{}).get('text')==before['text'] for x in a.values());record['positiveChange']=any(x.get('before') and x.get('after') and any(x['before'].get(k)!=x['after'].get(k) for k in ['text','caret','selectionStart','selectionLength'])for x in a.values());record['runtimeCompletedBoth']=all(x.get('status')=='complete'for x in a.values());record['complete']=record['sameInput'] and record['positiveChange'] and record['runtimeCompletedBoth'];rows.append(record);(out/'core-replay.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2));print(number,[(r,x.get('status'))for r,x in a.items()],'valid',record['complete'],flush=True)
 if not record['runtimeCompletedBoth']:
  print('Stopped for explicit recovery; no matching no-op or error pass',flush=True);break
