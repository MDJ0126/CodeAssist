import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module,send
text='void HistoryTarget103() {}\r\n'+('\r\n'*40)+'void HistoryCaller103() { HistoryTarget103(); }\r\n';start=text.rindex('HistoryTarget103')+3
for role in ['VA','CA']:
 setup(role,'navigation-history',text,start)
 for step,command in [('goto','VAssistX.GotoImplementation' if role=='VA' else '256'),('back','VAssistX.NavigateBack' if role=='VA' else '264'),('forward','VAssistX.NavigateForward' if role=='VA' else '265')]:
  v,p=module(role,'queue-command',windowTitle='',command=command,focusEditor=True);save(role+'.navigation-history.'+step+'.queued.json',v);time.sleep(.65);path=(v.get('result') or {}).get('completion');completion=json.loads(Path(path).read_text(encoding='utf-8-sig')) if path and Path(path).exists() else None;print(role,step,completion,flush=True)
  if completion and completion.get('error') and role=='VA' and step in ['back','forward']:
   alt='View.NavigateBackward' if step=='back' else 'View.NavigateForward';v,p=module(role,'queue-command',windowTitle='',command=alt,focusEditor=True);save(role+'.navigation-history.'+step+'.standard-queued.json',v);time.sleep(.5)
  v,p=send(role,dict(kind='classification'));save(role+'.navigation-history.'+step+'.observed.json',v);print(role,step,v['status'],v.get('after',{}).get('file'),v.get('after',{}).get('caret'),flush=True)
