import sys,json,time,hashlib,shutil,re
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module
OUT=Path('docs/validation/background-103/options-pages');OUT.mkdir(exist_ok=True)
def record(role,label,v):
 name=role+'-'+re.sub('[^a-zA-Z0-9_-]','_',label);out=OUT/(name+'.json');out.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8');result=v.get('result') or {};image=result.get('image')
 if image:shutil.copy2(image,OUT/(name+'.png'))
 return dict(page=label,status=v['status'],observation=result.get('status'),headings=result.get('visibleHeadings'),json=out.name,image=name+'.png' if image else None)
rows={}
for role,title in [('VA','Visual Assist Options'),('CA','CodeAssist Options')]:
 rows[role]=[]
 try:
  module(role,'open-options',windowTitle=title);time.sleep(.8)
  v,p=module(role,'options-read',windowTitle=title)
  if v.get('result',{}).get('status')!='observed':raise RuntimeError(str(v))
  rows[role].append(record(role,'initial',v));headers=v['result']['treeHeaders']
  for label in headers:
   if not label.strip():continue
   v,p=module(role,'options-read',windowTitle=title,page=label);rows[role].append(record(role,label,v))
   (OUT/'status.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
  v,p=module(role,'options-search',windowTitle=title,query='Coloring');rows[role].append(record(role,'search-Coloring',v))
  v,p=module(role,'options-search',windowTitle=title,query='DefinitelyAbsent103');rows[role].append(record(role,'search-negative',v))
  print(role,len(headers),'pages captured',flush=True)
 finally:
  v,p=module(role,'options-button',windowTitle=title,button='Cancel');rows[role].append(record(role,'cancel',v));(OUT/'status.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print('All option page captures saved; changes cancelled',flush=True)
