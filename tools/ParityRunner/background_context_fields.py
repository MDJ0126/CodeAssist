import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import send,RUN
text='namespace BarAudit103 {\r\nclass Owner103 {\r\npublic:\r\n    int dataMember;\r\n    int Compute(int input) {\r\n        int localCounter = input + dataMember;\r\n        UnknownSymbol103;\r\n        return localCounter;\r\n    }\r\n};\r\n}\r\n';h=json.loads((RUN/'hosts.json').read_text());save('context-fields.input.json',dict(text=text))
for role in ['VA','CA']:
 setup(role,'context-fields',text,text.index('dataMember')+2)
 for phase,needle in [('field','dataMember'),('local','localCounter'),('unknown','UnknownSymbol103')]:
  v,p=send(role,dict(kind='classification',caret=text.index(needle)+2));save(role+'.context-fields.'+phase+'.probe.json',v);time.sleep(.55);out=OUT/(role+'.context-fields.'+phase+'.tree.json');q=launch(['ui-tree',h[role]['pid'],out,'Parity']);q.wait(timeout=15);r=json.loads(out.read_text(encoding='utf-8-sig'));selected=[x for x in r if x['type'] in ['ControlType.Text','ControlType.ComboBox','ControlType.Edit','ControlType.Button'] and any(k in str(x.get('value') or x['name']) for k in ['Context:','Definition:','Owner103','Compute','dataMember','localCounter','UnknownSymbol103'])];save(role+'.context-fields.'+phase+'.filtered.json',selected);print(role,phase,[(x['type'],x['name'],x.get('value')) for x in selected][-12:],flush=True)
  v,p=send(role,{});save(role+'.context-fields.'+phase+'.render.json',v)
