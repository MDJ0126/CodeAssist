import subprocess,sys,json,time
from pathlib import Path
O=Path('docs/validation/reexecution-108');rows=[]
jobs=['suite-create-all','suite-related','suite-view','background_editor_context_menu','background_abbreviation_popup','suite-editor-fix','suite-tool-fix']
for name in jobs:
 logfile=O/(name+'.log');start=time.time()
 with logfile.open('w',encoding='utf-8')as f:
  w=subprocess.Popen([sys.executable,'tools/ParityRunner/reexecute_job108.py',name],stdout=f,stderr=subprocess.STDOUT,creationflags=0x08000000)
  try:w.wait(timeout=180);status='returned'
  except subprocess.TimeoutExpired:w.kill();w.wait();status='supervisor-timeout'
 rows.append({'script':name,'status':status,'exitCode':w.returncode,'elapsedSeconds':round(time.time()-start,2)});(O/'independent-progress.json').write_text(json.dumps(rows,indent=2));print(name,status,w.returncode,rows[-1]['elapsedSeconds'],flush=True)
 record=O/'jobs'/name/'execution.json'
 if status=='supervisor-timeout' or not record.exists() or json.loads(record.read_text()).get('status')!='completed':break
