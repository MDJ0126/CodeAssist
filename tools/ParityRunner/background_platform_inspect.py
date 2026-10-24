import sys,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import save
module('VA','open-options',windowTitle='');time.sleep(.2)
for op,kw in [('options-read',dict(page='C/C++ Directories',inspectDirectoryRows=True)),('options-edit',dict(setting='DirectoryPlatform',newText='Custom')),('options-read',dict(inspectDirectoryRows=True))]:
 v,_=module('VA',op,windowTitle='Visual Assist Options',**kw);save('VA.directory-platform.'+('custom' if kw.get('inspectDirectoryRows') and 'page' not in kw else op)+'.json',v);print(v,flush=True)
module('VA','options-button',windowTitle='Visual Assist Options',button='Cancel')
