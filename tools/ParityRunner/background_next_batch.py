import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner')
from background_steps import setup,save,launch,OUT
from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text())
text='void DeclarationProbe() {}\r\n'+('\r\n'*40)+'void Caller(){ DeclarationProbe(); }\r\n'
for role in ['VA','CA']:
 setup(role,'navigation-history-warm',text,text.rindex('DeclarationProbe')+3,file='goto-implementation.cpp')
 for step,cmd in [('goto','VAssistX.GotoImplementation' if role=='VA' else '256'),('back','VAssistX.NavigateBack' if role=='VA' else '264'),('forward','VAssistX.NavigateForward' if role=='VA' else '265')]:
  v,p=module(role,'queue-command',windowTitle='',command=cmd,focusEditor=True);save(role+'.history-warm.'+step+'.queued.json',v);time.sleep(.7)
  v,p=send(role,dict(kind='classification'));save(role+'.history-warm.'+step+'.observed.json',v)
  print(role,step,v.get('after',{}).get('file'),v.get('after',{}).get('caret'),flush=True)
text='namespace BarAudit103 {\r\nclass Owner103 {\r\npublic:\r\n int dataMember;\r\n int Compute(int input) {\r\n int localCounter = input + dataMember;\r\n UnknownSymbol103;\r\n return localCounter;\r\n }\r\n};\r\n}\r\n'
for role in ['VA','CA']:
 for phase,needle in [('field','dataMember'),('local','localCounter'),('unknown','UnknownSymbol103')]:
  setup(role,'context-dte-'+phase,text,text.index(needle)+2,file='context-fields.cpp');time.sleep(.6)
  out=OUT/(role+'.context-dte.'+phase+'.tree.json');q=launch(['ui-tree',h[role]['pid'],out,'Parity']);q.wait(timeout=15)
  rows=json.loads(out.read_text(encoding='utf-8-sig'));vals=[(x['type'],str(x.get('value') or x['name'])[:130]) for x in rows if x['type']=='ControlType.ComboBox' or x['name'].startswith(('Context:','Definition:'))]
  save(role+'.context-dte.'+phase+'.values.json',vals);print(role,phase,vals[:8],flush=True)
