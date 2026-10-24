import sys,json,time,subprocess,os,hashlib,winreg,ctypes,collections
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT
from background_client import module,send,RUN
import verify_native_parse_pool105 as pool
pool.P=OUT;pool.save=save
j={};text='class CustomExtension108 { public: int Compute(){return 108;} };\r\nint main(){CustomExtension108 value;return value.Compute();}\r\n';start=text.rindex('CustomExtension108')+3
reg=r'Software\Whole Tomato\Visual Assist X\VaNet17'
with winreg.OpenKey(winreg.HKEY_CURRENT_USER,reg)as key:oldExt,extKind=winreg.QueryValueEx(key,'ExtSource')
j['nativeOriginalExtSource']=oldExt

def restart(label):
 h=json.loads((RUN/'hosts.json').read_text());pid=h['VA']['pid'];r=OUT/(label+'.quit.json');w=launch(['quit',pid,r]);w.wait(timeout=20);assert json.loads(r.read_text()).get('quitRequested')
 k=ctypes.WinDLL('kernel32');k.OpenProcess.argtypes=[ctypes.c_ulong,ctypes.c_int,ctypes.c_ulong];k.OpenProcess.restype=ctypes.c_void_p;k.WaitForSingleObject.argtypes=[ctypes.c_void_p,ctypes.c_ulong];k.CloseHandle.argtypes=[ctypes.c_void_p];handle=k.OpenProcess(0x100000,False,pid)
 if handle:
  try:assert k.WaitForSingleObject(handle,30000)==0,'Owned VS did not quit; no force termination'
  finally:k.CloseHandle(handle)
 env=os.environ.copy();env['CODEASSIST_FOREGROUND_VALIDATION']='1';r=subprocess.run([sys.executable,'tools/ParityRunner/background_hosts.py','--roles','VA','--skip-configuration','--visible-input-desktop'],env=env,capture_output=True,timeout=40,creationflags=0x08000000);save(label+'.launch.json',{'returnCode':r.returncode,'stdout':r.stdout.decode(errors='replace'),'stderr':r.stderr.decode(errors='replace')});assert r.returncode==0
 pid=json.loads((RUN/'hosts.json').read_text())['VA']['pid']
 for mode in ['show-owned','load-capture-package']:
  for attempt in range(30):
   r=OUT/(label+'.'+mode+'.json');w=launch([mode,pid,r]);w.wait(timeout=15);v=json.loads(r.read_text());
   if v.get('visible')is True or v.get('loaded')is True:break
   time.sleep(.25)
  else:raise RuntimeError('Host not ready '+mode)
 print('restarted owned VA',pid,label,flush=True)

def native_setting(value):
 module('VA','open-options',windowTitle='');v,_=module('VA','options-read',windowTitle='Visual Assist Options',page='Performance');values=[str(x['value'])for x in v['result']['controls']if x['type']=='TextBox'and str(x.get('value','')).isdigit()];assert len(values)==1,values
 if 'originalParser'not in j:j['originalParser']=values[0];save('configuration.transaction.json',j)
 module('VA','options-edit',windowTitle='Visual Assist Options',setting='ParseConcurrency',oldText=values[0],newText=str(value));module('VA','options-button',windowTitle='Visual Assist Options',button='Save')
installedNativeMap=False
try:
 with winreg.OpenKey(winreg.HKEY_CURRENT_USER,reg,0,winreg.KEY_SET_VALUE)as key:winreg.SetValueEx(key,'ExtSource',0,extKind,oldExt+'.ca105;')
 native_setting(1);restart('single-parser-extension108');single=pool.count('single108')
 v,_=module('VA','extension-mapping105',windowTitle='',action='install');save('VA.extension.install.json',v);installedNativeMap=v.get('status')=='complete'
 setup('VA','extension108',text,start,file='custom-extension108.ca105');module('VA','queue-command',windowTitle='',command='VAssistX.ReparseCurrentFile',focusEditor=True);time.sleep(.6);module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);time.sleep(.6);v,_=send('VA',dict(kind='classification'));save('VA.extension.navigation.json',v);j['nativeFreshCustomDefinitionResolved']=v.get('after',{}).get('caret',999)<30;print('Native custom definition',j['nativeFreshCustomDefinitionResolved'],flush=True)
