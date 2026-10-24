import sys
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import module
v,_=module('VA','native-pattern-query',windowTitle='');save('VA.source-link-pattern.shape.json',v);print(v,flush=True)
