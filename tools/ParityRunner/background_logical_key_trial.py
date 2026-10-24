import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module;from isolated_desktop import state
case=json.load(open('docs/validation/background-103/corresponding-file.case.json',encoding='utf-8'));rows={};before=state()
for role in ['VA','CA']:
 setup(role,'shortcut-alt-o',case['text'],0,file=case['file']);v,p=module(role,'logical-key',windowTitle='',key=79,alt=True);save(role+'.shortcut-alt-o.logical.json',v);rows[role]=v;print(role,v.get('result'),v.get('error'),flush=True)
save('shortcut-alt-o.isolation.json',dict(before=before,after=state(),physicalInput=False));print('logical key trial finished',flush=True)
