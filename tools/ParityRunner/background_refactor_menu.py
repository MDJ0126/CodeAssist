import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,RUN
h=json.loads((RUN/'hosts.json').read_text());text='class MenuAudit103 { public: int Compute(int value) { return value + 1; } };\r\n'
for role in ['VA','CA']:
 setup(role,'refactor-context-menu',text,text.index('Compute')+2)
 v,p=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorContextMenu' if role=='VA' else '267',focusEditor=True);save(role+'.refactor-context-menu.queued.json',v);time.sleep(.6)
 q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.refactor-context-menu.tree.json')]);q.wait(timeout=15)
 rows=json.loads((OUT/(role+'.refactor-context-menu.tree.json')).read_text(encoding='utf-8-sig'));items=[x for x in rows if x['type']=='ControlType.MenuItem'];save(role+'.refactor-context-menu.items.json',items);print(role,[(x['name'],x.get('enabled')) for x in items][:25],flush=True)
 q=launch(['logical-escape',h[role]['pid'],OUT/(role+'.refactor-context-menu.escape.json')]);q.wait(timeout=10)
