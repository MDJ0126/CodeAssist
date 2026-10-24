import json,sys,hashlib,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT;from background_client import RUN,module
r=json.loads((OUT/'CA.file-preview-utility.picker-tree.json').read_text(encoding='utf-8-sig'));print([(x['type'],x['name'],x['id']) for x in r if x['id']=='1'])
h=json.loads((RUN/'hosts.json').read_text());file=RUN/'CA/ViewFileTarget103.h';sha=hashlib.sha256(file.read_bytes()).hexdigest()
for mode,spec in [('set-id',dict(id='1148',value=str(file))),('drive',dict(title='CodeAssist: Preview File',button='열기(O)',requiredFile=str(file),requiredSha256=sha,fileFieldId='1148',privateFocus=True))]:
 p=OUT/('CA.file-preview-utility.'+mode+'.spec.json');save(p.name,spec);q=launch([mode,h['CA']['pid'],OUT/('CA.file-preview-utility.'+mode+'.result.json'),p]);q.wait(timeout=15)
time.sleep(.3);v,_=module('CA','window-read',windowTitle=file.name);save('CA.file-preview-utility.actual.json',v);print(v.get('result'),str(v.get('error'))[:120],flush=True)
