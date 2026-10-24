import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text())
for role in ['VA','CA']:
 v,_=module(role,'queue-command',windowTitle='',command='VAssistX.VAMemoryView' if role=='VA' else '402');save(role+'.memory103.open.json',v);time.sleep(.3);q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.memory103.tree.json')]);q.wait(timeout=15);r=json.loads((OUT/(role+'.memory103.tree.json')).read_text(encoding='utf-8-sig'));print(role,[(x['type'],x['name'],x['id'],x.get('value')) for x in r if x['type']=='ControlType.Edit' or x['type']=='ControlType.Text' and 'emory' in x['name']][:12],flush=True)
