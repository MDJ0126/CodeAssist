import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,RUN
h=json.loads((RUN/'hosts.json').read_text());text='class ContextMenuAudit103 { public: int Compute(int value) { return value + 1; } };\r\n'
for role in ['VA','CA']:
 module(role,'cancel-menus',windowTitle='')
 setup(role,'editor-context-menu',text,text.index('Compute')+2)
 v,p=module(role,'editor-menu-service',windowTitle='');save(role+'.editor-context-menu.posted.json',v);time.sleep(.3)
 q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.editor-context-menu.tree.json')]);q.wait(timeout=15);a=json.loads((OUT/(role+'.editor-context-menu.tree.json')).read_text(encoding='utf-8-sig'));items=[x for x in a if x['type']=='ControlType.MenuItem'];save(role+'.editor-context-menu.items.json',items);print(role,[x['name'] for x in items][:30],flush=True)
 v,p=module(role,'render-sources',windowTitle='');save(role+'.editor-context-menu.sources.json',v)
 q=launch(['native-menus',h[role]['pid'],OUT/(role+'.editor-context-menu.native-state.json')]);q.wait(timeout=10)
 v,p=module(role,'cancel-menus',windowTitle='');save(role+'.editor-context-menu.cancel.json',v)
