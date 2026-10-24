import sys,json,shutil,collections
from PIL import Image
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import ROOT,read,update
old=ROOT/'column.geometry.json'
if old.exists():shutil.copyfile(old,ROOT/'column.geometry.old-threshold.json')
files={};geometry={}
for role in ['VA','CA']:
 images={};paths={}
 for state in ['off','on']:
  render=read(role+'.column.'+state+'-render.json');paths[state]=next(x['path'] for x in render['rows'] if x['label']=='editor' and x['status']=='captured');images[state]=Image.open(paths[state]).convert('RGB')
 a,b=images['off'],images['on'];assert a.size==b.size;counts=[sum(a.getpixel((x,y))!=b.getpixel((x,y)) for y in range(60,a.height-20)) for x in range(a.width)];maximum=max(counts);xs=[x for x,n in enumerate(counts) if n==maximum];ys=[y for y in range(60,a.height-20) if a.getpixel((xs[0],y))!=b.getpixel((xs[0],y))];steps=dict(collections.Counter(j-i for i,j in zip(ys,ys[1:])));colors=list({b.getpixel((x,y)) for x in xs for y in ys});geometry[role]=dict(viewport=a.size,columnPixels=xs,changedRows=maximum,verticalStepCounts=steps,colors=colors,requestedColumn=40)
 assert maximum>0;target=ROOT/'render-evidence'/(role+'-column-guide.png');shutil.copyfile(paths['on'],target);name=role+'.column.checked-bundle.json';(ROOT/name).write_text(json.dumps(dict(geometry=geometry[role],images=paths,originalSettings=read(role+'.column.original-options.json'),restoredSettings=read(role+'.column.restored-options.json'),transaction=read('column.transaction.json')),ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
(ROOT/'column.geometry.json').write_text(json.dumps(geometry,indent=2),encoding='utf-8');assert geometry['VA']['columnPixels']==[282] and geometry['CA']['columnPixels']==[281,282];assert '5' in {str(x) for x in geometry['VA']['verticalStepCounts']} and 1 in geometry['CA']['verticalStepCounts']
update(57,'different','同じ40열 설정에서 두 제품의 가이드 위치는 1픽셀 범위 안입니다. VA는 x=282의 1픽셀 점선(5픽셀 간격), CA는 x=281~282의 연속선으로 나타납니다. 이전 70% 연속선 검출 기준이 VA 점선을 놓친 분석 오류를 수정했으며, 가이드 미표시로 판정하지 않았습니다. 설정은 원복했습니다.'.replace('同じ','같은'),files,{'guideVisibleBoth':True,'horizontalPositionWithinOnePixel':True,'strokePatternAndWidthEqual':False,'settingsRestored':all(x.get('restored') for x in read('column.transaction.json'))})
from pathlib import Path
p=Path('tools/ParityRunner/ReportBackground103.py');s=p.read_text().replace("images.update({15:","images.update({57:('render-evidence/VA-column-guide.png','render-evidence/CA-column-guide.png'),15:",1);p.write_text(s)
print(geometry)
