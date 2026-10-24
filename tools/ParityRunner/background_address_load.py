import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import save,OUT
f=json.loads((OUT/'pdb-fixture.json').read_text());v,p=module('VA','options-toggle',windowTitle='Browse',toggle='_Browse to a .pdb or an executable file',value=True);save('VA.address-resolver.browse-mode.json',v)
v,p=module('VA','window-edit',windowTitle='Browse',file=f['pdb'],fileSha256=f['pdbSha256'],commit=True,button='OK');save('VA.address-resolver.browse-load.json',v);print(v['status'],v.get('result',{}).get('clicked'),v.get('error'),flush=True)
