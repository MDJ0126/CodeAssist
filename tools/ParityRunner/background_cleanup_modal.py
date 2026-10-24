import sys,json,time
sys.path.insert(0,'tools/ParityRunner')
from background_steps import save,launch,OUT
from background_client import RUN,send
h=json.loads((RUN/'hosts.json').read_text())
# spec=OUT/'CA.forward-declaration.save-driver.json';save(spec.name,dict(title='CodeAssist',edit='ForwardNeeded103',button='Save'));q=launch(['drive',h['CA']['pid'],OUT/'CA.forward-declaration.saved.json',spec]);q.wait(timeout=15)

for role in ['CA','VA']:
 for i in range(4):
  q=launch(['logical-escape',h[role]['pid'],OUT/(role+'.cleanup-modal-'+str(i)+'.json')]);q.wait(timeout=10);time.sleep(.2)
 q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.after-cleanup-tree.json')]);q.wait(timeout=15)
 rows=json.loads((OUT/(role+'.after-cleanup-tree.json')).read_text(encoding='utf-8-sig'));print(role,'windows',[(x['name']) for x in rows if x['type']=='ControlType.Window'],flush=True)
