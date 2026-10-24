import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text());c=json.loads((OUT/'similar-member.input.json').read_text())
def drive(role,name,spec):
 p=OUT/(name+'.driver.json');save(p.name,spec);q=launch(['drive',h[role]['pid'],OUT/(name+'.driver-result.json'),p]);q.wait(timeout=15);r=json.loads((OUT/(name+'.driver-result.json')).read_text(encoding='utf-8-sig'));assert r['acted'];time.sleep(.25)
for role in ['VA','CA']:
 if role=='VA':drive(role,'VA.similar-member.add',dict(title='Add Similar Member',button='Add',edit='int secondField'))
 else:
  setup(role,'similar-member',c['text'],c['start']);v,p=module(role,'queue-command',windowTitle='',command='387',focusEditor=True);save('CA.similar-member.queued.json',v);drive(role,'CA.similar-member.signature',dict(title='CodeAssist',button='Save',edit='secondField'));drive(role,'CA.similar-member.apply',dict(title='Add Similar Member',button='Apply changes'))
 v,p=send(role,dict(kind='classification'));save(role+'.similar-member.after.json',v);print(role,repr(v.get('after',{}).get('text')),flush=True);module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.4);v,p=send(role,dict(kind='classification'));save(role+'.similar-member.undo.json',v);print('undo',v['after']['text']==c['text'],flush=True)
