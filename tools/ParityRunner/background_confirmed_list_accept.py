import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from background_client import module
expected=json.loads((OUT/'VA.suggest-auto-native.typed.json').read_text())['result']['after']['text'];v,_=module('VA','native-list-accept',windowTitle='',hwnd=3283458,choice='true',expectedText=expected);save('VA.native-autosuggest.actual-accept.json',v);print(v,flush=True)
