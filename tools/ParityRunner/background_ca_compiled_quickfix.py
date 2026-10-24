import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import setup,save,launch,OUT
text='int* ReadAudit103() { return 0; }\r\nint main(void) { return ReadAudit103() != 0; }\r\n';folder=RUN/'CA/quickfix-compiled';folder.mkdir(exist_ok=True);(folder/'compile_flags.txt').write_text('-std=c++17\n-Wzero-as-null-pointer-constant\n');(folder/'.clangd').write_text('Diagnostics:\n  ClangTidy:\n    Add: [modernize-use-nullptr, modernize-redundant-void-arg]\n');setup('CA','inspection-compiled',text,text.index('return 0')+7,file='quickfix-compiled/QuickFix103.cpp');v,_=module('CA','queue-command',windowTitle='',command=str(0x196),focusEditor=True);save('CA.inspection-compiled.editor-queued.json',v);time.sleep(1)
for n in range(15):
 v,_=module('CA','ca-quickfix-read',windowTitle='');save('CA.inspection-compiled.editor-actions.json',v)
 if (v.get('result') or {}).get('status')=='observed' or v.get('error'):break
 time.sleep(.2)
print(v,flush=True);h=json.loads((RUN/'hosts.json').read_text());p=launch(['ui-tree',h['CA']['pid'],OUT/'CA.inspection-compiled.editor-tree.json']);p.wait(timeout=15)
