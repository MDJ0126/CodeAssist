import sys,json,subprocess,time,hashlib
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import RUN,module
from background_steps import save,OUT
plan=Path('docs/validation/layout-audit-0.0.90/keyboard-analysis.json').resolve();h=json.loads((RUN/'hosts.json').read_text());root=RUN/'physical-keys108';root.mkdir(exist_ok=True)
for role in ['VA','CA']:
 module(role,'cancel-menus',windowTitle='');r=subprocess.run(['tools/ShortcutProbe/SingleCaseProbe.exe',str(h[role]['pid']),role,str(root),str(plan)],capture_output=True,timeout=130,creationflags=0x08000000);save(role+'.physical22.execution.json',{'exitCode':r.returncode,'stdout':r.stdout.decode(errors='replace'),'stderr':r.stderr.decode(errors='replace')});assert r.returncode==0;d=json.loads((root/(role+'.json')).read_text());save(role+'.physical22.json',d);assert len(d['cases'])==22;print(103,role,'inputSent',sum(bool(x['inputSent'])for x in d['cases']),'of',22,'errors',sum(bool(x.get('error'))for x in d['cases']),flush=True)
