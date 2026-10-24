import sys
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import module
v,_=module('VA','native-snippet-editor',windowTitle='');save('VA.created-selection.editor-draft.json',v);print(v,flush=True)
