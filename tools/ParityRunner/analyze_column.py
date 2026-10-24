import json
from pathlib import Path
from PIL import Image
root=Path('docs/validation/background-103');res={}
for role in ['VA','CA']:
 imgs={}
 for state in ['off','on']:
  v=json.loads((root/(role+'.column.'+state+'-render.json')).read_text());p=next(x['path'] for x in v['rows'] if x['label']=='editor' and x['status']=='captured');imgs[state]=Image.open(p).convert('RGB')
 a,b=imgs['off'],imgs['on'];assert a.size==b.size
 points=[]
 for x in range(a.width):
  n=sum(a.getpixel((x,y))!=b.getpixel((x,y)) for y in range(60,a.height-20))
  if n>(a.height-80)*.7:points.append(dict(x=x,changedRows=n,color=b.getpixel((x,100))))
 res[role]=dict(size=a.size,verticalChanges=points[:30],totalVerticalColumns=len(points));print(role,res[role])
(root/'column.geometry.json').write_text(json.dumps(res,indent=2),encoding='utf-8')
