import sys,json,time,re
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
hosts=json.loads((RUN/'hosts.json').read_text());c=json.loads((OUT/'extract.input.json').read_text())
def drive(role,id,spec):
 p=OUT/(id+'.driver.json');save(p.name,spec);q=launch(['drive',hosts[role]['pid'],OUT/(id+'.driver-result.json'),p]);q.wait(timeout=15);r=json.loads((OUT/(id+'.driver-result.json')).read_text(encoding='utf-8-sig'));assert r['acted'];time.sleep(.25)
name=None
for role in ['VA','CA']:
 if role=='VA':drive(role,'VA.extract.confirm',dict(title='Extract Method',button='Extract'))
 else:
  setup(role,'extract',c['text'],c['start'],c['length']);v,p=module(role,'queue-command',windowTitle='',command='396',focusEditor=True);save('CA.extract.queued.json',v);drive(role,'CA.extract.signature',dict(title='CodeAssist',button='Save',edit='void '+name+'()'));drive(role,'CA.extract.apply',dict(title='Extract Method',button='Apply changes'))
 v,p=send(role,dict(kind='classification'));save(role+'.extract.after.json',v);assert v['status']=='complete';print(role,repr(v['after']['text']),flush=True)
 if role=='VA':
  names=re.findall(r'\bvoid\s+(\w+)\(',v['after']['text']);name=next(n for n in names if n not in ['StepOne','StepTwo','ExtractAudit']);save('extract.native-name.json',dict(name=name))
 v,p=module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.5);v,p=send(role,dict(kind='classification'));save(role+'.extract.undo.json',v);print(role,'undo',v['after']['text']==c['text'],flush=True)
