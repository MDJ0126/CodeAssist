import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
hosts=json.loads((RUN/'hosts.json').read_text());c=json.loads((OUT/'change-signature.input.json').read_text())
def drive(role,name,spec):
 p=OUT/(name+'.driver.json');save(p.name,spec);q=launch(['drive',hosts[role]['pid'],OUT/(name+'.driver-result.json'),p]);q.wait(timeout=15);r=json.loads((OUT/(name+'.driver-result.json')).read_text(encoding='utf-8-sig'));print(name,r['acted'],flush=True);return r['acted']
assert drive('VA','VA.change-signature.confirm',dict(title='Change Signature',button='Change',edit='int ChangeSig103(int second, int first)'))
v,p=send('VA',dict(kind='classification'));save('VA.change-signature.after.json',v);print('VA',repr(v.get('after',{}).get('text')),flush=True)
module('VA','queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.4);v,p=send('VA',dict(kind='classification'));save('VA.change-signature.undo.json',v)
flags=RUN/'CA/compile_flags.txt';existed=flags.exists();old=flags.read_bytes() if existed else None
try:
 if not existed:flags.write_text('-std=c++17\n',encoding='utf-8')
 setup('CA','change-signature',c['text'],c['start']);v,p=module('CA','queue-command',windowTitle='',command='448',focusEditor=True);save('CA.change-signature.queued.json',v);assert drive('CA','CA.change-signature.order',dict(title='CodeAssist',button='Save',edit='2,1'))
 applied=drive('CA','CA.change-signature.apply',dict(title='Change Signature',button='Apply changes'))
 if not applied:
  q=launch(['ui-tree',hosts['CA']['pid'],OUT/'CA.change-signature.pending-tree.json']);q.wait(timeout=15)
 else:
  v,p=send('CA',dict(kind='classification'));save('CA.change-signature.after.json',v);print('CA',repr(v.get('after',{}).get('text')),flush=True);module('CA','queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.4);v,p=send('CA',dict(kind='classification'));save('CA.change-signature.undo.json',v)
finally:
 if not existed:flags.unlink(missing_ok=True)
 elif flags.read_bytes()!=old:flags.write_bytes(old)
 save('compile-flags-transaction.json',dict(existed=existed,restored=flags.read_bytes()==old if existed else not flags.exists()))