finally:
 if installedNativeMap:save('VA.extension.mapping-restored.json',module('VA','extension-mapping105',windowTitle='',action='remove')[0])
 with winreg.OpenKey(winreg.HKEY_CURRENT_USER,reg,0,winreg.KEY_SET_VALUE)as key:winreg.SetValueEx(key,'ExtSource',0,extKind,oldExt)
 if 'originalParser'in j:
  native_setting(int(j['originalParser']));restart('parser-settings-restored108');multi=pool.count('restored108');j['nativeParserSettingRestored']=True
 with winreg.OpenKey(winreg.HKEY_CURRENT_USER,reg)as key:j['nativeExtensionSettingRestored']=winreg.QueryValueEx(key,'ExtSource')[0]==oldExt
 save('configuration.transaction.json',j)
# Actual installed CA indexing engine, not a mirrored fake parser.
probe=Path('tools/ParseConcurrencyProbe/bin/Release/net8.0/ParseConcurrencyProbe.exe').resolve();assert probe.exists();r=subprocess.run([str(probe),str((OUT/'index-runtime').resolve())],capture_output=True,timeout=40,creationflags=0x08000000);save('CA.index-probe.execution.json',{'exitCode':r.returncode,'stdout':r.stdout.decode(errors='replace'),'stderr':r.stderr.decode(errors='replace')});assert r.returncode==0
installed=Path(os.environ['LOCALAPPDATA'])/'Microsoft/VisualStudio/17.0_ca39936fCodeAssistBackgroundCA/Extensions/nsmk4umm.bjy/CodeAssist.Indexing.dll';runtime=json.loads((OUT/'index-runtime/CA.parse-runtime.json').read_text());j['caEngineHashMatchesInstalled']=runtime['sha256'].lower()==hashlib.sha256(installed.read_bytes()).hexdigest();save('configuration.transaction.json',j);assert j['caEngineHashMatchesInstalled']
# Runtime extension option A-B-A and fresh-symbol index lookup.
installedCaMap=False;old=None
try:
 v,_=module('CA','extension-mapping105',windowTitle='',action='install');save('CA.extension.install.json',v);installedCaMap=v.get('status')=='complete'
 module('CA','open-options',windowTitle='');v,_=module('CA','options-read',windowTitle='CodeAssist Options',page='File Extensions');boxes=[x for x in v['result']['controls']if x['type']=='TextBox'and x['bounds']['x']>245];assert len(boxes)==1;old=boxes[0]['value'];module('CA','options-edit',windowTitle='CodeAssist Options',setting='Extensions105',oldText=old,newText='.ca105');module('CA','options-button',windowTitle='CodeAssist Options',button='Save');setup('CA','extension108',text,start,file='custom-extension108.ca105');v,_=module('CA','index-reinitialize',windowTitle='',_timeout=60);save('CA.extension.index-rebuilt.json',v);v,_=module('CA','index-state',windowTitle='',query='CustomExtension108');save('CA.extension.definition-on.json',v);j['caCustomDefinitionResolved']=len(v.get('result',{}).get('definitions',[]))>0
finally:
 if old is not None:
  module('CA','open-options',windowTitle='');module('CA','options-read',windowTitle='CodeAssist Options',page='File Extensions');module('CA','options-edit',windowTitle='CodeAssist Options',setting='Extensions105',oldText='.ca105',newText=old);module('CA','options-button',windowTitle='CodeAssist Options',button='Save');j['caExtensionsRestored']=True;save('CA.extension.final-index.json',module('CA','index-reinitialize',windowTitle='',_timeout=60)[0])
 if installedCaMap:save('CA.extension.mapping-restored.json',module('CA','extension-mapping105',windowTitle='',action='remove')[0])
 save('configuration.transaction.json',j)
print(j,flush=True)
