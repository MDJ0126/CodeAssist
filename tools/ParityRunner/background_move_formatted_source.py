import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module,send
h=json.loads((RUN/'hosts.json').read_text());header='#pragma once\r\nclass MoveFormatted103\r\n{\r\npublic:\r\n    int Value() const\r\n    {\r\n        return 7;\r\n    }\r\n};\r\n';cpp='#include "move-formatted103.h"\r\n';journal=[]
try:
 for role in ['VA','CA']:
  folder=RUN/role;flags=folder/'compile_flags.txt';journal.append(dict(role=role,path=str(flags),old=flags.read_text() if flags.exists() else None));flags.write_bytes(b'-std=c++17\n');(folder/'move-formatted103.cpp').write_bytes(cpp.encode());(folder/'move-formatted103.h').write_bytes(header.encode());setup(role,'move-formatted-source',header,header.index('Value')+2,file='move-formatted103.h',extras=['move-formatted103.cpp']);time.sleep(.8)
  v,p=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorMoveImplementationToSourceFile' if role=='VA' else '267',focusEditor=True);save(role+'.move-formatted-source.queued.json',v)
  if role=='CA':
   end=time.monotonic()+20
   while time.monotonic()<end:
    v,p=module(role,'compiler-read',windowTitle='');
    if v.get('result',{}).get('status')=='observed':break
    time.sleep(.25)
   save('CA.move-formatted-source.actions.json',v);print('CA actions',v.get('result'),flush=True)
   if 'Move function body to out-of-line' in v.get('result',{}).get('labels',[]):
    v,p=module(role,'compiler-select',windowTitle='',label='Move function body to out-of-line');save('CA.move-formatted-source.action-select.json',v);v,p=module('CA','window-read',windowTitle='CodeAssist: Move function body');save('CA.move-formatted-source.preview.json',v)
    d=OUT/'CA.move-formatted-source.apply.driver.json';save(d.name,dict(title='Move function body',button='Apply changes',privateFocus=True));q=launch(['drive',h[role]['pid'],OUT/'CA.move-formatted-source.apply-result.json',d]);q.wait(timeout=15)
  time.sleep(.6)
  for name in ['move-formatted103.h','move-formatted103.cpp']:
   o=OUT/(role+'.move-formatted-source.'+name+'.buffer.json');q=launch(['read-document',h[role]['pid'],o,folder/name]);q.wait(timeout=15);r=json.loads(o.read_text(encoding='utf-8-sig'));print(role,name,repr(r.get('text')),flush=True)
finally:
 for j in journal:
  from pathlib import Path
  p=Path(j['path']);p.unlink() if j['old'] is None else p.write_text(j['old']);j['restored']=True
 save('move-formatted-source.flags-transaction.json',journal)
