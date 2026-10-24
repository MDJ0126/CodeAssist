import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,OUT;from background_client import module,send
header='class ForwardKnown103 { public: int value; };\r\n';text='class ForwardOwnerWarm103 {\r\n    ForwardKnown103* item;\r\n};\r\n'
for role in ['VA','CA']:
 setup(role,'forward-known-header',header,header.index('ForwardKnown103')+2,file='forward-known.h');time.sleep(.8)
 setup(role,'forward-known-use',text,text.index('ForwardKnown103')+3,extras=['forward-known.h']);time.sleep(2)
 v,p=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorAddForwardDeclaration' if role=='VA' else '378',focusEditor=True);save(role+'.forward-known.queued.json',v);time.sleep(.8)
 if role=='CA':
  from background_steps import launch
  from background_client import RUN
  h=json.loads((RUN/'hosts.json').read_text());spec=OUT/'CA.forward-known.driver.json';save(spec.name,dict(title='CodeAssist',edit='ForwardKnown103',button='Save'));q=launch(['drive',h[role]['pid'],OUT/'CA.forward-known.driver-result.json',spec]);q.wait(timeout=15)
 v,p=send(role,dict(kind='classification'));save(role+'.forward-known.observed.json',v);print(role,repr(v.get('after',{}).get('text')),flush=True)
