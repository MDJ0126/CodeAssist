import sys,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module,send,RUN;from background_steps import setup,focus_existing,save
text='int* ReadAudit103() { return 0; }\r\nint main(void) { return ReadAudit103() != 0; }\r\n'
setup('VA','inspection-nullptr',text,text.index('return 0')+7);module('VA','native-inspection-read',windowTitle='',enabled=True,reparse=True)
try:
 for n in range(20):
  time.sleep(.15);v,_=module('VA','native-inspection-read',windowTitle='')
  if len((v.get('result') or {}).get('fixes') or [])>=3:break
 save('VA.inspection-nullptr.final-issues.json',v);assert len(v['result']['fixes'])>=3;v,_=send('VA',{});save('VA.inspection-nullptr.enabled-render.json',v)
 v,_=module('VA','native-quickfix-window',windowTitle='');save('VA.inspection-nullptr.quickfix-window.json',v);print('window',v,flush=True);focus_existing('VA','inspection-fix-focus',RUN/'VA/inspection-nullptr.cpp');v,_=module('VA','native-nullptr-fix',windowTitle='');save('VA.inspection-nullptr.applied.json',v);print('fix',v,flush=True)
finally:
 focus_existing('VA','inspection-restore-focus',RUN/'VA/inspection-nullptr.cpp');v,_=module('VA','native-inspection-read',windowTitle='',enabled=False);save('VA.inspection-nullptr.final-settings-restored.json',v)
