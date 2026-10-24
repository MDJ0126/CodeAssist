import json,sys,subprocess,time
from pathlib import Path
ROOT=Path.cwd();OUT=ROOT/'docs/validation/background-103';PY=sys.executable;WORKER=ROOT/'tools/ShortcutProbe/BackgroundOptionsV3.exe'
def launch(args):return subprocess.Popen([PY,str(ROOT/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(WORKER),*map(str,args)],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=0x08000000)
p=launch(['case',44248,OUT/'CA.missing-cases.valid.json',OUT/'missing-cases.valid.case.json','CA'])
for i,spec in enumerate([dict(title='CodeAssist',button='Save'),dict(title='CodeAssist',button='Save'),dict(title='Add Missing Case Statements',button='Apply changes')]):
 s=OUT/('CA.missing-cases.valid.driver%d.json'%i);s.write_text(json.dumps(spec));q=launch(['drive',44248,OUT/('CA.missing-cases.valid.driver%d.result.json'%i),s]);q.wait(timeout=15)
 r=json.loads((OUT/('CA.missing-cases.valid.driver%d.result.json'%i)).read_text(encoding='utf-8-sig'));print(i,r['acted'],flush=True)
 if not r['acted']:raise RuntimeError('Dialog step not performed')
p.wait(timeout=15);print((OUT/'CA.missing-cases.valid.json').read_text(encoding='utf-8-sig'),flush=True)
