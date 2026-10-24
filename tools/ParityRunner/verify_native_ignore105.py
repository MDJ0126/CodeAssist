import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module,send;from background_steps import setup
P=Path('.local/parity-runs/remaining-105');text='class CustomExtension105 { public: int Value() { return 105; } };\r\nint main() { CustomExtension105 value; return value.Value(); }\r\n';start=text.rindex('CustomExtension105')+4;j={}
def opt(op,**kw):
 for n in range(30):
  v,_=module('VA',op,windowTitle='Visual Assist Options',**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
def lookup(state):
 setup('VA','custom-extension105-'+state,text,start,file='custom-extension105.ca105');module('VA','queue-command',windowTitle='',command='VAssistX.ReparseCurrentFile',focusEditor=True);time.sleep(.4);module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);time.sleep(.5);v,_=send('VA',dict(kind='classification'));(P/('VA.extension-'+state+'.actual.json')).write_text(json.dumps(v,ensure_ascii=False,indent=2));print(state,v['after']['caret'],v['after']['selectionStart'],flush=True);return v
try:
 module('VA','open-options',windowTitle='');v=opt('options-read',page='Projects and Files');fields=[x for x in v['result']['controls'] if x['type']=='TextBox' and x['bounds']['x']>245];assert len(fields)==1;original=fields[0]['value'];assert original=='';j['original']=original;(P/'VA.ignore-extension.transaction.json').write_text(json.dumps(j));opt('options-edit',setting='Extensions105',oldText=original,newText='.ca105');opt('options-button',button='Save');v=lookup('ignored');assert v['after']['caret']==start,v
finally:
 if 'original' in j:
  module('VA','open-options',windowTitle='');opt('options-read',page='Projects and Files');opt('options-edit',setting='Extensions105',oldText='.ca105',newText=j['original']);opt('options-button',button='Save');j['restored']=True;(P/'VA.ignore-extension.transaction.json').write_text(json.dumps(j));v=lookup('ignore-restored');assert v['after']['selectionStart']==6 and v['after']['selectedText']=='CustomExtension105',v
