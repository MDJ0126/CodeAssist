import sys,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import setup,save
text='int* ReadAudit103() { return 0; }\r\nint main(void) { return ReadAudit103() != 0; }\r\n'
for role in ['VA','CA']:
 setup(role,'inspection-nullptr',text,text.index('0'))
 if role=='VA':
  v,_=module(role,'native-inspection-read',windowTitle='',reparse=True);save('VA.inspection-nullptr.started.json',v);print(v,flush=True);time.sleep(2);v,_=module(role,'native-inspection-read',windowTitle='');save('VA.inspection-nullptr.results.json',v);print(v,flush=True)
 v,_=module(role,'marker-state',windowTitle='');save(role+'.inspection-nullptr.markers.json',v);print(role,str(v)[:1600],flush=True)
