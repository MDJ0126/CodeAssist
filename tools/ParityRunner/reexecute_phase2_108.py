import subprocess,sys,json,time
from pathlib import Path
O=Path('docs/validation/reexecution-108');rows=[]
jobs=['background_forward_known', 'background_info_clean_focus', 'background_change_signature', 'background_add_member', 'background_similar_member', 'background_extract', 'background_encapsulate', 'background_implement_virtual', 'background_created_snippet_roundtrip_fixed', 'background_snippet_input_year', 'background_snippet_surround', 'background_smart_template_designated', 'suite-usage', 'suite-recent', 'suite-csharp', 'background_issue_mock', 'suite-debug']
for name in jobs:
 logfile=O/(name+'.log');start=time.time()
 with logfile.open('w',encoding='utf-8')as f:
  w=subprocess.Popen([sys.executable,'tools/ParityRunner/reexecute_job108.py',name],stdout=f,stderr=subprocess.STDOUT,creationflags=0x08000000)
  try:w.wait(timeout=180);status='returned'
  except subprocess.TimeoutExpired:w.kill();w.wait();status='supervisor-timeout'
 rows.append({'script':name,'status':status,'exitCode':w.returncode,'elapsedSeconds':round(time.time()-start,2)});(O/'phase2-progress.json').write_text(json.dumps(rows,indent=2));print(name,status,w.returncode,rows[-1]['elapsedSeconds'],flush=True)
 if status=='supervisor-timeout':break
