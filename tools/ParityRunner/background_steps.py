import sys,json,subprocess,time,os
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module,send,RUN
ROOT=Path.cwd();OUT=ROOT/'docs/validation/background-103';PY=sys.executable;WORKER=ROOT/'tools/ShortcutProbe/BackgroundOptionsV35.exe'
def save(name,v):(OUT/name).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def launch(args):
 hosts=json.loads((RUN/'hosts.json').read_text());foreground=any(str(v['pid'])==str(args[1]) and v.get('desktop')=='Default' for v in hosts.values());env=os.environ.copy()
 if foreground:env['CODEASSIST_FOREGROUND_VALIDATION']='1'
 worker=ROOT/'tools/ShortcutProbe/BackgroundForeground108e.exe' if foreground else WORKER
 cmd=[str(worker),*map(str,args)] if foreground else [PY,str(ROOT/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(worker),*map(str,args)]
 return subprocess.Popen(cmd,env=env,creationflags=0x08000000,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
def setup(role,id,text,start,length=0,file=None,extras=None):
 hosts=json.loads((RUN/'hosts.json').read_text());p=OUT/(role+'.'+id+'.prepared.case.json');save(p.name,dict(id=id,file=file or id+'.cpp',text=text,start=start,length=length,inspectionOnly=True,undo=False,vaCommand='',caCommand='',**({'extras':extras} if extras else {})));out=OUT/(role+'.'+id+'.prepared.json');proc=launch(['case',hosts[role]['pid'],out,p,role]);proc.wait(timeout=15);r=json.loads(out.read_text(encoding='utf-8-sig'));assert r['status']=='complete' and r['after']['text']==text
 target,_=module(role,'focus-target-info',windowTitle='');assert target['status']=='complete' and target['result']['file']==r['after']['file'],target
 focus=OUT/(role+'.'+id+'.focus-hwnd.case.json');save(focus.name,dict(hwnd=target['result']['sourceHwnd']));p=launch(['focus-hwnd',hosts[role]['pid'],OUT/(role+'.'+id+'.focus-hwnd.result.json'),focus]);p.wait(timeout=10)
 v,_=module(role,'focus-document-view',windowTitle='');save(role+'.'+id+'.focused-view.json',v)
 if v['status']=='complete' and not v['result'].get('hasAggregateFocus') and hosts[role].get('desktop')=='Default' and not globals().get('BACKGROUND_ONLY',False):
  module(role,'cancel-menus',windowTitle='');req=OUT/(role+'.'+id+'.physical-focus.spec.json');observed=OUT/(role+'.'+id+'.physical-focus.json');save(req.name,dict(pid=hosts[role]['pid'],action='focus',output=str(observed.resolve())));z=subprocess.run([str(ROOT/'tools/ShortcutProbe/SingleCaseProbe.exe'),'physical-step106',str(req.resolve())],capture_output=True,timeout=20,creationflags=0x08000000);save(role+'.'+id+'.physical-focus.worker.json',dict(exitCode=z.returncode,stderr=z.stderr.decode(errors='replace')));assert z.returncode==0
  v,_=module(role,'focus-document-view',windowTitle='');save(role+'.'+id+'.focused-view-retry.json',v)
 if globals().get('BACKGROUND_ONLY',False) and not v['result'].get('hasAggregateFocus'):save(role+'.'+id+'.focus-scope.json',dict(physicalFocusVerified=False,scope='In-process editor command and owned buffer inspection only; physical input gate excluded'))
 else:assert v['status']=='complete' and v['result'].get('hasAggregateFocus'),v
 v,_=module(role,'view-state',windowTitle='');save(role+'.'+id+'.view-state.json',v);view=v['result']['views'][0];assert view['file']==r['after']['file'] and view['sample']==text[:160] and view['caret']==start+length,(v,r['after'])
 return r
if __name__=='__main__':
 text='class EncapAudit {\r\npublic:\r\n    int value;\r\n    int Read() { return value; }\r\n};\r\n';r=setup('VA','encapsulate',text,text.index('value')+2);v,p=module('VA','queue-command',windowTitle='',command='VAssistX.RefactorEncapsulateField',focusEditor=True);save('VA.encapsulate.queued.json',v);time.sleep(.8);hosts=json.loads((RUN/'hosts.json').read_text());q=launch(['ui-tree',hosts['VA']['pid'],OUT/'VA.encapsulate.dialog-tree.json']);q.wait(timeout=15);print('VA encapsulate opened',flush=True)

def focus_existing(role,id,file=None):
 hosts=json.loads((RUN/'hosts.json').read_text())
 if file is not None:
  q=launch(['activate-document',hosts[role]['pid'],OUT/(role+'.'+id+'.activate.json'),file]);q.wait(timeout=15)
 target,_=module(role,'focus-target-info',windowTitle='');assert target['status']=='complete',target
 if file is not None:assert Path(target['result']['file']).resolve()==Path(file).resolve(),target
 spec=OUT/(role+'.'+id+'.focus.case.json');save(spec.name,dict(hwnd=target['result']['sourceHwnd']));q=launch(['focus-hwnd',hosts[role]['pid'],OUT/(role+'.'+id+'.focus.result.json'),spec]);q.wait(timeout=10)
 value,_=module(role,'focus-document-view',windowTitle='');save(role+'.'+id+'.focused.json',value);assert value['status']=='complete' and (value['result']['hasAggregateFocus'] or globals().get('BACKGROUND_ONLY',False)),value
 value,_=module(role,'view-state',windowTitle='');save(role+'.'+id+'.state.json',value);assert value['result']['views'][0]['file']==target['result']['file'];return value
