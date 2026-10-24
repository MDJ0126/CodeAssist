import json,sys,subprocess,time,concurrent.futures
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import send,module,RUN
ROOT=Path.cwd();OUT=ROOT/'docs/validation/background-103';PY=sys.executable;hosts=json.loads((RUN/'hosts.json').read_text())
def save(name,v):(OUT/name).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
text='void SnippetProbe() {\r\n    for\r\n}\r\n';case=dict(id='snippet-for-shortcut',feature=84,file='snippet-for-shortcut.cpp',text=text,start=text.index('for')+3,length=0,inspectionOnly=True,undo=False,vaCommand='',caCommand='');save('snippet-for-shortcut.case.json',case)
for role in ['VA','CA']:
 spec=OUT/'snippet-for-shortcut.case.json';out=OUT/(role+'.snippet-for-shortcut.setup.json')
 p=subprocess.Popen([PY,str(ROOT/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(ROOT/'tools/ShortcutProbe/BackgroundOptions.exe'),'case',str(hosts[role]['pid']),str(out),str(spec),role],creationflags=0x08000000,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);p.wait(timeout=15);assert json.loads(out.read_text(encoding='utf-8-sig'))['status']=='complete'
 if role=='VA':v,path=send(role,dict(kind='input',action='tab'),timeout=10);save(role+'.snippet-for-shortcut.input.json',v)
 else:
  with concurrent.futures.ThreadPoolExecutor(1) as pool:
   task=pool.submit(send,role,dict(kind='input',action='tab'),25);time.sleep(1)
   if not task.done():
    v,path=module(role,'snippet-insert',windowTitle='CodeAssist: Snippets',fields={'Index':'i','Count':'3'});save(role+'.snippet-for-shortcut.dialog.json',v)
   v,path=task.result();save(role+'.snippet-for-shortcut.input.json',v)
 v,path=send(role,{});save(role+'.snippet-for-shortcut.render.json',v)
 print(role,'snippet done',flush=True)
