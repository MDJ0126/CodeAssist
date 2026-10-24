import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());text='class UsageAudit103\r\n{\r\npublic:\r\n    void Run() { MissingAction103(7); }\r\n};\r\n'
for role in ['VA','CA']:
 setup(role,'create-from-usage',text,text.index('MissingAction103')+3)
 v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorCreateFromUsage' if role=='VA' else '268',focusEditor=True);save(role+'.create-from-usage.queued.json',v)
 time.sleep(.5);q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.create-from-usage.tree.json')]);q.wait(timeout=15)
 r=json.loads((OUT/(role+'.create-from-usage.tree.json')).read_text(encoding='utf-8-sig'));print(role,[(x['name'],x['id'],x.get('value')) for x in r[:14]],flush=True)
