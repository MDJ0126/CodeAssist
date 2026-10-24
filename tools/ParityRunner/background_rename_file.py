import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text())
for role in ['VA','CA']:
 header='#pragma once\r\nclass RenameFileAudit103 {};\r\n';cpp='#include "RenameFileAudit103.h"\r\n';(RUN/role/'RenameFileAudit103.cpp').write_bytes(cpp.encode())
 setup(role,'rename-related-file',header,24,file='RenameFileAudit103.h',extras=['RenameFileAudit103.cpp'])
 v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorRenameFiles' if role=='VA' else '371',focusEditor=True);save(role+'.rename-related-file.queued.json',v)
 time.sleep(.3);q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.rename-related-file.tree.json')]);q.wait(timeout=15)
 r=json.loads((OUT/(role+'.rename-related-file.tree.json')).read_text(encoding='utf-8-sig'));print(role,[(x['name'],x['id'],x.get('value')) for x in r[:10]],flush=True)
