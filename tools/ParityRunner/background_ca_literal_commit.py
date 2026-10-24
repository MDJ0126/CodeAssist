import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module
text='bool NativeSuggestion103() { bool value = ; return value; }\r\n';setup('CA','suggest-auto-native',text,text.index('; return'));v,_=module('CA','posted-text',windowTitle='',text='tru');save('CA.native-autosuggest.typed.json',v);v,_=module('CA','completion-commit',windowTitle='',choice='true');save('CA.native-autosuggest.actual-accept.json',v);print(v,flush=True)
