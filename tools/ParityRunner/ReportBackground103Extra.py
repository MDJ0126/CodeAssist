from pathlib import Path
import json,hashlib,html,shutil
from PIL import Image
from collections import Counter
root=Path('docs/validation/background-103');checks={};ev={}
for role in ['VA','CA']:
 pair=[]
 for suffix in ['', '.moved']:
  p=root/(role+'.current-line'+suffix+('-render.json' if suffix else '.render.json'));r=json.loads(p.read_text());image=Path(next(x['path'] for x in r['rows'] if x['label']=='editor'));im=Image.open(image).convert('RGB');ys=[]
  for y in range(80):
   c,n=Counter(im.crop((550,y,900,y+1)).getdata()).most_common(1)[0]
   if c==(70,70,70) and n==350:ys.append(y)
  target=root/'render-evidence'/(role+'-current-line'+suffix+'.png');shutil.copy2(image,target);pair.append(dict(rows=ys,image=str(target.relative_to(root)),sha256=hashlib.sha256(image.read_bytes()).hexdigest()))
 assert pair[0]['rows']==[32,33,45,46] and pair[1]['rows']==[16,17,29,30]
 setup=json.loads((root/(role+'.current-line.render-setup.json')).read_text(encoding='utf-8-sig'));moved=json.loads((root/(role+'.current-line.moved-probe.json')).read_text());assert setup['after']['caret']==48 and moved['after']['caret']==28
 checks[role]=dict(beforeCaret=48,afterCaret=28,frameShift=-16,captures=pair)
 p=root/(role+'.current-line.moved-probe.json');ev[role]=dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),data=moved)
(root/'current-line-geometry.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2))
f=json.loads((root/'features.json').read_text());d=json.loads((root/'adjudicated.json').read_text())
def update(id,state,reason,evidence):
 next(x for x in f if x['id']==id).update(state=state,reason=reason,mainEvidence='background-103/'+next(iter(evidence.values()))['file'],fullFeatureCertified=False)
 d['newJudgments'][:]=[x for x in d['newJudgments'] if x['id']!=id];d['newJudgments'].append(dict(id=id,state=state,reason=reason,evidence=evidence))
update('VA-F007','representative-match','동일 소스의 커서를 3행→2행으로 이동했을 때 현재 줄 테두리가 양쪽 모두 16px 위로 이동. VS 기본 표시를 포함한 구성 결과의 대표 비교이며 VA 고유 렌더러 인과 검증은 아님.',ev)
e={}
for role in ['VA','CA']:
 p=root/'options-pages'/(role+'-Coloring.json');v=json.loads(p.read_text());assert v['status']=='complete' and v['result']['status']=='observed'
 e[role]=dict(file='options-pages/'+p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),data=v)
 p=root/'options-pages'/(role+'-cancel.json');v=json.loads(p.read_text());assert v['result']['clicked'] and not v['result']['stillVisible']
update('VA-F108','different','옵션 전체 트리 30개(VA)/32개(CA) 실제 순회·촬영, 검색 양성/음성·취소 실행. Coloring에서 VA의 enum/namespace 색상과 적용 대상 6개 토글이 CA 화면에 없음. 전체 설정 저장 동작의 인증은 아님.',e)
d['counts']={s:sum(x['state']==s for x in f) for s in ['representative-match','different','unverified']};d['determinate']=108-d['counts']['unverified'];(root/'features.json').write_text(json.dumps(f,ensure_ascii=False,indent=2));(root/'adjudicated.json').write_text(json.dumps(d,ensure_ascii=False,indent=2))
pages=root/'options-pages';r=json.loads((pages/'status.json').read_text());names=list(dict.fromkeys(x['page'] for v in r.values() for x in v));body=''
for name in names:
 body+='<article><h2>'+html.escape(name)+'</h2><div class="pair">'
 for role in ['VA','CA']:
  x=next((x for x in r[role] if x['page']==name),None);body+='<section><h3>'+role+'</h3>'
  if x:body+='<a href="'+x['json']+'">상태 JSON</a><img loading="lazy" src="'+(x['image'] or '')+'">'
  else:body+='<p>동일 이름의 트리 항목 없음</p>'
  body+='</section>'
 body+='</div></article>'
(pages/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>VA / CA 전체 옵션</title><style>body{background:#17191d;color:#eee;font:15px Segoe UI;margin:24px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}img{width:100%;height:auto}a{color:#9cf}article{margin:28px 0}section{min-width:0}</style><h1>VA / CA 실제 옵션 페이지 대조</h1><p>VA 30개·CA 32개 트리 항목 순회. 부모 항목은 기본 하위 페이지가 표시될 수 있음. 실제 설정 저장/기능 통과율과 별개인 UI 증거입니다. 검색 후 원래 설정을 저장하지 않고 취소했습니다.</p>'+body,encoding='utf-8')
print(d['counts'],d['determinate'])
