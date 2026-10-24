import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text());target=RUN/'CA/move-selection-new-file-output103.h';assert not target.exists()
for mode,spec in [('set-id',dict(id='1001',value=str(target))),('drive',dict(title='Create file',button='저장(S)',requiredNewFile=str(target),fileFieldId='1001',privateFocus=True))]:
 p=OUT/('CA.new-file.retry-'+mode+'.spec.json');save(p.name,spec);q=launch([mode,h['CA']['pid'],OUT/('CA.new-file.retry-'+mode+'.result.json'),p]);q.wait(timeout=15)
 print(mode,flush=True)
time.sleep(.5)
for name in ['move-selection-new-file.cpp',target.name]:
 q=launch(['read-document',h['CA']['pid'],OUT/('CA.new-file.'+name+'.buffer.json'),RUN/'CA'/name]);q.wait(timeout=15)
 print(name,flush=True)
