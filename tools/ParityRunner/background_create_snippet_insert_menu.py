import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());text='void CreateSelectionAudit103()\r\n{\r\n    int value = 103;\r\n}\r\n';selected='int value = 103;';setup('VA','create-snippet-menu',text,text.index(selected),len(selected));v,_=module('VA','queue-command',windowTitle='',command='VAssistX.VaSnippetInsert',focusEditor=True);save('VA.create-snippet-menu.queued.json',v);time.sleep(.2)
for mode in ['native-menus','ui-tree']:
 q=launch([mode,h['VA']['pid'],OUT/('VA.create-snippet-menu.'+mode+'.json')]);q.wait(timeout=15)
r=json.loads((OUT/'VA.create-snippet-menu.ui-tree.json').read_text(encoding='utf-8-sig'));print([(x['type'],x['name'],x['id']) for x in r if any(s in x['name'].lower() for s in ['selection','create','edit','snippet'])][:24],flush=True);print((OUT/'VA.create-snippet-menu.native-menus.json').read_text(encoding='utf-8-sig')[:1900],flush=True)
