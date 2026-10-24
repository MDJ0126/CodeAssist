import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text());c=json.loads((OUT/'create-all.input.json').read_text())
def drive(role,name,spec):
 p=OUT/(name+'.driver.json');save(p.name,spec);q=launch(['drive',h[role]['pid'],OUT/(name+'.driver-result.json'),p]);q.wait(timeout=15);r=json.loads((OUT/(name+'.driver-result.json')).read_text(encoding='utf-8-sig'));assert r['acted'];time.sleep(.25)
for role in ['VA','CA']:
 if role=='VA':drive(role,'VA.create-all.confirm',dict(title='Create Implementations -- Visual Assist',button='OK'))
 else:
  (RUN/role/'create-all.cpp').write_bytes(c['source'].encode());setup(role,'create-all',c['header'],c['start'],file='create-all.h',extras=['create-all.cpp']);v,p=module(role,'queue-command',windowTitle='',command='392',focusEditor=True);save('CA.create-all.queued.json',v);drive(role,'CA.create-all.owner',dict(title='CodeAssist',button='Save',edit='CreateAllAudit'));q=launch(['ui-tree',h[role]['pid'],OUT/'CA.create-all.actual-dialog-tree.json']);q.wait(timeout=15);r=json.loads((OUT/'CA.create-all.actual-dialog-tree.json').read_text(encoding='utf-8-sig'));print('CA first controls',[(x['type'],x['name']) for x in r[:4]],flush=True)
  if any(x['name']=='The selected syntax is unsupported. Select the complete declaration or expression.' for x in r):drive(role,'CA.create-all.warning-ok',dict(title='CodeAssist',button='OK'))
  else:drive(role,'CA.create-all.apply',dict(title='Generate Method Implementation',button='Apply changes'))
 v,p=send(role,dict(kind='classification'));save(role+'.create-all.after.json',v);print(role,repr(v.get('after',{}).get('text')),flush=True)
 for ext in ['h','cpp']:
  q=launch(['read-document',h[role]['pid'],OUT/(role+'.create-all.'+ext+'.buffer.json'),RUN/role/('create-all.'+ext)]);q.wait(timeout=15)
 if role=='VA':
  for step in range(3):
   module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.35);v,p=send(role,dict(kind='classification'));save(role+'.create-all.undo%d.json'%step,v)
   if v['after']['text']==c['source']:break
