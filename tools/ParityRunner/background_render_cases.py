import json,sys,subprocess,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import send,RUN
ROOT=Path.cwd();OUT=ROOT/'docs/validation/background-103';PY=sys.executable;hosts=json.loads((RUN/'hosts.json').read_text())
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
cases=[('matched-brace',71,'void BraceAudit() {\r\n    int count = (1 + 2);\r\n}\r\n','(1',1),('mismatched-brace',72,'void BrokenBraceAudit() {\r\n    int count = (1 + 2;\r\n}\r\n','(1',1),('current-line',7,'void LineAudit() {\r\n    int first = 1;\r\n    int second = 2;\r\n}\r\n','second',0)]
for id,feature,text,needle,delta in cases:
 spec=OUT/(id+'.render.case.json');save(spec,dict(id=id,feature=feature,file=id+'.cpp',text=text,start=text.index(needle)+delta,length=0,inspectionOnly=True,undo=False,vaCommand="",caCommand=""))
 for role in ['VA','CA']:
  out=OUT/(role+'.'+id+'.render-setup.json')
  p=subprocess.Popen([PY,str(ROOT/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(ROOT/'tools/ShortcutProbe/BackgroundOptions.exe'),'case',str(hosts[role]['pid']),str(out),str(spec),role],creationflags=0x08000000,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);p.wait(timeout=15)
  setup=json.loads(out.read_text(encoding='utf-8-sig'));assert setup['status']=='complete'
  time.sleep(.8);v,path=send(role,dict(kind='classification'));save(OUT/(role+'.'+id+'.probe.json'),v);v,path=send(role,{});save(OUT/(role+'.'+id+'.render.json'),v)
  if feature==7:
   v,path=send(role,dict(kind='classification',caret=text.index('first')));save(OUT/(role+'.'+id+'.moved-probe.json'),v);time.sleep(.4);v,path=send(role,{});save(OUT/(role+'.'+id+'.moved-render.json'),v)
 print(id,'captured both',flush=True)
