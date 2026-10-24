import sys,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import save
v,_=module('CA','queue-command',windowTitle='',command=str(0x197),focusEditor=True);save('CA.inspection-tool.queued.json',v)
for n in range(25):
 time.sleep(.15);v,_=module('CA','ca-quickfix-read',windowTitle='')
 if (v.get('result') or {}).get('status')=='observed':break
save('CA.inspection-tool.actions.json',v);print('tool',v.get('error'),len((v.get('result') or {}).get('rows',[])),flush=True)
