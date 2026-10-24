import sys,json,time,subprocess,os,winreg
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT
from background_client import module,send,RUN
keypath=r'Software\Whole Tomato\Visual Assist X\VaNet17'
with winreg.OpenKey(winreg.HKEY_CURRENT_USER,keypath)as k:old,kind=winreg.QueryValueEx(k,'ExtSource')
patched=old if '.ca105;'in old else old+'.ca105;';journal={'original':old,'patched':patched};save('native-extension-config.transaction.json',journal);mapped=False
try:
 v,_=module('VA','extension-mapping105',windowTitle='',action='install');save('VA.extension.mapping-before-restart.json',v);mapped=v.get('status')=='complete';assert mapped,v
 with winreg.OpenKey(winreg.HKEY_CURRENT_USER,keypath,0,winreg.KEY_SET_VALUE)as k:winreg.SetValueEx(k,'ExtSource',0,kind,patched)
 h=json.loads((RUN/'hosts.json').read_text());pid=h['VA']['pid'];v,_=module('VA','owned-host-quit108',windowTitle='');save('VA.extension.restart.quit.json',v);assert v.get('result',{}).get('status')=='quit-queued'
 for i in range(60):
  r=subprocess.run(['powershell.exe','-NoProfile','-Command','if(Get-Process -Id '+str(pid)+' -ErrorAction SilentlyContinue){exit 9}'],capture_output=True,creationflags=0x08000000)
  if r.returncode!=9:break
  time.sleep(.1)
 else:raise RuntimeError('Normal exit did not complete')
 env=os.environ.copy();env['CODEASSIST_FOREGROUND_VALIDATION']='1';r=subprocess.run([sys.executable,'tools/ParityRunner/background_hosts.py','--roles','VA','--skip-configuration','--visible-input-desktop'],env=env,capture_output=True,timeout=35,creationflags=0x08000000);assert r.returncode==0;h=json.loads((RUN/'hosts.json').read_text())
 for mode in ['show-owned','load-capture-package']:
  for i in range(25):
   f=OUT/('VA.extension.'+mode+'.json');w=launch([mode,h['VA']['pid'],f]);w.wait(timeout=15);d=json.loads(f.read_text());
   if d.get('visible')is True or d.get('loaded')is True:break
   time.sleep(.2)
  else:raise RuntimeError('Host not ready')
 text='class NativeConfiguredExt108 { public: int Value(){return 108;} };\r\nint main(){ NativeConfiguredExt108 item;return item.Value();}\r\n';pos=text.rindex('NativeConfiguredExt108')+3;setup('VA','configured-extension108',text,pos,file='configured-extension108.ca105');module('VA','queue-command',windowTitle='',command='VAssistX.ReparseCurrentFile',focusEditor=True);time.sleep(.5);module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);time.sleep(.6);v,_=send('VA',dict(kind='classification'));save('VA.configured-extension.navigation.json',v);journal['definitionReached']=(v.get('after')or{}).get('caret',999)<35;print('Native configured extension',journal['definitionReached'],flush=True);assert journal['definitionReached']
finally:
 with winreg.OpenKey(winreg.HKEY_CURRENT_USER,keypath,0,winreg.KEY_SET_VALUE)as k:winreg.SetValueEx(k,'ExtSource',0,kind,old)
 if mapped:save('VA.extension.mapping-removed.json',module('VA','extension-mapping105',windowTitle='',action='remove')[0])
 with winreg.OpenKey(winreg.HKEY_CURRENT_USER,keypath)as k:journal['registryRestored']=winreg.QueryValueEx(k,'ExtSource')[0]==old
 journal['runtimeNote']='Temporary extension remains loaded only in this owned test VS until normal teardown';save('native-extension-config.transaction.json',journal)
