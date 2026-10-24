import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import setup
P=Path('.local/parity-runs/remaining-105');j={}
def save(n,v):(P/n).write_text(json.dumps(v,ensure_ascii=False,indent=2))
def opt(op,**kw):
 for n in range(30):
  v,_=module('VA',op,windowTitle='Visual Assist Options',**kw)
  if (v.get('result') or {}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
try:
 v,_=module('VA','cpp-error-options',windowTitle='');j['ide']=v['result']['values'];save('VA.unknown105.original-ide.json',v)
 for name in ['DisableSquiggles','DisableErrorSquiggles','DisableErrorReporting']:
  if name in j['ide']:
   v,_=module('VA','cpp-error-options',windowTitle='',setting=name,value=True);assert v['status']=='complete',v
 module('VA','open-options',windowTitle='');v=opt('options-read',page='Underlining');j['native']=next(c['value'] for c in v['result']['controls'] if c['name']=='_Underline unrecognized symbols using ');save('VA.unknown105.transaction.json',j);opt('options-toggle',toggle='_Underline unrecognized symbols using ',value=True);opt('options-button',button='Save')
 text='int main()\r\n{\r\n    int declared105 = 3;\r\n    \r\n    return declared105;\r\n}\r\n';start=text.index('    \r\n')+4;setup('VA','unknown-lowercase105',text,start);module('VA','arm-markers105',windowTitle='');v,_=module('VA','posted-text',windowTitle='',text='declraed105 = 4;');save('VA.unknown105.typed.json',v);module('VA','caret-set',windowTitle='',offset=text.index('return')+20)
 for delay in [3,6]:
  time.sleep(delay);v,_=module('VA','marker-state',windowTitle='');save('VA.unknown105.wait'+str(delay)+'.markers.json',v);print(delay,(v.get('result') or {}).get('markers'),flush=True);v,_=module('VA','legacy-markers105',windowTitle='');save('VA.unknown105.wait'+str(delay)+'.legacy.json',v);print('legacy',v.get('result'),flush=True)
finally:
 if 'ide' in j:
  for name in ['DisableSquiggles','DisableErrorSquiggles','DisableErrorReporting']:
   if name in j['ide']:module('VA','cpp-error-options',windowTitle='',setting=name,value=j['ide'][name])
  j['ideRestored']=True
 if 'native'in j:
  module('VA','open-options',windowTitle='');opt('options-read',page='Underlining');opt('options-toggle',toggle='_Underline unrecognized symbols using ',value=j['native']);opt('options-button',button='Save');j['nativeRestored']=True
 save('VA.unknown105.transaction.json',j)
