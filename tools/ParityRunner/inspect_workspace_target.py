import json,sys,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import module
v,_=module('CA','pane-read',windowTitle='',pane='workspace');save('CA.view-file-nav.tree-retry.json',v);r=v.get('result',{});print(r.get('status'),len(r.get('labels',[])),[x for x in r.get('labels',[]) if 'ViewFile' in x],r.get('labels',[])[:12]);v,_=module('CA','index-state',windowTitle='',query='ViewFileTarget103');save('CA.view-file-nav.index-state.json',v);print(v)
