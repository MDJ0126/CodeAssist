import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from background_client import module
address=json.loads((OUT/'VA.debuggee103.break-state.json').read_text())['result']['expression']['value'];v,_=module('VA','native-memory-inspect',windowTitle='',address=address);save('VA.memory103.controls.json',v);print(v,flush=True)
