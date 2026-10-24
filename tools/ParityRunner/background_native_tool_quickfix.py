import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import save,focus_existing,OUT,launch;from compiler_contracts import build_run
focus_existing('VA','inspection-tool-fix',RUN/'VA/inspection-nullptr.cpp');module('VA','native-inspection-read',windowTitle='',enabled=True,reparse=True)
try:
 for n in range(20):
  time.sleep(.15);v,_=module('VA','native-inspection-read',windowTitle='')
  if len((v.get('result') or {}).get('fixes') or [])>=3:break
 save('VA.inspection-tool.issues.json',v);assert len(v['result']['fixes'])>=3;v,_=module('VA','native-quickfix-window',windowTitle='');save('VA.inspection-tool.window.json',v);v,_=module('VA','native-tool-nullptr-fix',windowTitle='');save('VA.inspection-tool.applied.json',v);print(v,flush=True);assert v.get('result',{}).get('after',{}).get('text','').startswith('int* ReadAudit103() { return nullptr; }');r=build_run('inspection-tool-quickfix','VA',v['result']['after']['text'],'ReadAudit103 returns nullptr; main returns0');save('compiler-check/VA-inspection-tool-quickfix-result.json',r);assert r['passed'];focus_existing('VA','inspection-tool-undo',RUN/'VA/inspection-nullptr.cpp');module('VA','queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.2);h=json.loads((RUN/'hosts.json').read_text());p=launch(['read-document',h['VA']['pid'],OUT/'VA.inspection-tool.undo.json',RUN/'VA/inspection-nullptr.cpp']);p.wait(timeout=15);assert json.loads((OUT/'VA.inspection-tool.undo.json').read_text())['text']=='int* ReadAudit103() { return 0; }\r\nint main(void) { return ReadAudit103() != 0; }\r\n'
finally:
 focus_existing('VA','inspection-tool-restored',RUN/'VA/inspection-nullptr.cpp');v,_=module('VA','native-inspection-read',windowTitle='',enabled=False);save('VA.inspection-tool.settings-restored.json',v)
