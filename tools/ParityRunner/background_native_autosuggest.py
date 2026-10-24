import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());text='bool NativeSuggestion103() { bool value = ; return value; }\r\n';setup('VA','suggest-auto-native',text,text.index('; return'));v,_=module('VA','posted-text',windowTitle='',text='tru');save('VA.suggest-auto-native.typed.json',v)
for mode in ['windows','ui-tree','native-menus']:
 q=launch([mode,h['VA']['pid'],OUT/('VA.suggest-auto-native.'+mode+'.json')]);q.wait(timeout=15)
 if mode=='windows':print((OUT/('VA.suggest-auto-native.'+mode+'.json')).read_text(encoding='utf-8-sig')[:1000],flush=True)
r=json.loads((OUT/'VA.suggest-auto-native.ui-tree.json').read_text(encoding='utf-8-sig'));print([(x['type'],x['name'],x['id']) for x in r if x['type'] in ['ControlType.ListItem','ControlType.DataItem']][:20],flush=True)
