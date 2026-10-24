import sys,json,time,subprocess,uuid
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT
from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());text='int Keep108(){return 3;}\r\n\r\nint Moved108(){return 7;}\r\n\r\nint Caller108(){return Keep108()+Moved108();}\r\n';start=text.index('int Moved');end=text.index('\r\n\r\nint Caller')
results={}
for role in ['VA','CA']:
 try:
  setup(role,'selection-new108c',text,start,end-start);time.sleep(2);v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorMoveSelectionToNewFile'if role=='VA'else'372',focusEditor=True);save(role+'.selection-new.queued.json',v);time.sleep(.4);target=RUN/role/('selection-new108-'+uuid.uuid4().hex[:8]+'.h');result=OUT/(role+'.selection-new.submit.json');r=subprocess.run(['tools/ParityRunner/OwnedNewFile108.exe',str(h[role]['pid']),role,str(target),str(result)],capture_output=True,timeout=15,creationflags=0x08000000);assert r.returncode==0,result.read_text();time.sleep(.5);values={}
  for name in ['selection-new108c.cpp',target.name]:
   f=OUT/(role+'.selection-new.'+name+'.json');w=launch(['read-document',h[role]['pid'],f,RUN/role/name]);w.wait(timeout=10);values[name]=json.loads(f.read_text());
  save(role+'.selection-new.actual.json',{'target':str(target),'documents':values});print(24,role,'target exists',target.exists(),'moved function',any('int Moved108'in str(x.get('text'))for n,x in values.items()if n!= 'selection-new108c.cpp'),flush=True)
  results[role]={'status':'completed'}
 except Exception as error:
  results[role]={'status':'error','error':str(error)};print(role,str(error),flush=True)
 save('selection-new.per-role.json',results)
