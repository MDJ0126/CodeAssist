import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,send;from background_steps import save
v,_=send('VA',dict(kind='classification'));save('VA.create-snippet-selection.after-menu.json',v);print(v.get('after'),flush=True)
v,_=module('VA','window-read',windowTitle='VA Snippet');save('VA.create-snippet-selection.wpf-find.json',v);print(v.get('result'),str(v.get('error'))[:300],flush=True)
