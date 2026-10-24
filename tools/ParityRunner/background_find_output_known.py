import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import setup,save,OUT,launch
text='// FindAudit103 one\r\n// FindAudit103 two\r\nint main() { return 0; }\r\n';h=json.loads((RUN/'hosts.json').read_text())
for role in ['VA','CA']:
 setup(role,'find-comment',text,0);v,_=module(role,'owned-find',windowTitle='');save(role+'.find-comment.actual.json',v);print(role,'find',v,flush=True)
 r=json.loads((OUT/(role+'.find-comment.prepared.json')).read_text());file=str(RUN/role/'Bootstrap.cpp')
 try:
  v,_=module(role,'owned-output',windowTitle='',file=file,keep=True);save(role+'.build-output-known.actual.json',v);p=launch(['observe',h[role]['pid'],OUT/(role+'.build-output-known.native.json'),OUT/(role+'.find-comment.prepared.json')]);p.wait(timeout=15);print(role,'output',v,flush=True)
 finally:module(role,'owned-output',windowTitle='',file=file)
