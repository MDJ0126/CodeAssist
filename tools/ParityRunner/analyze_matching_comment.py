import json,math,collections
from pathlib import Path
from PIL import Image
p=Path('docs/validation/background-103');r={}
for role in ['VA','CA']:
 crops={}
 for state in ['baseline','active','restored']:
  b=json.loads((p/(role+'.matching-comment.'+state+'.bounds.json')).read_text())['result'];v=json.loads((p/(role+'.matching-comment.'+state+'.render.json')).read_text());im=Image.open(next(x['path'] for x in v['rows'] if x['label']=='editor' and x['status']=='captured')).convert('RGB');ch=b['characters'][0];scale=im.width/b['logicalWidth'];box=[math.floor(ch['left']*scale),math.floor(ch['top']*scale),math.ceil((ch['left']+ch['width']*12)*scale),math.ceil((ch['top']+ch['height'])*scale)];crops[state]=list(im.crop(box).getdata())
 r[role]=dict(activeChangedPixels=sum(a!=b for a,b in zip(crops['baseline'],crops['active'])),restoredChangedPixels=sum(a!=b for a,b in zip(crops['baseline'],crops['restored'])),activeColors=collections.Counter(crops['active']).most_common(5))
print(r);(p/'matching-comment.geometry.json').write_text(json.dumps(r,indent=2))
