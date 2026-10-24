import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import save
v,_=module('VA','native-links-query',windowTitle='',file=str(RUN/'VA/RenamedFileAudit103.h'));save('VA.native-links-host.query.json',v);print(v,flush=True)
