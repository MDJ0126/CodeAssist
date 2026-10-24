import sys,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import save,focus_existing,RUN
focus_existing('VA','inspection-enable',RUN/'VA/inspection-nullptr.cpp')
v,_=module('VA','native-inspection-read',windowTitle='',enabled=True,reparse=True);save('VA.inspection-nullptr.enabled.json',v);print(v,flush=True)
try:
 for n in range(8):
  time.sleep(.5);v,_=module('VA','native-inspection-read',windowTitle='');save('VA.inspection-nullptr.enabled-results.json',v)
  if (v.get('result') or {}).get('fixes'):break
 print(v,flush=True);v,_=module('VA','marker-state',windowTitle='');save('VA.inspection-nullptr.enabled-markers.json',v);print(v,flush=True)
finally:
 v,_=module('VA','native-inspection-read',windowTitle='',enabled=False);save('VA.inspection-nullptr.disabled-restored.json',v);print('restored',v.get('result',{}).get('enabled'),flush=True)
