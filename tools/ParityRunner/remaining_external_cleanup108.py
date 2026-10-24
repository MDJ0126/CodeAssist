import sys,json,base64,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN
from background_steps import OUT,save
source=__import__('pathlib').Path('docs/validation/reexecution-108/jobs/suite-external');header=RUN/'shared-custom103/PlatformHeader103.h'
for kind in [1,0]:
 module('VA','open-options',windowTitle='');module('VA','options-read',windowTitle='Visual Assist Options',page='C/C++ Directories');module('VA','options-edit',windowTitle='Visual Assist Options',setting='DirectoryPlatform',newText='Custom');module('VA','options-edit',windowTitle='Visual Assist Options',setting='DirectoryKind',index=kind);v,_=module('VA','options-read',windowTitle='Visual Assist Options',inspectDirectoryRows=True);paths=[x['path']for x in v['result']['lists'][0]['items']]
 if str(header.parent)in paths:module('VA','options-edit',windowTitle='Visual Assist Options',setting='CustomDirectory',action='remove',path=str(header.parent))
 module('VA','options-button',windowTitle='Visual Assist Options',button='Save');save('VA.cleanup'+str(kind)+'.json',{'observedPaths':paths,'removedOnlyOwnedPath':str(header.parent)})
module('VA','open-options',windowTitle='');module('VA','options-read',windowTitle='Visual Assist Options',page='C/C++ Directories');module('VA','options-edit',windowTitle='Visual Assist Options',setting='DirectoryPlatform',newText='Project defined');save('VA.final-platform.json',module('VA','options-button',windowTitle='Visual Assist Options',button='Save')[0])
j=json.loads((source/'external-build.flags-transaction.json').read_text());path=__import__('pathlib').Path(j['path']);assert path==RUN/'CA/compile_flags.txt';current=path.read_bytes();expected=('-std=c++17\n-I'+str(header.parent).replace('\\','/')+'\n').encode();assert current.replace(b'\r\n',b'\n')==expected
if j['original']is None:path.unlink()
else:path.write_bytes(base64.b64decode(j['original']))
build=json.loads((source/'compiler-check/external-build-result.json').read_text());assert all(x['compileExit']==0 and x['runExit']==0 for x in build.values())
actual={r:json.loads((source/(r+'.external-build-lookup.after.json')).read_text())for r in ['VA','CA']}
save('external-result.json',{'feature':106,'actualBuildPassedBoth':True,'actualExecutablePassedBoth':True,'definitionReached':{r:x['result']['dteFile']==str(header)for r,x in actual.items()},'build':build,'observations':actual,'settingsRestored':True,'scope':'Actual external -I compile/run both; navigation differs; not a strict match'})
