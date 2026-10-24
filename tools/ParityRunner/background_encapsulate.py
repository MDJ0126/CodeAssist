import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
hosts=json.loads((RUN/'hosts.json').read_text())
def drive(role,name,title,button):
 s=OUT/(name+'.driver.json');save(s.name,dict(title=title,button=button));o=OUT/(name+'.driver-result.json');p=launch(['drive',hosts[role]['pid'],o,s]);p.wait(timeout=15);r=json.loads(o.read_text(encoding='utf-8-sig'));assert r['acted'];time.sleep(.3)
text='class EncapAudit {\r\npublic:\r\n    int value;\r\n    int Read() { return value; }\r\n};\r\n'
for role in ['VA','CA']:
 if role=='VA':drive(role,'VA.encapsulate.refactor','Encapsulate Field','Refactor')
 else:
  setup(role,'encapsulate',text,text.index('value')+2);v,p=module(role,'queue-command',windowTitle='',command='385',focusEditor=True);save('CA.encapsulate.queued.json',v);drive(role,'CA.encapsulate.suffix','CodeAssist','Save');drive(role,'CA.encapsulate.apply','Encapsulate Field','Apply changes')
 v,p=send(role,dict(kind='classification'));save(role+'.encapsulate.after.json',v);print(role,v['status'],repr(v.get('after',{}).get('text')),flush=True)
 if v.get('after',{}).get('text')!=text:
  q,p=module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.5);v,p=send(role,dict(kind='classification'));save(role+'.encapsulate.undo.json',v);print(role,'undo',v.get('after',{}).get('text')==text,flush=True)
