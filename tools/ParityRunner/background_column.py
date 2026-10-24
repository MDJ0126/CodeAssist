import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module,send
journal=[]
TARGET='40'
def opt(role,op,**kw):
 title='Visual Assist Options' if role=='VA' else 'CodeAssist Options'
 for n in range(20):
  v,p=module(role,op,windowTitle=title,**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.15)
 raise RuntimeError(str(v))
text='// column guide audit 103\r\n'+(' '*120+'// end\r\n')*8
try:
 for role in ['VA','CA']:
  module(role,'open-options',windowTitle='');v=opt(role,'options-read',page='Display');save(role+'.column.original-options.json',v)
  label='_Display indicator after column ' if role=='VA' else 'Show column indicator';toggles=[x for x in v['result']['controls'] if x.get('name')==label and 'CheckBox' in x['type']];assert len(toggles)==1
  numeric=[x['value'] for x in v['result']['controls'] if x['type'] in ['TextBox','ComboBox'] and str(x.get('value','')).isdigit()];assert len(numeric)==1,numeric
  j=dict(role=role,label=label,enabled=toggles[0]['value'],column=numeric[0]);journal.append(j);save('column.transaction.json',journal)
  opt(role,'options-toggle',toggle=label,value=False);opt(role,'options-button',button='Save');setup(role,'column-guide',text,0);v,p=send(role,{});save(role+'.column.off-render.json',v)
  module(role,'open-options',windowTitle='');opt(role,'options-toggle',page='Display',toggle=label,value=True)
  if numeric[0]!=TARGET:opt(role,'options-edit',oldText=numeric[0],newText=TARGET)
  opt(role,'options-button',button='Save');setup(role,'column-guide',text,0);time.sleep(.5);v,p=send(role,{});save(role+'.column.on-render.json',v);print(role,'column capture',[(x['label'],x['status']) for x in v['rows']],flush=True)
finally:
 for j in reversed(journal):
  role=j['role']
  try:
   module(role,'open-options',windowTitle='');opt(role,'options-toggle',page='Display',toggle=j['label'],value=j['enabled'])
   if j['column']!=TARGET:opt(role,'options-edit',oldText=TARGET,newText=j['column'])
   v=opt(role,'options-read',page='Display');save(role+'.column.restored-options.json',v);opt(role,'options-button',button='Save');j['restored']=True
  except Exception as e:j['restoreError']=str(e)
 save('column.transaction.json',journal)
