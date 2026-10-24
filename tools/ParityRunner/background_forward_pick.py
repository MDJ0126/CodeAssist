import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,save,OUT;from background_client import RUN,send
h=json.loads((RUN/'hosts.json').read_text());p=OUT/'VA.forward-menu.pick.spec.json';save(p.name,dict(row='Add Forward Declaration'));q=launch(['native-menu-pick',h['VA']['pid'],OUT/'VA.forward-menu.pick.json',p]);q.wait(timeout=15);time.sleep(.4);v,_=send('VA',dict(kind='classification'));save('VA.forward-menu.observed.json',v);print(v['after'],flush=True)
