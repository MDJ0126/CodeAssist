import sys,time,json
sys.path.insert(0,'tools/ParityRunner')
from background_steps import setup,save,launch,OUT
from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text());text='enum class State { Ready, Running, Done };\r\nvoid Probe(State state) { switch (state) { case State::Ready: break; } }\r\n'
for role in ['VA','CA']:
 setup(role,'enum-final108b',text,text.index('switch')+3);time.sleep(2)
 v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorCreateMissingCases'if role=='VA'else'394',focusEditor=True);save(role+'.enum.command.json',v)
 if role=='CA':
  for i,(label,edit) in enumerate([('Qualified scoped enum name','State'),('Enum declaration','enum class State { Ready, Running, Done };')]):
   spec=OUT/(role+str(i)+'.spec.json');spec.write_text(json.dumps(dict(title='CodeAssist',button='Save',edit=edit,requiredText=label)));f=OUT/(role+str(i)+'.drive.json');w=launch(['drive',h[role]['pid'],f,spec]);w.wait(timeout=12);assert json.loads(f.read_text()).get('acted')

  for retry in range(30):
   v,_=module(role,'window-read',windowTitle='Add Missing Case Statements')
   if v.get('result',{}).get('status')=='observed':break
   time.sleep(.1)
  save(role+'.enum.preview.json',v)
  if v.get('result',{}).get('status')=='observed':
   module(role,'arm-owner-focus-restore',windowTitle='Add Missing Case Statements');save(role+'.enum.applied.json',module(role,'options-button',windowTitle='Add Missing Case Statements',button='Apply changes')[0])
  else:
   save(role+'.enum.warning.json',module(role,'window-read',windowTitle='CodeAssist')[0]);module(role,'options-button',windowTitle='CodeAssist',button='확인')
 time.sleep(.2);v,_=send(role,dict(kind='classification'));save(role+'.enum.actual.json',v);print(role,'Running', 'State::Running' in (v.get('after')or{}).get('text',''),flush=True)
