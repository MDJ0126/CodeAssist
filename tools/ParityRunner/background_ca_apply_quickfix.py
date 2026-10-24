import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import save,launch,OUT
v,_=module('CA','ca-quickfix-read',windowTitle='',apply=True);save('CA.inspection-compiled.editor-chosen.json',v);print('chosen',v.get('error'),flush=True);title="CodeAssist: change '0' to 'nullptr'"
for n in range(25):
 v,_=module('CA','window-read',windowTitle=title)
 if (v.get('result') or {}).get('status')=='observed':break
 time.sleep(.1)
save('CA.inspection-compiled.preview.json',v);assert (v.get('result') or {}).get('status')=='observed',v
v,_=module('CA','arm-owner-focus-restore',windowTitle=title);save('CA.inspection-compiled.focus-restorer.json',v);assert v['status']=='complete',v
v,_=module('CA','options-button',windowTitle=title,button='Apply changes');save('CA.inspection-compiled.preview-approved.json',v);time.sleep(.3);h=json.loads((RUN/'hosts.json').read_text());p=launch(['read-document',h['CA']['pid'],OUT/'CA.inspection-compiled.after.json',RUN/'CA/quickfix-compiled/QuickFix103.cpp']);p.wait(timeout=15);print((OUT/'CA.inspection-compiled.after.json').read_text(),flush=True)
