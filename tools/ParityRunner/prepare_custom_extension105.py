import sys,json,time,subprocess,os,winreg
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import RUN;from background_steps import launch
P=Path('.local/parity-runs/remaining-105');h=json.loads((RUN/'hosts.json').read_text());pid=h['VA']['pid'];q=launch(['quit',pid,P/'custom-extension.native-quit.json']);q.wait(timeout=20)
for n in range(60):
 c=subprocess.run(['powershell.exe','-NoProfile','-Command','if(Get-Process -Id '+str(pid)+' -ErrorAction SilentlyContinue){exit 9}'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=0x08000000)
 if c.returncode!=9:break
 time.sleep(.1)
key=r'Software\Whole Tomato\Visual Assist X\VaNet17'
with winreg.OpenKey(winreg.HKEY_CURRENT_USER,key,0,winreg.KEY_READ|winreg.KEY_SET_VALUE) as k:
 old,kind=winreg.QueryValueEx(k,'ExtSource');assert '.ca105;' not in old;patched=old+('.' if False else '')+'.ca105;';(P/'native-extension-registry.transaction.json').write_text(json.dumps(dict(key=key,name='ExtSource',original=old,patched=patched,kind=kind),indent=2));winreg.SetValueEx(k,'ExtSource',0,kind,patched)
IDE=Path('C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE');env=os.environ.copy();env['PATH']=str(IDE)+';'+env.get('PATH','');env['CODEASSIST_PARITY_ROOT']=str(RUN.resolve());env['CODEASSIST_PARITY_ROLE']='VA';env['CODEASSIST_BACKGROUND_CAPTURE']='1'
f=(P/'VA.custom-extension.restart.log').open('wb');broker=subprocess.Popen([sys.executable,'tools/ParityRunner/isolated_desktop.py','CodeAssistVerify-runtime',str(IDE/'devenv.exe'),str(RUN/'VA/Parity.sln'),'/RootSuffix','CodeAssistBackgroundVA','/Log',str(P.resolve()/'VA.custom-extension.activity.xml')],env=env,stdout=f,stderr=subprocess.STDOUT,creationflags=0x08000000);f.close()
for n in range(80):
 lines=(P/'VA.custom-extension.restart.log').read_text(errors='replace').splitlines();row=next((json.loads(x) for x in lines if x.startswith('{') and 'launchedPid' in x),None)
 if row:break
 time.sleep(.1)
assert row;pid=row['launchedPid'];h['VA']=dict(pid=pid,brokerPid=broker.pid,profile='CodeAssistBackgroundVA',desktop='CodeAssistVerify-runtime');(RUN/'hosts.json').write_text(json.dumps(h,indent=2));print('VA launched',pid,flush=True)
for mode in ['show-owned','load-capture-package']:
 out=P/('custom-extension.'+mode+'.json')
 for attempt in range(60):
  q=launch([mode,pid,out]);q.wait(timeout=15);r=json.loads(out.read_text(encoding='utf-8-sig'))
  if r.get('pid')==pid and r.get('visible' if mode=='show-owned' else 'loaded') is True:break
  time.sleep(.5)
 else:raise RuntimeError('Native host not ready '+mode)
 print(mode,'ready',flush=True)
