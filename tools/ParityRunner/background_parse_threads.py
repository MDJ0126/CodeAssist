import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from background_client import module
journal=[]
def opt(role,op,**kw):
 for n in range(20):
  v,_=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
try:
 for role in ['VA','CA']:
  module(role,'open-options',windowTitle='');v=opt(role,'options-read',page='Performance');save(role+'.parse-threads.original.json',v);values=[str(x['value']) for x in v['result']['controls'] if x['type'] in ['TextBox','ComboBox'] and str(x.get('value','')).isdigit() and int(x['value'])<=16];assert len(values)==1,values;old=values[0];j=dict(role=role,original=old);journal.append(j);save('parse-threads.transaction.json',journal)
  opt(role,'options-edit',oldText=old,newText='1',setting='ParseConcurrency');opt(role,'options-button',button='Save');module(role,'open-options',windowTitle='');v=opt(role,'options-read',page='Performance');save(role+'.parse-threads.saved-readback.json',v);assert any(str(x.get('value'))=='1' and x['type'] in ['TextBox','ComboBox'] for x in v['result']['controls']);opt(role,'options-button',button='Cancel');print(role,'saved and read back1',flush=True)
finally:
 for j in reversed(journal):
  try:
   role=j['role'];module(role,'open-options',windowTitle='');opt(role,'options-read',page='Performance');opt(role,'options-edit',oldText='1',newText=j['original'],setting='ParseConcurrency');opt(role,'options-button',button='Save');module(role,'open-options',windowTitle='');v=opt(role,'options-read',page='Performance');save(role+'.parse-threads.restored.json',v);opt(role,'options-button',button='Cancel');j['restored']=any(str(x.get('value'))==j['original'] and x['type'] in ['TextBox','ComboBox'] for x in v['result']['controls'])
  except Exception as error:j['restoreError']=str(error)
 save('parse-threads.transaction.json',journal)
