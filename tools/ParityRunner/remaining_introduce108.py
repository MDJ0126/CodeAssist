import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT
from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text());text='int ExpressionAudit() { return (1 + 2) * 3; }\r\n'
for role in ['VA','CA']:
 setup(role,'introduce108',text,32,5);v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorIntroduceVariable' if role=='VA' else '267',focusEditor=True);save(role+'.introduce.queued.json',v)
 if role=='VA':
  spec=OUT/'VA.introduce.driver.spec.json';save(spec.name,dict(title='Introduce Variable',button='Introduce',edit='int subtotal'));w=launch(['drive',h[role]['pid'],OUT/'VA.introduce.driver.json',spec]);w.wait(timeout=15);assert json.loads((OUT/'VA.introduce.driver.json').read_text()).get('acted')
 else:
  time.sleep(.5);v,_=module(role,'compiler-read',windowTitle='');save('CA.introduce.actions.json',v)
 v,_=send(role,dict(kind='classification'));save(role+'.introduce.after.json',v);print(20,role,repr(v.get('after',{}).get('text')),flush=True)
 module(role,'cancel-menus',windowTitle='')
