import sys,json,time,shutil
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,OUT;from background_client import module,send
labels=['Local symbols in bold','System symbols in italics'];journal=[]
def mod(role,op,**kw):
 title='Visual Assist Options' if role=='VA' else 'CodeAssist Options';v,p=module(role,op,windowTitle=title,**kw)
 for i in range(8):
  if v.get('result',{}).get('status')!='window-not-found':break
  time.sleep(.2);v,p=module(role,op,windowTitle=title,**kw)
 assert v['status']=='complete' and v.get('result',{}).get('status')=='observed',str(v);return v
text='#include <vector>\r\nvoid StyleAttributeAudit()\r\n{\r\n    int localCounter = 1;\r\n    std::vector<int> values;\r\n    values.push_back(localCounter);\r\n}\r\n'
def capture(role,phase):
 setup(role,'style-attributes',text,text.index('{')+1);time.sleep(1.2);v,p=send(role,dict(kind='classification'));save(role+'.style-'+phase+'.probe.json',v);v,p=send(role,{});save(role+'.style-'+phase+'.render.json',v);image=next(x['path'] for x in v['rows'] if x['label']=='editor');shutil.copy2(image,OUT/'render-evidence'/(role+'-style-'+phase+'.png'))
module('VA','open-options',windowTitle='Visual Assist Options');v=mod('VA','options-read',page='Coloring');save('VA.style-native-options.json',v);mod('VA','options-button',button='Cancel')
capture('VA','native-default');capture('CA','before-enable')
try:
 module('CA','open-options',windowTitle='CodeAssist Options');v=mod('CA','options-read',page='Coloring');save('CA.style-original-options.json',v)
 for label in labels:
  row=next(c for c in v['result']['controls'] if c.get('name')==label and 'CheckBox' in c['type']);journal.append(dict(label=label,original=row['value']));save('style-preference-transaction.json',dict(state='in-progress',entries=journal));mod('CA','options-toggle',toggle=label,value=True)
 mod('CA','options-button',button='Save');capture('CA','after-enable')
finally:
 if journal:
  module('CA','open-options',windowTitle='CodeAssist Options')
  for row in journal:mod('CA','options-toggle',page='Coloring',toggle=row['label'],value=row['original'])
  v=mod('CA','options-read',page='Coloring');save('CA.style-restored-options.json',v);mod('CA','options-button',button='Save')
  for row in journal:row['restored']=next(c for c in v['result']['controls'] if c.get('name')==row['label'] and 'CheckBox' in c['type'])['value']==row['original']
 save('style-preference-transaction.json',dict(state='restored' if all(x.get('restored') for x in journal) else 'restore-required',entries=journal))
print('Style captures complete; original options restored',flush=True)
