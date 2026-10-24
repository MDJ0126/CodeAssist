import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,save,OUT;from background_client import RUN,module,send
h=json.loads((RUN/'hosts.json').read_text());p=OUT/'VA.create-from-usage.commit.spec.json';save(p.name,dict(title='Create From Usage',button='Create',privateFocus=True));q=launch(['drive',h['VA']['pid'],OUT/'VA.create-from-usage.commit.json',p]);q.wait(timeout=15)
for role in ['VA','CA']:
 time.sleep(.3);q=launch(['read-document',h[role]['pid'],OUT/(role+'.create-from-usage.buffer.json'),RUN/role/'create-from-usage.cpp']);q.wait(timeout=15)
 print(role,json.loads((OUT/(role+'.create-from-usage.buffer.json')).read_text(encoding='utf-8-sig'))['text'],flush=True)
 v,_=module(role,'render-sources',windowTitle='');save(role+'.create-from-usage.popup.json',v)
 v,_=send(role,{});save(role+'.create-from-usage.render.json',v)
