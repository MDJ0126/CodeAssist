import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module,send
h=json.loads((RUN/'hosts.json').read_text());title='Visual Assist Options';journal=[]
def opt(op,**kw):
 for n in range(20):
  v,_=module('VA',op,windowTitle=title,**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
try:
 module('VA','open-options',windowTitle='');v=opt('options-read',page='Enhanced Listboxes');save('VA.completion-source.original.json',v);original=next(x['value'] for x in v['result']['controls'] if x['name']=='_Visual Assist');journal.append(dict(originalVisualAssist=original));save('completion-source.transaction.json',journal);opt('options-toggle',toggle='_Visual Assist',value=True);opt('options-button',button='Save')
 cases=[('smart-bool','bool SmartBoolAudit103() { bool value = ; return value; }\r\n','; return','tru'),('suggest-recent','int SuggestRecentAudit103() { int nearbyValue103 = 7; return ; }\r\n','; }','nearbyVal')]
 for id,text,marker,typed in cases:
  for role in ['VA','CA']:
   setup(role,id,text,text.index(marker));v,_=module(role,'posted-text',windowTitle='',text=typed);save(role+'.'+id+'.typed.json',v);v,_=module(role,'queue-command',windowTitle='',command='Edit.CompleteWord',focusEditor=True);save(role+'.'+id+'.complete.queued.json',v);time.sleep(.3);q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.'+id+'.completion-tree.json')]);q.wait(timeout=15);v,_=module(role,'render-sources',windowTitle='');save(role+'.'+id+'.popup.json',v);v,_=send(role,dict(kind='input',action='tab'));save(role+'.'+id+'.committed.json',v);print(role,id,v.get('after'),flush=True)
finally:
 if journal:
  module('VA','open-options',windowTitle='');opt('options-toggle',page='Enhanced Listboxes',toggle='_Visual Assist' if original else 'Default _Intellisense',value=True);v=opt('options-read',page='Enhanced Listboxes');save('VA.completion-source.restored.json',v);opt('options-button',button='Save');journal[0]['restored']=True;save('completion-source.transaction.json',journal)
