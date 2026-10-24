import sys,json,subprocess,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');import foreground106 as f
from background_steps import setup,save,OUT
from background_client import RUN,module,send
h=json.loads((RUN/'hosts.json').read_text())
def act(role,label,action,**kwargs):
 req=OUT/(role+'.'+label+'.spec.json');out=OUT/(role+'.'+label+'.input.json');save(req.name,dict(pid=h[role]['pid'],action=action,output=str(out),**kwargs));w=subprocess.Popen(['tools/ShortcutProbe/SingleCaseProbe.exe','physical-step106',str(req)],stdout=subprocess.PIPE,stderr=subprocess.PIPE,creationflags=0x08000000);stdout,stderr=w.communicate();save(role+'.'+label+'.worker.json',{'exitCode':w.returncode,'stdout':stdout.decode(errors='replace'),'stderr':stderr.decode(errors='replace')});assert w.returncode==0;v=json.loads(out.read_text());cleanup=json.loads(Path(str(out)+'.cleanup.json').read_text());save(role+'.'+label+'.cleanup.json',cleanup)
 if action in ['paste','history']:assert cleanup['clipboardRestored'],cleanup
 return v
payload='if(true){\r\nint value=1;\r\nvalue++;\r\n}'
for role in ['VA','CA']:
 text='void Paste108() {\r\n    \r\n}\r\n';setup(role,'paste108',text,text.index('    \r\n')+4);v=act(role,'paste108','paste',text=payload);save(role+'.paste108.result.json',v);u=act(role,'paste108-undo','keys',keys='CTRL+Z');save(role+'.paste108.undo.json',u);assert u['after']['text']==text;print(45,role,'paste and undo completed',flush=True)
for role in ['VA','CA']:
 text='// ClipboardAlpha106\r\n// ClipboardBeta106\r\n\r\n';setup(role,'clipboard108',text,len(text));v=act(role,'history108','history',native=role=='VA');save(role+'.history108.result.json',v);assert v['after']['text']!=text;print(48,role,'history inserted',repr(v['after']['text'][-40:]),flush=True)
