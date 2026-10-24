import sys
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import module
v,_=module('VA','native-step-open',windowTitle='');save('VA.step103.opened.json',v);print(v,flush=True)
