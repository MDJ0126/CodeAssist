import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,save,OUT;from background_client import RUN,module;from compiler_contracts import build_files
h=json.loads((RUN/'hosts.json').read_text());results={}
for role in ['VA','CA']:
 header=json.loads((OUT/(role+'.move-formatted-source.move-formatted103.h.buffer.json')).read_text(encoding='utf-8-sig'))['text'];cpp=json.loads((OUT/(role+'.move-formatted-source.move-formatted103.cpp.buffer.json')).read_text(encoding='utf-8-sig'))['text'];r=build_files('move-formatted-source',role,{'move-formatted103.h':header,'move-formatted103.cpp':cpp,'main.cpp':'#include "move-formatted103.h"\nint main(){ MoveFormatted103 value; return value.Value()==7?0:1; }\n'},'Value()==7 after requested body move');r['bodyMovedToSource']='MoveFormatted103::Value' in cpp and 'Value() const;' in header;results[role]=r
save('compiler-check/move-formatted-source-result.json',results);print({k:(v['passed'],v['bodyMovedToSource']) for k,v in results.items()},flush=True)
q=launch(['activate-document',h['VA']['pid'],OUT/'VA.move-formatted-source.undo-activate.json',RUN/'VA/move-formatted103.cpp']);q.wait(timeout=15);v,p=module('VA','queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.5)
for name in ['move-formatted103.h','move-formatted103.cpp']:
 q=launch(['read-document',h['VA']['pid'],OUT/('VA.move-formatted-source.'+name+'.undo.json'),RUN/'VA'/name]);q.wait(timeout=15)
for i in range(2):
 p=OUT/('CA.move-formatted-source.close-warning'+str(i)+'.driver.json');save(p.name,dict(title='CodeAssist',button='OK'));q=launch(['drive',h['CA']['pid'],OUT/('CA.move-formatted-source.close-warning'+str(i)+'.result.json'),p]);q.wait(timeout=15)
