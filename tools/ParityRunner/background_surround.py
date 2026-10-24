import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import send,module
text='int SurroundAudit103() { return 1 + 2; }\r\n';start=text.index('1 + 2')
for role in ['VA','CA']:
 setup(role,'surround-parentheses',text,start,length=5)
 v,p=send(role,dict(kind='input',action='typechar',character='(',expectedFile=setup.__globals__['RUN'].__str__()+'\\'+role+'\\surround-parentheses.cpp'));save(role+'.surround-parentheses.input.json',v);print(role,v['status'],repr(v.get('after',{}).get('text')),flush=True)
 module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.35);v,p=send(role,dict(kind='classification'));save(role+'.surround-parentheses.undo.json',v)
