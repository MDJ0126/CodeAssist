import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module,send;from background_steps import setup,save
journal=[]
def opt(role,op,**kw):
 for n in range(25):
  v,_=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
  if v.get('error'):raise RuntimeError(v['error'])
  if (v.get('result') or {}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
text='int main()\r\n{\r\n    \r\n    return 0;\r\n}\r\n';start=text.index('    \r\n')+4
try:
 for role in ['VA','CA']:
  v,_=module(role,'cpp-error-options',windowTitle='');old=v['result']['values']['DisableSquiggles'];journal.append(dict(role=role,ideFlag=old));v,_=module(role,'cpp-error-options',windowTitle='',setting='DisableSquiggles',value=True);save(role+'.unknown-symbol.ide-disabled.json',v);assert v['result']['values']['DisableSquiggles'] is True
 for role in ['VA','CA']:
  states=[('off',False),('on',True)] if role=='VA' else [('on',True)]
  for state,enabled in states:
   if role=='VA':
    module(role,'open-options',windowTitle='');v=opt(role,'options-read',page='Underlining')
    if not any('nativeOriginal' in j for j in journal):journal[0]['nativeOriginal']=next(c['value'] for c in v['result']['controls'] if c['name']=='_Underline unrecognized symbols using ' and 'CheckBox' in c['type']);save('unknown-symbol.transaction.json',journal)
    opt(role,'options-toggle',toggle='_Underline unrecognized symbols using ',value=enabled);opt(role,'options-button',button='Save')
   setup(role,'unknown-edited-'+state,text,start);v,_=module(role,'posted-text',windowTitle='',text='MissingBareSymbol103; ');save(role+'.unknown-symbol.'+state+'.typed.json',v);body=v['result']['after']['text'];module(role,'caret-set',windowTitle='',offset=body.index('return')+3);time.sleep(1.1);v,_=module(role,'marker-state',windowTitle='');save(role+'.unknown-symbol.'+state+'.markers.json',v);v,_=module(role,'character-bounds',windowTitle='',offsets=[start,start+19]);save(role+'.unknown-symbol.'+state+'.bounds.json',v);v,_=send(role,{});save(role+'.unknown-symbol.'+state+'.render.json',v);print(role,state,'captured',flush=True)
finally:
 for j in journal:
  try:
   v,_=module(j['role'],'cpp-error-options',windowTitle='',setting='DisableSquiggles',value=j['ideFlag']);j['ideRestored']=v['result']['values']['DisableSquiggles']==j['ideFlag']
   if 'nativeOriginal' in j:
    module('VA','open-options',windowTitle='');opt('VA','options-read',page='Underlining');opt('VA','options-toggle',toggle='_Underline unrecognized symbols using ',value=j['nativeOriginal']);opt('VA','options-button',button='Save');j['nativeRestored']=True
  except Exception as e:j['restoreError']=str(e)
 save('unknown-symbol.transaction.json',journal);print(journal,flush=True)
