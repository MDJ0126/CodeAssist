import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import OUT,save,setup
folder=RUN/'shared-custom103';j={}
def opt(role,op,**kw):
 for n in range(25):
  v,_=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
  if v.get('error'):raise RuntimeError(v['error'])
  if (v.get('result') or {}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
def opened(role):
 module(role,'open-options',windowTitle='');return opt(role,'options-read',page='C/C++ Directories',inspectDirectoryRows=True)
try:
 v=opt('VA','options-button',button='Save');save('VA.custom-directory.saved.json',v);v=opened('VA');save('VA.custom-directory.saved-readback.json',v);assert v['result']['combos'][1]['selected']==0 and v['result']['lists'][0]['items'][0]['path']==str(folder);opt('VA','options-button',button='Cancel')
 v,_=module('VA','owned-build-configuration',windowTitle='');save('VA.platform.original-build.json',v);j['originalBuild']=v['result']['active'];j['originalBuildPlatform']=v['result']['activePlatform'];assert j['originalBuild']=='Debug';v,_=module('VA','owned-build-configuration',windowTitle='',name='Debug',platform='Any CPU');save('VA.platform.anycpu-build.json',v);assert v['result']['activePlatform']=='Any CPU';v=opened('VA');save('VA.platform.after-build-switch.json',v);assert v['result']['combos'][1]['selected']==0 and v['result']['lists'][0]['items'][0]['path']==str(folder);j['customPlatformPersistedAcrossBuildSwitch']=True;opt('VA','options-button',button='Cancel')
 text='#include "PlatformHeader103.h"\r\nint ExternalLookup103() { return ExternalPlatform103::Value(); }\r\n';setup('VA','external-custom-lookup',text,text.index('ExternalPlatform103')+3);time.sleep(.5);v,_=module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);save('VA.external-custom-lookup.queued.json',v);time.sleep(.5);v,_=module('VA','view-state',windowTitle='');save('VA.external-custom-lookup.after.json',v);print('lookup',str(v)[:1700],flush=True)
 v=opened('CA');save('CA.platform.custom-controls.json',v);v=opt('CA','options-read',page='C/C++ Directories');save('CA.platform.custom-page.json',v);opt('CA','options-button',button='Cancel')
finally:
 try:
  v=opened('VA');opt('VA','options-edit',setting='DirectoryPlatform',newText='Custom');v=opt('VA','options-read',inspectDirectoryRows=True);paths=[x['path'] for x in v['result']['lists'][0]['items']]
  if str(folder) in paths:opt('VA','options-edit',setting='CustomDirectory',action='remove',path=str(folder))
  v=opt('VA','options-read',inspectDirectoryRows=True);assert v['result']['lists'][0]['items']==[];opt('VA','options-edit',setting='DirectoryPlatform',newText='Project defined');opt('VA','options-button',button='Save');v=opened('VA');save('VA.platform.restored-options.json',v);j['originalPlatformRestored']=v['result']['combos'][1]['selected']==1;opt('VA','options-button',button='Cancel')
  if j.get('originalBuild'):v,_=module('VA','owned-build-configuration',windowTitle='',name=j['originalBuild'],platform=j['originalBuildPlatform']);save('VA.platform.restored-build.json',v);j['originalBuildRestored']=v['result']['active']==j['originalBuild'] and v['result']['activePlatform']==j['originalBuildPlatform']
 except Exception as e:j['restoreError']=str(e)
 save('platform-custom.transaction.json',j);print(j,flush=True)
