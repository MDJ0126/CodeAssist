import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import focus_existing,save,OUT,launch;from background_client import RUN,module;from compiler_contracts import build_files
h=json.loads((RUN/'hosts.json').read_text());results={}
for role in ['VA','CA']:
 header=json.loads((OUT/(role+'.move-restored-source.move-restored103.h.buffer.json')).read_text(encoding='utf-8-sig'))['text'];cpp=json.loads((OUT/(role+'.move-restored-source.move-restored103.cpp.buffer.json')).read_text(encoding='utf-8-sig'))['text'];r=build_files('move-restored-source',role,{'move-restored103.h':header,'move-restored103.cpp':cpp,'main.cpp':'#include "move-restored103.h"\nint main(){ MoveRestored103 value; return value.Value()==7?0:1; }\n'},'Value()==7 and body exists only in source');r['bodyMovedToSource']='MoveRestored103::Value' in cpp and 'Value() const;' in header;results[role]=r
 focus_existing(role,'move-restored-source-undo',RUN/role/'move-restored103.cpp');module(role,'queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.4)
 for name in ['move-restored103.h','move-restored103.cpp']:
  q=launch(['read-document',h[role]['pid'],OUT/(role+'.move-restored-source.'+name+'.undo.json'),RUN/role/name]);q.wait(timeout=15)
save('compiler-check/move-restored-source-result.json',results);print({k:(v['passed'],v['bodyMovedToSource']) for k,v in results.items()})
