from foreground106 import *
journal={}
text='class KnownOpen107 {};\r\nvoid OpenUnknown107() {\r\n    MissingBeforeEdit107;\r\n}\r\n'
try:
 for role in ['VA','CA']:
  v,_=module(role,'cpp-error-options',windowTitle='');journal[role]=dict(ide=v['result']['values'])
  for key in ['DisableErrorReporting','DisableSquiggles']:
   module(role,'cpp-error-options',windowTitle='',setting=key,value=True)
 module('VA','open-options',windowTitle='');v=option('VA','options-read',page='Underlining');journal['VA']['native']=next(c['value'] for c in v['result']['controls'] if c['name']=='_Underline unrecognized symbols using ');option('VA','options-toggle',toggle='_Underline unrecognized symbols using ',value=True);option('VA','options-button',button='Save')
 for role in ['VA','CA']:
  setup(role,'unknown-not-edited107',text,text.index('MissingBeforeEdit107')+3);physical(role,'unknown-open107-focus','focus');module(role,'arm-markers105',windowTitle='');time.sleep(1.5);observed(role,'unknown-open107');v,_=module(role,'document-clean107',windowTitle='');save(role+'.unknown-open107.clean.json',v);assert v['result']['saved'] and v['result']['equalsDisk'],v;print(role,'unmodified confirmed',flush=True)
finally:
 for role,j in journal.items():
  for key in ['DisableErrorReporting','DisableSquiggles']:module(role,'cpp-error-options',windowTitle='',setting=key,value=j['ide'][key])
  j['ideRestored']=True
 if 'native'in journal.get('VA',{}):
  module('VA','open-options',windowTitle='');option('VA','options-read',page='Underlining');option('VA','options-toggle',toggle='_Underline unrecognized symbols using ',value=journal['VA']['native']);option('VA','options-button',button='Save');journal['VA']['nativeRestored']=True
 save('unknown-open107.transaction.json',journal)
