import sys,json,time,base64
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import OUT,save,launch,setup,focus_existing
h=json.loads((RUN/'hosts.json').read_text());p=launch(['show-owned',h['VA']['pid'],OUT/'VA.external-build.shown.json']);p.wait(timeout=15);print((OUT/'VA.external-build.shown.json').read_text(),flush=True);p=launch(['load-capture-package',h['VA']['pid'],OUT/'VA.external-build.watchdog.json']);p.wait(timeout=15)
text='#include "PlatformHeader103.h"\r\nint main() { return ExternalPlatform103::Value() == 103 ? 0 : 1; }\r\n';setup('VA','external-build-lookup',text,text.index('ExternalPlatform103')+4);time.sleep(1);v,_=module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);save('VA.external-build-lookup.queued.json',v);time.sleep(.4);v,_=module('VA','view-state',windowTitle='');save('VA.external-build-lookup.after.json',v);print('VA',v,flush=True)
flags=RUN/'CA/compile_flags.txt';journal=dict(path=str(flags),original=base64.b64encode(flags.read_bytes()).decode() if flags.exists() else None);flags.write_text('-std=c++17\n-I'+str(RUN/'shared-custom103').replace('\\','/')+'\n');save('external-build.flags-transaction.json',journal);setup('CA','external-build-lookup',text,text.index('ExternalPlatform103')+4);v,_=module('CA','queue-command',windowTitle='',command=str(0x100),focusEditor=True);save('CA.external-build-lookup.queued.json',v)
for n in range(35):
 time.sleep(.2);v,_=module('CA','view-state',windowTitle='')
 if (v.get('result') or {}).get('dteFile','').endswith('PlatformHeader103.h'):break
save('CA.external-build-lookup.after.json',v);print('CA',v,flush=True)
