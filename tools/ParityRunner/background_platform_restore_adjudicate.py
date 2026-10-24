import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import OUT,save;from adjudicate_background import update

def opt(op,**kw):
 for n in range(30):
  v,_=module('VA',op,windowTitle='Visual Assist Options',**kw)
  if v.get('error'):raise RuntimeError(v['error'])
  if (v.get('result') or {}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
module('VA','cancel-menus',windowTitle='');module('VA','open-options',windowTitle='');opt('options-read',page='C/C++ Directories');opt('options-edit',setting='DirectoryPlatform',newText='Custom');v=opt('options-read',inspectDirectoryRows=True);paths=[x['path'] for x in v['result']['lists'][0]['items']]
if paths:
 assert len(paths)==1 and paths[0].endswith('shared-custom103');opt('options-edit',setting='CustomDirectory',action='remove',path=paths[0]);opt('options-button',button='Save');module('VA','open-options',windowTitle='');v=opt('options-read',page='C/C++ Directories',inspectDirectoryRows=True);assert v['result']['lists'][0]['items']==[]
save('VA.platform.empty-custom-restored.json',v);opt('options-edit',setting='DirectoryPlatform',newText='Project defined');opt('options-button',button='Save');module('VA','open-options',windowTitle='');v=opt('options-read',page='C/C++ Directories',inspectDirectoryRows=True);save('VA.platform.final-restored-options.json',v);assert v['result']['combos'][1]['selected']==1;opt('options-button',button='Cancel');v,_=module('VA','owned-build-configuration',windowTitle='');save('VA.platform.final-build-state.json',v);assert v['result']['active']=='Debug' and v['result']['activePlatform']=='x64'
j=json.loads((OUT/'platform-custom.transaction.json').read_text());j.update(emptyOriginalCustomSetRestored=True,originalPlatformRestored=True,originalBuildRestored=True);save('platform-custom.transaction.json',j)
for role in ['VA','CA']:
 files=['VA.custom-directory.added.json','VA.custom-directory.saved-readback.json','VA.platform.anycpu-build.json','VA.platform.after-build-switch.json','VA.platform.empty-custom-restored.json','VA.platform.final-restored-options.json','VA.platform.final-build-state.json'] if role=='VA' else ['CA.platform.custom-controls.json','CA.platform.custom-page.json'];save(role+'.platform-editing.bundle.json',dict(observations={f:json.loads((OUT/f).read_text()) for f in files},transaction=j,scope='actual custom directory save and build platform switch independence; native header navigation requires restart and is not certified'))
update(105,'different','VAで자체 Custom 경로 저장·재열기를 확인하고 실제 솔루션 빌드 플랫폼을 x64에서 Any CPU로 바꿔도 편집 플랫폼과 경로가 Custom으로 유지됨을 확인했습니다. CA C/C++ Directories 페이지는 사용자 경로·플랫폼 편집을 지원하지 않는다고 표시하고 대응 컨트롤이 없습니다. 원래 빈 Custom 경로, Project defined, Debug/x64까지 복원했습니다. 재시작 후 헤더 검색 전체 지원으로 확대하지 않았습니다.',{x:x+'.platform-editing.bundle.json' for x in ['VA','CA']},dict(nativeCustomPathSavedReadBack=True,actualBuildPlatformChanged=True,nativeEditingPlatformUnaffected=True,caCustomPlatformEditable=False,allOriginalSettingsRestored=True))
