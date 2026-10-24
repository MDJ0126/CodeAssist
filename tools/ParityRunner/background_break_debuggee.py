import sys,json,hashlib,time
sys.path.insert(0,'tools/ParityRunner');from background_client import RUN,module;from background_steps import save,OUT
h=json.loads((OUT/'debuggee103.hosts.json').read_text());sha=hashlib.sha256((RUN/'debug-artifacts/ParityDebug103.exe').read_bytes()).hexdigest()
for role in ['VA','CA']:
 kw=dict(windowTitle='',pid=h[role]['pid'],fileSha256=sha);v,_=module(role,'owned-debug-signal',**kw);save(role+'.debuggee103.signal.json',v)
 for i in range(12):
  v,_=module(role,'owned-debug-state',**kw);save(role+'.debuggee103.break-state.json',v)
  if v.get('result',{}).get('mode')=='dbgBreakMode':break
  time.sleep(.15)
 print(role,v,flush=True)
