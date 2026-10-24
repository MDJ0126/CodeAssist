import sys
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import save,focus_existing
v,_=module('CA','index-reinitialize',windowTitle='',_timeout=60);save('CA.inspection-nullptr.index-prepared.json',v);print(v,flush=True);assert v['result']['projectRoot']==str(RUN/'CA')
v,_=module('CA','index-state',windowTitle='',query='ReadAudit103');save('CA.inspection-nullptr.index-state.json',v);print(v,flush=True);assert v['result']['definitions'] and not v['result']['building']
focus_existing('CA','inspection-index-ready',RUN/'CA/inspection-nullptr.cpp');v,_=module('CA','marker-state',windowTitle='');save('CA.inspection-nullptr.ready-markers.json',v);print(v,flush=True)
