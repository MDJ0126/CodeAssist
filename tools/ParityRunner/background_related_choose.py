import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text())
for name,row in [('expand','Base Classes'),('base','RelBase103')]:
 p=OUT/('VA.related-symbols.'+name+'.pick.json');save(p.name,dict(row=row,logicalKeys=True));q=launch(['pick',h['VA']['pid'],OUT/('VA.related-symbols.'+name+'.pick-result.json'),p]);q.wait(timeout=13);time.sleep(.3);r=json.loads((OUT/('VA.related-symbols.'+name+'.pick-result.json')).read_text(encoding='utf-8-sig'));print('VA',name,{k:v for k,v in r.items() if k!='observations'},flush=True)
p=OUT/'CA.related-symbols.base.pick.json';save(p.name,dict(row='RelBase103    '));q=launch(['pick',h['CA']['pid'],OUT/'CA.related-symbols.base.pick-result.json',p]);q.wait(timeout=13)
for role in ['VA','CA']:
 q=launch(['observe',h[role]['pid'],OUT/(role+'.related-symbols.navigation.json'),OUT/(role+'.related-symbols-menu.prepared.json')]);q.wait(timeout=15);r=json.loads((OUT/(role+'.related-symbols.navigation.json')).read_text(encoding='utf-8-sig'));print(role,'destination',r.get('after',{}).get('caret'),r.get('after',{}).get('selectionLength'),r.get('after',{}).get('selectedText'),flush=True)
