import sys,json,hashlib,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from background_client import RUN,module
h=json.loads((OUT/'debuggee103.hosts.json').read_text());sha=hashlib.sha256((RUN/'debug-artifacts/ParityDebug103.exe').read_bytes()).hexdigest()
for role in ['VA','CA']:
 kw=dict(windowTitle='',pid=h[role]['pid'],fileSha256=sha);v,_=module(role,'owned-debug-step-over',**kw);save(role+'.step103.baseline-over.json',v);time.sleep(.3);v,_=module(role,'owned-debug-step-into',**kw);save(role+'.step103.baseline-into.json',v);time.sleep(.3);v,_=module(role,'owned-debug-state',**kw);save(role+'.step103.baseline-frame.json',v);print(role,v,flush=True)
v,_=module('VA','native-step-state',windowTitle='');save('VA.step103.history-shape.json',v);print(v,flush=True)
