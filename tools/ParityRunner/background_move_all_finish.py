import sys,json,time,base64
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner')
from background_steps import save,launch,focus_existing,OUT
from background_client import module,RUN
from compiler_contracts import build_files
h=json.loads((RUN/'hosts.json').read_text());journal=json.loads((OUT/'move-all-source.flags-transaction.json').read_text());original=json.loads((OUT/'move-all-source.input.json').read_text());results={}
def buffers(role,stage):
 r={}
 for name in ['move-all-audit103.h','move-all-audit103.cpp']:
  file=OUT/(role+'.move-all-source.'+stage+'.'+name+'.json');q=launch(['read-document',h[role]['pid'],file,RUN/role/name]);q.wait(timeout=15);r[name]=json.loads(file.read_text(encoding='utf-8-sig'))['text']
 return r
def drive(role,stage,spec):
 p=OUT/(role+'.move-all-source.'+stage+'.driver.json');save(p.name,spec);q=launch(['drive',h[role]['pid'],OUT/(role+'.move-all-source.'+stage+'.driver-result.json'),p]);q.wait(timeout=15)
try:
 tree=json.loads((OUT/'VA.move-all-source.native-dialog-tree.json').read_text(encoding='utf-8-sig'));checked=[x for x in tree if x['type']=='ControlType.TreeItem'];assert len(checked)==3 and all(x['toggle']=='On' for x in checked);assert any('move-all-audit103.cpp' in x['name'] for x in tree)
 drive('VA','ok',dict(title='Move',button='OK',privateFocus=True));time.sleep(.5)
 v,p=module('CA','queue-command',windowTitle='',command='449',focusEditor=True);save('CA.move-all-source.queued.json',v);deadline=time.monotonic()+45;preview=None
 while time.monotonic()<deadline:
  v,p=module('CA','window-read',windowTitle='CodeAssist: Move ')
  if v.get('result',{}).get('status')=='observed':preview=v;break
  time.sleep(.2)
 save('CA.move-all-source.preview.json',v);assert preview,v
 title=preview['result']['title'];v,p=module('CA','arm-owner-focus-restore',windowTitle=title);save('CA.move-all-source.restore-armed.json',v);assert v['status']=='complete',v
 drive('CA','apply',dict(title=title,button='Apply changes',privateFocus=True));time.sleep(.5)
 for role in ['VA','CA']:
  current=buffers(role,'after');r=build_files('move-all-source',role,dict(current,**{'main.cpp':'#include "move-all-audit103.h"\nint main(){MoveAllAudit103 v;return v.First()+v.Second()==15?0:1;}\n'}),'First()+Second()==15 after both bodies move');r['bothMoved']=all('MoveAllAudit103::'+name in current['move-all-audit103.cpp'] for name in ['First','Second']) and all(name+'() const;' in current['move-all-audit103.h'] for name in ['First','Second']);r['undoStages']=[];results[role]=r;print(role,r['passed'],r['bothMoved'],flush=True)
  if r['bothMoved']:
   focus_existing(role,'move-all-source-undo',RUN/role/'move-all-audit103.cpp')
   for step in range(1,4):
    module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.3);current=buffers(role,'undo'+str(step));restored=current['move-all-audit103.h']==original['header'] and current['move-all-audit103.cpp']==original['source'];r['undoStages'].append(dict(step=step,restored=restored));print(role,'Undo',step,restored,flush=True)
    if restored:break
finally:
 for j in journal['entries']:
  f=Path(j['path']);f.unlink() if j['original'] is None else f.write_bytes(base64.b64decode(j['original']));j['restored']=True
 journal['state']='restored';save('move-all-source.flags-transaction.json',journal);save('compiler-check/move-all-source-result.json',results)
