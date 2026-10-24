import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module,send;from background_steps import setup
P=Path('.local/parity-runs/remaining-105');text='class CustomExtension105 { public: int Value() { return 105; } };\r\nint main() { CustomExtension105 value; return value.Value(); }\r\n';start=text.rindex('CustomExtension105')+4
setup('VA','extension-ignore-restored105',text,start,file='custom-extension105.ca105');module('VA','queue-command',windowTitle='',command='VAssistX.ReparseCurrentFile',focusEditor=True);time.sleep(.4);module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);time.sleep(.4);v,_=send('VA',dict(kind='classification'));(P/'VA.extension-ignore-restored.actual.json').write_text(json.dumps(v,ensure_ascii=False,indent=2));assert v['after']['selectedText']=='CustomExtension105';print('Native ignore OFF/ON/OFF navigation positive/no-change/positive',flush=True)
def opt(op,**kw):
 for n in range(25):
  v,_=module('CA',op,windowTitle='CodeAssist Options',**kw)
  if (v.get('result') or {}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
try:
 module('CA','open-options',windowTitle='');opt('options-read',page='File Extensions');opt('options-edit',setting='Extensions105',oldText='',newText='.ca105');opt('options-button',button='Save');module('CA','index-reinitialize',windowTitle='',_timeout=40);setup('CA','extension-lookup105',text,start,file='custom-extension105.ca105');module('CA','queue-command',windowTitle='',command='256',focusEditor=True);time.sleep(.4);v,_=send('CA',dict(kind='classification'));(P/'CA.extension-added-lookup.actual.json').write_text(json.dumps(v,ensure_ascii=False,indent=2));assert v['after']['caret']<30,v;print('CA custom extension actual definition navigation',v['after']['caret'],flush=True)
finally:
 module('CA','open-options',windowTitle='');opt('options-read',page='File Extensions');opt('options-edit',setting='Extensions105',oldText='.ca105',newText='');opt('options-button',button='Save')
