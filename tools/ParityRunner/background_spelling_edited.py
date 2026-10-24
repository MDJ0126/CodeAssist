import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module,send
journal=[]
def opt(role,op,**kw):
 for n in range(20):
  v,_=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
text='// This mispellng wrod needs correction.\r\nconst char* text = "mispellng wrod";\r\nint main() { return 0; }\r\n'
try:
 for role in ['VA','CA']:
  page='Underlining' if role=='VA' else 'Guides';label='Underline _spelling errors in comments and strings using ' if role=='VA' else 'Underline comment and string spelling'
  module(role,'open-options',windowTitle='');v=opt(role,'options-read',page=page);save(role+'.spelling.original-options.json',v);original=next(x['value'] for x in v['result']['controls'] if x['name']==label and 'CheckBox' in x['type']);journal.append(dict(role=role,page=page,label=label,original=original));save('spelling.transaction.json',journal)
  for state,enabled in [('off',False),('on',True)]:
   if state=='on':module(role,'open-options',windowTitle='')
   opt(role,'options-toggle',page=page,toggle=label,value=enabled);opt(role,'options-button',button='Save');setup(role,'spelling-edited',text,text.index('mispellng'));module(role,'posted-text',windowTitle='',text='mispellng wrod ');module(role,'caret-set',windowTitle='',offset=text.index('main')+16);time.sleep(2);v,_=module(role,'marker-state',windowTitle='');save(role+'.spelling-edited.'+state+'.markers.json',v);v,_=module(role,'character-bounds',windowTitle='',offsets=[text.index('mispellng'),text.index('wrod')]);save(role+'.spelling-edited.'+state+'.bounds.json',v);v,_=send(role,{});save(role+'.spelling-edited.'+state+'.render.json',v)
  print(role,'spelling off/on captured',flush=True)
finally:
 for j in reversed(journal):
  try:
   module(j['role'],'open-options',windowTitle='');opt(j['role'],'options-toggle',page=j['page'],toggle=j['label'],value=j['original']);opt(j['role'],'options-button',button='Save');j['restored']=True
  except Exception as error:j['restoreError']=str(error)
 save('spelling.transaction.json',journal)
