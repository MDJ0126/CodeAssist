import sys
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import setup,save
text='int FindAudit103 = 7;\r\nint main() { return FindAudit103; }\r\n'
for role in ['VA','CA']:
 r=setup(role,'find-highlight',text,0);v,_=module(role,'owned-find',windowTitle='');save(role+'.find-highlight.actual.json',v);print(role,'find',v,flush=True)
 v,_=module(role,'owned-output',windowTitle='',file=r['after']['file']);save(role+'.output-highlight.actual.json',v);print(role,'output',v,flush=True)
