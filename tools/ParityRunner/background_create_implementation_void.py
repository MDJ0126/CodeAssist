import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text());header='#pragma once\r\n#include <stdexcept>\r\nclass CreateImplementationVoidAudit {\r\npublic:\r\n    void Compute();\r\n};\r\n';source='#include "create-implementation-v3.h"\r\n';save('create-implementation-v3.input.json',dict(header=header,source=source))
def drive(role,name,spec):
 p=OUT/(name+'.driver.json');save(p.name,spec);q=launch(['drive',h[role]['pid'],OUT/(name+'.driver-result.json'),p]);q.wait(timeout=15);r=json.loads((OUT/(name+'.driver-result.json')).read_text(encoding='utf-8-sig'));assert r['acted'];time.sleep(.25)
for role in ['VA','CA']:
 file=RUN/role/'create-implementation-v3.cpp';file.write_bytes(source.encode('utf-8'));setup(role,'create-implementation-v3',header,header.index('Compute')+2,file='create-implementation-v3.h',extras=['create-implementation-v3.cpp']);v,p=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorCreateImplementation' if role=='VA' else '391',focusEditor=True);save(role+'.create-implementation-v3.queued.json',v);time.sleep(.6)
 if role=='CA':drive(role,'CA.create-implementation-v3.owner',dict(title='CodeAssist',button='Save',edit='CreateImplementationVoidAudit'));drive(role,'CA.create-implementation-v3.apply',dict(title='Generate Method Implementation',button='Apply changes'))
 v,p=send(role,dict(kind='classification'));save(role+'.create-implementation-v3.after.json',v);print(role,v['status'],v.get('after',{}).get('file'),flush=True)
 for ext in ['h','cpp']:
  q=launch(['read-document',h[role]['pid'],OUT/(role+'.create-implementation-v3.'+ext+'.buffer.json'),RUN/role/('create-implementation-v3.'+ext)]);q.wait(timeout=15)
 if role=='VA':print(v.get('after',{}).get('text'),flush=True)
 module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.4);v,p=send(role,dict(kind='classification'));save(role+'.create-implementation-v3.undo.json',v)
