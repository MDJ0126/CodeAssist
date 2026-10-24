import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module,send
label='Highlight matching words when caret is in _comments and strings';title='Visual Assist Options';journal=[]
def opt(op,**kw):
 for n in range(20):
  v,_=module('VA',op,windowTitle=title,**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
try:
 module('VA','open-options',windowTitle='');v=opt('options-read',page='Highlighting');save('VA.matching-comment.original.json',v);original=next(x['value'] for x in v['result']['controls'] if x['name']==label and 'CheckBox' in x['type']);journal.append(dict(label=label,original=original));save('matching-comment.transaction.json',journal);opt('options-toggle',toggle=label,value=True);opt('options-button',button='Save')
 text='// auditword103 auditword103 auditword103\r\n// other comment\r\n'
 for role in ['VA','CA']:
  for state,caret in [('baseline',text.index('other')+2),('active',text.index('auditword103')+3),('restored',text.index('other')+2)]:
   setup(role,'matching-comment',text,caret);time.sleep(.3);v,_=module(role,'marker-state',windowTitle='');save(role+'.matching-comment.'+state+'.markers.json',v);v,_=module(role,'character-bounds',windowTitle='',offsets=[text.index('auditword103',4)]);save(role+'.matching-comment.'+state+'.bounds.json',v);v,_=send(role,{});save(role+'.matching-comment.'+state+'.render.json',v)
  print(role,'matching comments captured',flush=True)
finally:
 if journal:
  module('VA','open-options',windowTitle='');opt('options-toggle',page='Highlighting',toggle=label,value=original);v=opt('options-read',page='Highlighting');save('VA.matching-comment.restored-options.json',v);opt('options-button',button='Save');journal[0]['restored']=True;save('matching-comment.transaction.json',journal)
