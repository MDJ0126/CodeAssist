import json,math,collections
from pathlib import Path
from PIL import Image
p=Path('docs/validation/background-103');rows={}
for role in ['VA','CA']:
 imgs={};b=json.loads((p/(role+'.spelling.off.bounds.json')).read_text())['result']
 for state in ['off','on']:
  v=json.loads((p/(role+'.spelling.'+state+'.render.json')).read_text());imgs[state]=Image.open(next(x['path'] for x in v['rows'] if x['label']=='editor' and x['status']=='captured')).convert('RGB')
 r=[]
 for char,length in zip(b['characters'],[9,4]):
  scale=imgs['off'].width/b['logicalWidth'];box=[math.floor(char['left']*scale),math.floor(char['top']*scale),math.ceil((char['left']+char['width']*length)*scale),math.ceil((char['top']+char['height']+3)*scale)];c={k:list(v.crop(box).getdata()) for k,v in imgs.items()};r.append(dict(box=box,changedPixels=sum(x!=y for x,y in zip(c['off'],c['on'])),offColors=collections.Counter(c['off']).most_common(5),onColors=collections.Counter(c['on']).most_common(5)))
 rows[role]=r
print(rows);(p/'spelling.geometry.json').write_text(json.dumps(rows,indent=2))
