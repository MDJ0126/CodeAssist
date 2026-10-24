import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text());c=json.loads((OUT/'add-member.input.json').read_text())
def drive(role,name,spec):
 p=OUT/(name+'.driver.json');save(p.name,spec);q=launch(['drive',h[role]['pid'],OUT/(name+'.driver-result.json'),p]);q.wait(timeout=15);r=json.loads((OUT/(name+'.driver-result.json')).read_text(encoding='utf-8-sig'));assert r['acted'];time.sleep(.25)
for role in ['VA','CA']:
 if role=='VA':drive(role,'VA.add-member.add',dict(title='Add Member',button='Add',edit='int addedField;'))
 else:
  setup(role,'add-member',c['text'],c['start']);v,p=module(role,'queue-command',windowTitle='',command='386',focusEditor=True);save('CA.add-member.queued.json',v);drive(role,'CA.add-member.signature',dict(title='CodeAssist',button='Save',edit='int addedField;'));drive(role,'CA.add-member.apply',dict(title='Add Member',button='Apply changes'))
 v,p=send(role,dict(kind='classification'));save(role+'.add-member.after.json',v);print(role,repr(v.get('after',{}).get('text')),flush=True);module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.4);v,p=send(role,dict(kind='classification'));save(role+'.add-member.undo.json',v);print('undo',v['after']['text']==c['text'],flush=True)
