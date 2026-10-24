"""Cross-file move verification, including private-owner focus restoration."""
import sys,json,time,base64,argparse
sys.path.insert(0,'tools/ParityRunner')
from background_steps import setup,focus_existing,save,launch,OUT
from background_client import RUN,module
from compiler_contracts import build_files
p=argparse.ArgumentParser();p.add_argument('direction',choices=['to-header','to-source']);args=p.parse_args()
id='cross-move-'+args.direction;stem=id+'103';owner='CrossMoveHeader103' if args.direction=='to-header' else 'CrossMoveSource103'
decl='#pragma once\r\nclass '+owner+'\r\n{\r\npublic:\r\n    int Value() const;\r\n};\r\n'
inline='#pragma once\r\nclass '+owner+'\r\n{\r\npublic:\r\n    int Value() const\r\n    {\r\n        return 7;\r\n    }\r\n};\r\n'
include='#include "'+stem+'.h"\r\n';definition=include+'\r\nint '+owner+'::Value() const\r\n{\r\n    return 7;\r\n}\r\n'
header=decl if args.direction=='to-header' else inline;cpp=definition if args.direction=='to-header' else include
save(id+'.input.json',dict(header=header,source=cpp,direction=args.direction));h=json.loads((RUN/'hosts.json').read_text());journal=[];results={}
try:
 for role in ['VA','CA']:
  folder=RUN/role;flags=folder/'compile_flags.txt';journal.append(dict(role=role,path=str(flags),original=base64.b64encode(flags.read_bytes()).decode() if flags.exists() else None));flags.write_bytes(b'-std=c++17\n')
  (folder/(stem+'.h')).write_bytes(header.encode());(folder/(stem+'.cpp')).write_bytes(cpp.encode());active=cpp if args.direction=='to-header' else header;file=stem+('.cpp' if args.direction=='to-header' else '.h');extra=stem+('.h' if args.direction=='to-header' else '.cpp')
  setup(role,id,active,active.index('Value')+2,file=file,extras=[extra])
  command=('VAssistX.RefactorMoveImplementationToHeaderFile' if args.direction=='to-header' else 'VAssistX.RefactorMoveImplementationToSourceFile') if role=='VA' else '267'
  v,_=module(role,'queue-command',windowTitle='',command=command,focusEditor=True);save(role+'.'+id+'.queued.json',v)
  if role=='CA':
   label='Move function body to declaration' if args.direction=='to-header' else 'Move function body to out-of-line';deadline=time.monotonic()+20
   while time.monotonic()<deadline:
    v,_=module(role,'compiler-read',windowTitle='')
    if v.get('result',{}).get('status')=='observed':break
    time.sleep(.15)
   save(role+'.'+id+'.actions.json',v);print(role,id,'actions',v.get('result'),flush=True)
   if label in v.get('result',{}).get('labels',[]):
    v,_=module(role,'compiler-select',windowTitle='',label=label);save(role+'.'+id+'.selected.json',v)
    title='CodeAssist: '+label;v,_=module(role,'window-read',windowTitle=title);save(role+'.'+id+'.preview.json',v)
    v,_=module(role,'arm-owner-focus-restore',windowTitle=title);save(role+'.'+id+'.restore-armed.json',v);assert v['status']=='complete',v
    spec=OUT/(role+'.'+id+'.apply.driver.json');save(spec.name,dict(title=title,button='Apply changes',privateFocus=True));q=launch(['drive',h[role]['pid'],OUT/(role+'.'+id+'.apply-result.json'),spec]);q.wait(timeout=15)
  time.sleep(.4);buffers={}
  for name in [stem+'.h',stem+'.cpp']:
   path=OUT/(role+'.'+id+'.'+name+'.buffer.json');q=launch(['read-document',h[role]['pid'],path,folder/name]);q.wait(timeout=15);buffers[name]=json.loads(path.read_text(encoding='utf-8-sig'))['text'];print(role,name,repr(buffers[name]),flush=True)
  harness='#include "'+stem+'.h"\nint main(){ '+owner+' value; return value.Value()==7?0:1; }\n';r=build_files(id,role,dict(buffers,**{'main.cpp':harness}),'Value()==7 after body move');r['requestedMoveApplied']=('Value() const;' not in buffers[stem+'.h'] and owner+'::Value' not in buffers[stem+'.cpp']) if args.direction=='to-header' else ('Value() const;' in buffers[stem+'.h'] and owner+'::Value' in buffers[stem+'.cpp']);results[role]=r
  if r['requestedMoveApplied']:
   focus_existing(role,id+'-undo',folder/file);module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.3)
   for name in [stem+'.h',stem+'.cpp']:
    q=launch(['read-document',h[role]['pid'],OUT/(role+'.'+id+'.'+name+'.undo.json'),folder/name]);q.wait(timeout=15)
finally:
 from pathlib import Path
 for j in journal:
  f=Path(j['path']);f.unlink() if j['original'] is None else f.write_bytes(base64.b64decode(j['original']));j['restored']=True
 save(id+'.flags-transaction.json',journal);save('compiler-check/'+id+'-result.json',results)
