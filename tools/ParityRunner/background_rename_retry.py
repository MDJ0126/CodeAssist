import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,save,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text())
for id,mode,spec in [('close-error','drive',dict(title='Visual Assist',button='확인',privateFocus=True)),('set-name','set-id',dict(id='1014',value='RenamedFileAudit103')),('rename','drive',dict(title='Rename Files -- Visual Assist',button='Rename',privateFocus=True))]:
 p=OUT/('VA.rename-related-file.retry-'+id+'.spec.json');save(p.name,spec);q=launch([mode,h['VA']['pid'],OUT/('VA.rename-related-file.retry-'+id+'.result.json'),p]);q.wait(timeout=15);print(id,flush=True)
time.sleep(.5)
q=launch(['ui-tree',h['VA']['pid'],OUT/'VA.rename-related-file.retry-tree.json']);q.wait(timeout=15)
print([(x['name'],x['id'],x.get('value')) for x in json.loads((OUT/'VA.rename-related-file.retry-tree.json').read_text(encoding='utf-8-sig'))[:8]])
