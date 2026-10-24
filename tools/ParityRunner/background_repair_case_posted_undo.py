import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,OUT;from background_client import module,send
journal=[]
def opt(role,op,**kw):
 title='Visual Assist Options' if role=='VA' else 'CodeAssist Options'
 for n in range(20):
  v,p=module(role,op,windowTitle=title,**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.15)
 raise RuntimeError(str(v))
try:
 for role in ['VA','CA']:
  module(role,'open-options',windowTitle='');v=opt(role,'options-read',page='Editor');save(role+'.repair-case-posted-undo.options-original.json',v)
  label='_Repair case' if role=='VA' else 'Repair type name case';matches=[c for c in v['result']['controls'] if c.get('name')==label and 'CheckBox' in c.get('type','')];assert len(matches)==1,(role,label)
  j=dict(role=role,label=label,original=matches[0]['value']);journal.append(j);save('repair-case-posted-undo.transaction.json',journal)
  opt(role,'options-toggle',toggle=label,value=True);opt(role,'options-button',button='Save')
 text='struct RepairAuditType {};\r\nvoid RepairProbe() {  }\r\n';start=text.index('{  }')+2
 for role in ['VA','CA']:
  s=setup(role,'repair-case-posted-undo-enabled',text,start);steps=[]
  v,p=module(role,'posted-text',windowTitle='',text='repairaudittype ');save(role+'.repair-case-posted-undo.posted-input.json',v);assert v['status']=='complete',v
  save(role+'.repair-case-posted-undo.enabled.steps.json',steps);v,p=send(role,dict(kind='classification'));save(role+'.repair-case-posted-undo.enabled.final.json',v);print(role,repr(v['after']['text']),flush=True)
  module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.35);v,p=send(role,dict(kind='classification'));save(role+'.repair-case-posted-undo.after-undo.json',v);print(role,'Undo',repr(v['after']['text']),flush=True)
finally:
 for j in reversed(journal):
  role=j['role']
  try:
   module(role,'open-options',windowTitle='');opt(role,'options-toggle',page='Editor',toggle=j['label'],value=j['original']);v=opt(role,'options-read',page='Editor');save(role+'.repair-case-posted-undo.options-restored.json',v);opt(role,'options-button',button='Save');j['restored']=True
  except Exception as e:j['restoreError']=str(e)
 save('repair-case-posted-undo.transaction.json',journal)
