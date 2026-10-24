import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from background_client import module
state=json.loads((OUT/'VA.debuggee103.break-state.json').read_text());address=state['result']['expression']['value'];v,_=module('VA','native-memory-read',windowTitle='',address=address);save('VA.memory103.actual.json',v);print(v,flush=True)
