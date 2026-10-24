import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import setup,save
journal=[]
def opt(role,op,**kw):
 for n in range(25):
  v,_=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
  if v.get('error'):raise RuntimeError(v['error'])
  if (v.get('result') or {}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
text='class PrefixAudit103\r\n{\r\n    int \r\n};\r\n';start=text.index('int ')+4
try:
 for role in ['VA','CA']:
  module(role,'open-options',windowTitle='');v=opt(role,'options-read',page='Editor');label='_Insert __ after m and Shift' if role=='VA' else 'Insert underscore after m and Shift';original=next(c['value'] for c in v['result']['controls'] if c['name']==label and 'CheckBox' in c['type']);journal.append(dict(role=role,label=label,original=original));save('member-prefix.transaction.json',journal);opt(role,'options-toggle',toggle=label,value=True);opt(role,'options-button',button='Save')
  setup(role,'member-prefix',text,start);v,_=module(role,'posted-text',windowTitle='',text='m');save(role+'.member-prefix.typed.json',v);v,_=module(role,'logical-key',windowTitle='',key=160);save(role+'.member-prefix.shift.json',v);print(role,v,flush=True)
finally:
 for j in reversed(journal):
  try:
   module(j['role'],'open-options',windowTitle='');opt(j['role'],'options-read',page='Editor');opt(j['role'],'options-toggle',toggle=j['label'],value=j['original']);v=opt(j['role'],'options-read');save(j['role']+'.member-prefix.restored-options.json',v);opt(j['role'],'options-button',button='Save');j['restored']=True
  except Exception as e:j['restoreError']=str(e)
 save('member-prefix.transaction.json',journal)
