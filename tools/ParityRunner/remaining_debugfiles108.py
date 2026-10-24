import sys,json,time,hashlib,subprocess
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT
from background_client import module,RUN
h=json.loads((RUN/'hosts.json').read_text());fixture=json.loads(Path('docs/validation/reexecution-108/jobs/suite-pdb/pdb-fixture.json').read_text())
for mode,command,field in [('pdb','400','pdb'),('address','401','exe')]:
 v,_=module('CA','queue-command',windowTitle='',command=command,focusEditor=True);save('CA.'+mode+'.queued.json',v);time.sleep(.5);file=Path(fixture[field]);sha=hashlib.sha256(file.read_bytes()).hexdigest();title='CodeAssist: PDB Explorer' if mode=='pdb' else 'CodeAssist: Address Resolver';request=OUT/('CA.'+mode+'.open.json');worker=subprocess.run([str(Path('tools/ParityRunner/OwnedFileOpen108.exe').resolve()),str(h['CA']['pid']),title,str(file),sha,str(request)],capture_output=True,timeout=15,creationflags=0x08000000);assert worker.returncode==0,request.read_text();time.sleep(.5)
 if mode=='address':
  spec=OUT/'CA.address.rva.spec.json';save(spec.name,dict(title='CodeAssist',button='Save',edit='PdbAudit103+0x1000'));w=launch(['drive',h['CA']['pid'],OUT/'CA.address.rva.applied.json',spec]);w.wait(timeout=15);assert json.loads((OUT/'CA.address.rva.applied.json').read_text()).get('acted')
 tree=OUT/('CA.'+mode+'.actual-tree.json');w=launch(['ui-tree',h['CA']['pid'],tree]);w.wait(timeout=10);rows=json.loads(tree.read_text());values=[str(r.get('value') or r.get('name'))for r in rows if r['type']in ['ControlType.Edit','ControlType.Text','ControlType.DataItem']];save('CA.'+mode+'.observed-values.json',{'file':str(file),'sha256':sha,'values':values,'symbolObserved':any('PdbTarget103'in x for x in values)});print(mode,'PdbTarget103',any('PdbTarget103'in x for x in values),flush=True)
v,_=module('VA','address-input',windowTitle='',text='0x1000');save('VA.address.resolve-current.json',v);print('Native address',v.get('result'),flush=True)
