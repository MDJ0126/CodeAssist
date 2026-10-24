import sys,json,time
sys.path.insert(0,'tools/ParityRunner')
from background_steps import setup,save,launch,OUT
from background_client import RUN,module
text='int KeepFunction103() { return 3; }\r\n\r\nint MovedFunction103()\r\n{\r\n    return 7;\r\n}\r\n\r\nint Caller103() { return MovedFunction103()+KeepFunction103(); }\r\n'
start=text.index('int Moved');end=text.index('\r\n\r\nint Caller')
h=json.loads((RUN/'hosts.json').read_text())
for role in ['VA','CA']:
 setup(role,'move-selection-new-file',text,start,end-start)
 v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorMoveSelectionToNewFile' if role=='VA' else '372',focusEditor=True);save(role+'.move-selection-new-file.queued.json',v)
 time.sleep(.3)
 q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.move-selection-new-file.tree.json')]);q.wait(timeout=15)
 print(role,'dialog captured',flush=True)
