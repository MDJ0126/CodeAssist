import sys,json,time,subprocess,os,hashlib,shutil,collections
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT
from background_client import module,send,RUN
import verify_native_parse_pool105 as pool
pool.P=OUT;pool.save=save
baseline=json.loads(Path('docs/validation/reexecution-108/jobs/remaining_configuration108/configuration.transaction.json').read_text());original=int(baseline['originalParser']);j={'nativeOriginalParser':original};maps={}
text='class CustomExtension108 { public: int Compute(){return 108;} };\r\nint main(){CustomExtension108 value;return value.Compute();}\r\n';start=text.rindex('CustomExtension108')+3
single=pool.count('single-current108')
for role in ['VA','CA']:
 v,_=module(role,'extension-mapping105',windowTitle='',action='install');save(role+'.mapping.install.json',v);maps[role]=v.get('status')=='complete'
try:
 setup('VA','custom-extension-clean108',text,start,file='custom-extension-clean108.ca105');module('VA','queue-command',windowTitle='',command='VAssistX.ReparseCurrentFile',focusEditor=True);time.sleep(.5);module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);time.sleep(.5);v,_=send('VA',dict(kind='classification'));save('VA.custom-extension.actual.json',v);j['nativeCustomDefinitionReached']=(v.get('after')or{}).get('caret',999)<30;print('Native custom extension',j['nativeCustomDefinitionReached'],flush=True)
finally:
 if maps.get('VA'):save('VA.mapping.restore.json',module('VA','extension-mapping105',windowTitle='',action='remove')[0]);maps['VA']=False
 module('VA','open-options',windowTitle='');v,_=module('VA','options-read',windowTitle='Visual Assist Options',page='Performance');old=[str(x['value'])for x in v['result']['controls']if x['type']=='TextBox'and str(x.get('value','')).isdigit()];assert len(old)==1;module('VA','options-edit',windowTitle='Visual Assist Options',setting='ParseConcurrency',oldText=old[0],newText=str(original));module('VA','options-button',windowTitle='Visual Assist Options',button='Save');save('native-setting-restored-before-restart.json',{'previous':old[0],'restored':original});j['nativeSettingSavedOriginal']=True;save('configuration-clean.transaction.json',j)
# Graceful restart of this owned, clean test host to observe restored concurrency.
h=json.loads((RUN/'hosts.json').read_text());oldPid=h['VA']['pid'];v,_=module('VA','owned-host-quit108',windowTitle='');save('VA.clean-restart.quit.json',v);assert v.get('result',{}).get('status')=='quit-queued'
for i in range(60):
 r=subprocess.run(['powershell.exe','-NoProfile','-Command','if(Get-Process -Id '+str(oldPid)+' -ErrorAction SilentlyContinue){exit 9}'],capture_output=True,creationflags=0x08000000)
 if r.returncode!=9:break
 time.sleep(.15)
else:raise RuntimeError('Owned host did not exit normally; no forced shutdown')
env=os.environ.copy();env['CODEASSIST_FOREGROUND_VALIDATION']='1';r=subprocess.run([sys.executable,'tools/ParityRunner/background_hosts.py','--roles','VA','--skip-configuration','--visible-input-desktop'],env=env,capture_output=True,timeout=35,creationflags=0x08000000);assert r.returncode==0,r.stderr;h=json.loads((RUN/'hosts.json').read_text())
for mode in ['show-owned','load-capture-package']:
 for i in range(25):
  f=OUT/('VA.restart.'+mode+'.json');w=launch([mode,h['VA']['pid'],f]);w.wait(timeout=15);v=json.loads(f.read_text());
  if v.get('visible')is True or v.get('loaded')is True:break
  time.sleep(.2)
 else:raise RuntimeError('VA readiness failed '+mode)
setup('VA','parser-restored-clean108','class ParserRestored108 { public:int Value(){return 108;} };\r\n',9);multi=pool.count('restored-current108');a=collections.Counter(x['offset']for x in single['nativeThreads']);b=collections.Counter(x['offset']for x in multi['nativeThreads']);j['nativeWorkerGroupResponded']=any(a[k]==1 and b[k]>1 for k in b);j['nativeStartAddressGroups']={'single':dict(a),'restored':dict(b)};save('configuration-clean.transaction.json',j);print('native groups',j['nativeStartAddressGroups'],flush=True)
# Probe the exact installed indexing binary in a disposable child process.
installed=Path(os.environ['LOCALAPPDATA'])/'Microsoft/VisualStudio/17.0_ca39936fCodeAssistBackgroundCA/Extensions/nsmk4umm.bjy';src=Path('tools/ParseConcurrencyProbe/bin/Release/net8.0');dest=OUT/'installed-index-probe';shutil.copytree(src,dest,dirs_exist_ok=True)
for name in ['CodeAssist.Indexing.dll','CodeAssist.Core.dll']:
 if (installed/name).exists():shutil.copy2(installed/name,dest/name)
r=subprocess.run([str(dest/'ParseConcurrencyProbe.exe'),str(OUT/'index-runtime')],capture_output=True,timeout=40,creationflags=0x08000000);save('CA.index-probe.execution.json',{'exitCode':r.returncode,'stdout':r.stdout.decode(errors='replace'),'stderr':r.stderr.decode(errors='replace')});assert r.returncode==0
oldExt=None
try:
 module('CA','open-options',windowTitle='');v,_=module('CA','options-read',windowTitle='CodeAssist Options',page='File Extensions');boxes=[x for x in v['result']['controls']if x['type']=='TextBox'and x['bounds']['x']>245];assert len(boxes)==1;oldExt=boxes[0]['value'];module('CA','options-edit',windowTitle='CodeAssist Options',setting='Extensions105',oldText=oldExt,newText='.ca105');module('CA','options-button',windowTitle='CodeAssist Options',button='Save');setup('CA','custom-extension-clean108',text,start,file='custom-extension-clean108.ca105');module('CA','index-reinitialize',windowTitle='',_timeout=60);v,_=module('CA','index-state',windowTitle='',query='CustomExtension108');save('CA.custom-extension.index-on.json',v);j['caCustomDefinitionIndexed']=bool(v.get('result',{}).get('definitions'));print('CA custom extension',j['caCustomDefinitionIndexed'],flush=True)
finally:
 if oldExt is not None:
  module('CA','open-options',windowTitle='');module('CA','options-read',windowTitle='CodeAssist Options',page='File Extensions');module('CA','options-edit',windowTitle='CodeAssist Options',setting='Extensions105',oldText='.ca105',newText=oldExt);module('CA','options-button',windowTitle='CodeAssist Options',button='Save');save('CA.custom-extension.index-restored.json',module('CA','index-reinitialize',windowTitle='',_timeout=60)[0]);j['caExtensionOptionRestored']=True
 if maps.get('CA'):save('CA.mapping.restore.json',module('CA','extension-mapping105',windowTitle='',action='remove')[0])
 save('configuration-clean.transaction.json',j)
print(j,flush=True)
