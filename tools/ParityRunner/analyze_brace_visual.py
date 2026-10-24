import json,math,collections
from pathlib import Path
from PIL import Image
p=Path('docs/validation/background-103');allrows={}
for id in ['brace-visual-pair','brace-visual-mismatch']:
 allrows[id]={}
 for role in ['VA','CA']:
  images={};bounds={}
  for state in ['baseline','active','restored']:
   r=json.loads((p/(role+'.'+id+'.'+state+'.render.json')).read_text());path=next(x['path'] for x in r['rows'] if x['label']=='editor' and x['status']=='captured');images[state]=Image.open(path).convert('RGB');bounds[state]=json.loads((p/(role+'.'+id+'.'+state+'.bounds.json')).read_text())['result']
  result=[]
  for i,b in enumerate(bounds['baseline']['characters']):
   scale=images['baseline'].width/bounds['baseline']['logicalWidth'];box=(math.floor(b['left']*scale),math.floor(b['top']*scale),math.ceil((b['left']+b['width'])*scale),math.ceil((b['top']+b['height'])*scale));crops={s:images[s].crop(box) for s in images};a=list(crops['baseline'].getdata());on=list(crops['active'].getdata());back=list(crops['restored'].getdata());result.append(dict(character=b['text'],offset=b['offset'],box=box,activeChangedPixels=sum(x!=y for x,y in zip(a,on)),restoredChangedPixels=sum(x!=y for x,y in zip(a,back)),baselineColors=collections.Counter(a).most_common(4),activeColors=collections.Counter(on).most_common(4)))
  allrows[id][role]=result;print(id,role,result)
(p/'brace-visual.geometry.json').write_text(json.dumps(allrows,indent=2),encoding='utf-8')
