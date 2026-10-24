from foreground106 import *
journal={};text='/* comment\r\n * body'
try:
 for role,label in [('VA','_Extend multi-line comments'),('CA','Extend multi-line comments')]:
  module(role,'open-options',windowTitle='');v=option(role,'options-read',page='Editor');old=next(c['value'] for c in v['result']['controls'] if c['name']==label and 'CheckBox'in c.get('type',''));journal[role]=dict(original=False,label=label);save('comment107.transaction.json',journal);option(role,'options-toggle',toggle=label,value=True);option(role,'options-button',button='Cancel' if role=='VA' else 'Save');setup(role,'comment-extend107',text,len(text));physical(role,'comment107-focus','focus');v=physical(role,'comment107-enter','keys',keys='ENTER');save(role+'.comment107.result.json',v);print(role,repr(v['after']['text']),flush=True)
finally:
 for role,j in journal.items():
  module(role,'open-options',windowTitle='');option(role,'options-read',page='Editor');option(role,'options-toggle',toggle=j['label'],value=j['original']);option(role,'options-button',button='Save');j['restored']=True
 save('comment107.transaction.json',journal)
