import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import save
v,_=module('VA','native-list-read',windowTitle='',hwnd=3283458);save('VA.native-autosuggest.list.json',v);print(v,flush=True)
if v.get('status')=='complete' and any(x['text']=='true' for x in v.get('result',{}).get('rows',[])):
 v,_=module('VA','native-list-accept',windowTitle='',hwnd=3283458,choice='true');save('VA.native-autosuggest.accept.json',v);print(v,flush=True)
