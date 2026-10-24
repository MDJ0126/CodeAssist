import sys,json,time
sys.path.insert(0,'tools/ParityRunner')
from background_steps import save,launch,OUT
from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text())
for role in ['VA','CA']:
 target=RUN/role/'move-selection-new-file-output103.h';assert not target.exists()
 spec=OUT/(role+'.new-file.set-path.json');save(spec.name,dict(id='1210' if role=='VA' else '1148',value=str(target)))
 q=launch(['set-id',h[role]['pid'],OUT/(role+'.new-file.set-result.json'),spec]);q.wait(timeout=15)
 spec=OUT/(role+'.new-file.commit.json');save(spec.name,dict(title='Move Selection to New File' if role=='VA' else 'Create file',button='Move' if role=='VA' else 'Save',requiredNewFile=str(target),fileFieldId='1210' if role=='VA' else '1148',privateFocus=True))
 q=launch(['drive',h[role]['pid'],OUT/(role+'.new-file.commit-result.json'),spec]);q.wait(timeout=15);print(role,json.loads((OUT/(role+'.new-file.commit-result.json')).read_text(encoding='utf-8-sig')).get('acted'),flush=True)
 time.sleep(.4)
 for name in ['move-selection-new-file.cpp',target.name]:
  q=launch(['read-document',h[role]['pid'],OUT/(role+'.new-file.'+name+'.buffer.json'),RUN/role/name]);q.wait(timeout=15)
  print(role,name,flush=True)
