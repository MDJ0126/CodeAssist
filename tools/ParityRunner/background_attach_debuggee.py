import sys,json,hashlib
sys.path.insert(0,'tools/ParityRunner');from background_client import RUN,module;from background_steps import save,OUT
h=json.loads((OUT/'debuggee103.hosts.json').read_text());sha=hashlib.sha256((RUN/'debug-artifacts/ParityDebug103.exe').read_bytes()).hexdigest()
for role in ['VA','CA']:
 v,_=module(role,'owned-debug-attach',windowTitle='',pid=h[role]['pid'],fileSha256=sha);save(role+'.debuggee103.attach.json',v);print(role,v,flush=True)
