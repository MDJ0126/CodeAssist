import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import OUT,save,launch
v,_=module('VA','options-button',windowTitle='Visual Assist Options',button='Save');save('VA.external-build.custom-saved-before-restart.json',v);assert v['result']['clicked'];h=json.loads((RUN/'hosts.json').read_text());p=launch(['quit',h['VA']['pid'],OUT/'VA.external-build.graceful-quit.json']);p.wait(timeout=20);print((OUT/'VA.external-build.graceful-quit.json').read_text(),flush=True)
