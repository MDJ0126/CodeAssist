import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import RUN,module
v,_=module('VA','native-links-query',windowTitle='',file=str(RUN/'VA/ViewFileTarget103.h'),renderFilePreview=True);save('VA.native-file-preview.rendered.json',v);print([(x.get('plugin'),x.get('status'),x.get('previewImage'),x.get('previewContent'),str(x.get('error'))[:160]) for x in v.get('result',{}).get('rows',[])],flush=True)
