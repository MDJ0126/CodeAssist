import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text())
for role in ['VA','CA']:
 setup(role,'create-corresponding-file','#pragma once\r\nclass CreateFileAudit103 {};\r\n',24,file='CreateFileAudit103.h')
 v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorCreateFile' if role=='VA' else '370',focusEditor=True);save(role+'.create-corresponding-file.queued.json',v)
 time.sleep(.3);q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.create-corresponding-file.tree.json')]);q.wait(timeout=15)
 r=json.loads((OUT/(role+'.create-corresponding-file.tree.json')).read_text(encoding='utf-8-sig'));print(role,[(x['name'],x['id'],x.get('value')) for x in r if x['type']=='ControlType.Edit' and x['id'] in ['1210','1001'] or x['type']=='ControlType.TitleBar'][:6],flush=True)
