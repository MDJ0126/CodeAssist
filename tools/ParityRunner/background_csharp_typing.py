import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import save,OUT,focus_existing,launch
module('VA','open-options',windowTitle='');time.sleep(.2)
for op,kw in [('options-read',dict(page='Suggestions')),('options-toggle',dict(toggle='E_nable in C#',value=True)),('options-button',dict(button='Save'))]:
 v,_=module('VA',op,windowTitle='Visual Assist Options',**kw);save('VA.csharp-suggestions.'+op+'.json',v);assert v.get('result',{}).get('status')=='observed',v
v,_=module('VA','owned-csharp',windowTitle='',typing=True);save('VA.csharp-using.typing-prepared.json',v);focus_existing('VA','csharp-typing',RUN/'VA/csharp103/UsingAudit103.cs');v,_=module('VA','posted-text',windowTitle='',text='StringBuilder');save('VA.csharp-using.typed.json',v);time.sleep(.5);h=json.loads((RUN/'hosts.json').read_text());p=launch(['native-list-snapshot',h['VA']['pid'],OUT/'VA.csharp-using.list-snapshot.json']);p.wait(timeout=12);print((OUT/'VA.csharp-using.list-snapshot.json').read_text()[:1400],flush=True)
