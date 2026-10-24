import json,subprocess,time,sys
from pathlib import Path
sys.path.insert(0,str(Path('tools/ParityRunner').resolve()))
from background_client import module,send,RUN
ROOT=Path.cwd(); OUT=ROOT/'docs/validation/background-103'; PY=sys.executable
hosts=json.loads((RUN/'hosts.json').read_text())
def save(name,v): (OUT/name).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def worker(role,spec,name,wait=True):
 p=subprocess.Popen([PY,str(ROOT/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(ROOT/'tools/ShortcutProbe/BackgroundOptions.exe'),'case',str(hosts[role]['pid']),str(OUT/name),str(spec),role],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=0x08000000)
 if wait:p.wait(timeout=18)
 return p
def mod(role,op,**kw):
 v,p=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
 for retry in range(12):
  if v.get('result',{}).get('status')!='window-not-found':break
  time.sleep(.5);v,p=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
 if v.get('status')!='complete' or v.get('result',{}).get('status')!='observed':raise RuntimeError(str(v))
 return v
journal=[];opened={}
try:
 for role in ['VA','CA']:
  module(role,'open-options',windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options');time.sleep(.8)
  # VA options was already open from the supervised setup.
  original=mod(role,'options-read',page='Highlighting');save(role+'.highlight-original.json',original)
  label='Highlight _write and read references to symbol under cursor' if role=='VA' else 'Highlight references under caret'
  matches=[c for c in original['result']['controls'] if c.get('name')==label and 'CheckBox' in c.get('type','')]
  if len(matches)!=1:raise RuntimeError('Missing exact toggle '+role+' '+label)
  journal.append(dict(role=role,label=label,original=matches[0]['value']))
  save('preference-transaction.json',dict(state='in-progress',entries=journal))
  changed=mod(role,'options-toggle',toggle=label,value=True);save(role+'.highlight-enable.json',changed)
  mod(role,'options-button',button='Save');time.sleep(.8)
  if role=='VA':
   q=subprocess.Popen([PY,str(ROOT/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(ROOT/'tools/ShortcutProbe/BackgroundOptionsV2.exe'),'drive',str(hosts[role]['pid']),str(OUT/'VA.references-enabled.conflict-no.json'),str(OUT/'native-reference-conflict.no.json')],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=0x08000000);q.wait(timeout=13)
  if role in opened:opened[role].wait(timeout=8)
 text='class ReferenceAudit { public: int value; void Update() { value = 3; } };\r\n'
 for role in ['VA','CA']:
  case=dict(id='references-enabled',feature=64,file='references-enabled.cpp',text=text,start=text.index('value')+2,length=0,inspectionOnly=True,undo=False,vaCommand='',caCommand='')
  spec=OUT/'references-enabled.case.json';save(spec.name,case);worker(role,spec,role+'.references-enabled.setup.json');time.sleep(2)
  value,path=send(role,dict(kind='classification'));save(role+'.references-enabled.probe.json',value)
  value,path=send(role,{});save(role+'.references-enabled.render.json',value)
finally:
 for entry in reversed(journal):
  role=entry['role']
  try:
   module(role,'open-options',windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options');time.sleep(.8)
   mod(role,'options-toggle',page='Highlighting',toggle=entry['label'],value=entry['original'])
   restored=mod(role,'options-read',page='Highlighting');save(role+'.highlight-restored.json',restored)
   mod(role,'options-button',button='Save');entry['restored']=True
  except Exception as ex:entry['restoreError']=str(ex)
 save('preference-transaction.json',dict(state='restored' if all(x.get('restored') for x in journal) else 'restore-required',entries=journal))
print('Preference transaction and reference highlight probes finished',flush=True)
