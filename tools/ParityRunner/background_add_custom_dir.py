import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import OUT,save
folder=RUN/'shared-custom103';folder.mkdir(exist_ok=True);(folder/'PlatformHeader103.h').write_bytes(b'#pragma once\r\nclass ExternalPlatform103 { public: static int Value() { return 103; } };\r\n')
module('VA','open-options',windowTitle='');time.sleep(.2);module('VA','options-read',windowTitle='Visual Assist Options',page='C/C++ Directories');module('VA','options-edit',windowTitle='Visual Assist Options',setting='DirectoryPlatform',newText='Custom');v,_=module('VA','options-read',windowTitle='Visual Assist Options',inspectDirectoryRows=True);save('VA.custom-directory.original.json',v);assert all(x['path']==str(folder) for x in v['result']['lists'][0]['items'])
v,_=module('VA','options-edit',windowTitle='Visual Assist Options',setting='CustomDirectory',action='add',path=str(folder)) if not v['result']['lists'][0]['items'] else (v,None);save('VA.custom-directory.added.json',v);print(v,flush=True)
# Leave open for inspection; Cancel safely if adding did not work.
if v.get('error'):module('VA','options-button',windowTitle='Visual Assist Options',button='Cancel')
