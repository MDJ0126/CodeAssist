import sys,json,time
sys.path.insert(0,'tools/ParityRunner')
from background_steps import setup,save,launch,OUT
from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text())
text='class ForwardOwner103 {\r\n    ForwardNeeded103* item;\r\n};\r\n'
for role in ['VA','CA']:
 setup(role,'forward-declaration',text,text.index('ForwardNeeded103')+3)
 v,p=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorAddForwardDeclaration' if role=='VA' else '378',focusEditor=True);save(role+'.forward-declaration.queued.json',v);time.sleep(.8)
 out=OUT/(role+'.forward-declaration.tree.json');q=launch(['ui-tree',h[role]['pid'],out]);q.wait(timeout=15)
 if role=='CA':
  spec=OUT/'CA.forward-declaration.driver.json';save(spec.name,dict(title='CodeAssist',edit='ForwardNeeded103',button='저장'));q=launch(['drive',h[role]['pid'],OUT/'CA.forward-declaration.driver-result.json',spec]);q.wait(timeout=15);time.sleep(.5)
 v,p=send(role,dict(kind='classification'));save(role+'.forward-declaration.observed.json',v);print(role,v.get('after',{}).get('text'),flush=True)
