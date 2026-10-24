import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT;from background_client import RUN,send
h=json.loads((RUN/'hosts.json').read_text());p=OUT/'CA.close-goto-picker.driver.json';save(p.name,dict(title='CodeAssist: 정의 후보 선택',button='Cancel'));q=launch(['drive',h['CA']['pid'],OUT/'CA.close-goto-picker.result.json',p]);q.wait(timeout=15);r=json.loads((OUT/'CA.close-goto-picker.result.json').read_text(encoding='utf-8-sig'));print('closed',r.get('acted'))
v,p=send('CA',dict(kind='classification'));save('CA.after-cleanup-observed.json',v);print('state',v['status'],repr(v.get('after',{}).get('text','')[:150]))
