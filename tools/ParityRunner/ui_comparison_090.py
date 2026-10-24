import json, pathlib, shutil, html, collections
import numpy as np
from PIL import Image
P=pathlib.Path('C:/Projects/CodeAssist'); R=P/'.local/parity-runs/20261018-113400-full-090'; B=P/'docs/validation/automated-runner-20261018/20261018-015759-745'; O=P/'docs/validation/automated-runner-20261018/20261018-021854-OptionsVerified'; D=P/'docs/validation/ui-comparison-0.0.90-20261018';D.mkdir(parents=True,exist_ok=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
plan=read(R/'CA.plan.json')['cases'][:108]; cr={c['id']:c for c in read(R/'CA.results.json')['cases']};vr={c['id']:c for c in read(B/'VA.results.json')['cases']};op=read(O/'VA.plan.json')['cases'];orr={c['id']:c for c in read(O/'VA.results.json')['cases']};pages={c['page']:c for c in op}
def image(root,role,id,phase):
 p=root/'screenshots'/f'{role}-{id}-{phase}.png'
 if not p.exists():return None
 return p
copied={}
def copy(p):
 if not p:return ''
 key=p.parent.parent.name+'-'+p.name;t=D/key
 if key not in copied:
  shutil.copy2(p,t);m=p.with_suffix('.json')
  if m.exists():shutil.copy2(m,t.with_suffix('.json'))
  copied[key]=True
 return 'validation/'+D.name+'/'+key
def metric(a,b):
 aa=np.asarray(Image.open(a).convert('RGB'));bb=np.asarray(Image.open(b).convert('RGB'))
 h=max(aa.shape[0],bb.shape[0]);w=max(aa.shape[1],bb.shape[1]);arr=[];masks=[]
 for im in (aa,bb):
  colors,count=np.unique(im.reshape(-1,3),axis=0,return_counts=True);bg=colors[count.argmax()];out=np.empty((h,w,3),dtype=np.int16);out[:]=bg;out[:im.shape[0],:im.shape[1]]=im;arr.append(out);masks.append(np.max(np.abs(out-bg.astype(np.int16)),axis=2)>24)
 mask=masks[0]|masks[1];score=100*(1-np.abs(arr[0]-arr[1])[mask].mean()/255)
 return round(float(score),2),[aa.shape[1],aa.shape[0]],[bb.shape[1],bb.shape[0]]
rows=[];scores={};cards=[]
for c in plan:
 id=c['id'];ca=cr.get(id,{});va=vr.get(id,{});base=B;vid=id;family=c.get('page') or c.get('vaCommand') or id
 if c.get('page') in pages:
  vid=pages[c['page']]['id'];va=orr[vid];base=O
 ap=image(base,'VA',vid,'dialog');bp=image(R,'CA',id,'dialog');status='비교 화면 관찰 불충분';score=None
 same_input=c.get('page') or (ca.get('inputHash')==va.get('inputHash') and (ca.get('before') or {}).get('selectionStart')==(va.get('before') or {}).get('selectionStart') and (ca.get('before') or {}).get('selectionLength')==(va.get('before') or {}).get('selectionLength'))
 good=bool(ap and bp and not ca.get('error') and not va.get('error') and same_input)
 if c.get('page'):good=good and bool((ca.get('probe') or {}).get('pageSelected')) and bool((va.get('probe') or {}).get('pageSelected'))
 else:good=good and (ca.get('probe') or {}).get('status') in ('dialog-observed-and-cancelled','dialog-closed-without-cancel-control','popup-menu-observed-and-cancelled') and (va.get('probe') or {}).get('status') in ('dialog-observed-and-cancelled','dialog-closed-without-cancel-control','popup-menu-observed-and-cancelled')
 dims=None
 if good:
  if family not in scores:scores[family]=metric(ap,bp)
  score,*dims=scores[family];status='동일 페이지 픽셀 비교 (동작 일치 판정 아님)'
 r={'sameInput':bool(same_input),'id':id,'family':family,'status':status,'score':score,'vaSource':str(base),'vaCase':vid,'caProcessId':ca.get('processId'),'vaError':va.get('error'),'caError':ca.get('error'),'vaProbe':(va.get('probe') or {}).get('status'),'caProbe':(ca.get('probe') or {}).get('status'),'dimensions':dims};rows.append(r)
 pairs=[]
 for phase in ['dialog','before','after']:
  a=image(base if phase=='dialog' else B,'VA',vid if phase=='dialog' else id,phase);b=image(R,'CA',id,phase)
  cells=[]
  for role,p in [('VA',a),('CA',b)]:
   src=copy(p);cells.append('<figure><figcaption>'+role+' · '+phase+'</figcaption>'+('<a href="'+src+'"><img loading="lazy" src="'+src+'"></a>' if src else '<p>촬영 없음</p>')+'</figure>')
  pairs.append('<details '+('open' if phase=='dialog' else '')+'><summary>'+phase+'</summary><div class="pair">'+''.join(cells)+'</div></details>')
 cards.append('<article data-text="'+html.escape(id+' '+family+' '+status)+'"><h2>'+id+' · '+html.escape(family)+'</h2><p>'+status+(' · <b>'+str(score)+'%</b>' if score is not None else ' · 점수 제외')+'</p><p class="muted">VA 근거: '+base.name+' / '+vid+' · CA: '+R.name+' / PID '+str(ca.get('processId'))+'</p>'+''.join(pairs)+'</article>')
summary={'run':R.name,'caVersion':'0.0.90','vaBuild':2630,'scenarios':len(plan),'caCompleted':len(cr),'scoredScenarioCount':sum(r['score'] is not None for r in rows),'uniqueScoredUI':len(scores),'knownUniqueFamilies':len(set(r['family'] for r in rows)),'foregroundPixelSimilarity':round(sum(s[0] for s in scores.values())/len(scores),2) if scores else None,'uniqueScores':scores,'rows':rows,'formula':'100*(1-mean(abs(RGB_VA-RGB_CA))/255), foreground union threshold24 from modal background; native size, no stretching; equal unique-family weighting','limitations':['108 scenarios are not exhaustive UI states','VA English vs CA Korean affects score','Pixel similarity is not functional equivalence','Missing or wrong-page probes excluded','VA baseline from earlier today; CA fresh 0.0.90']}
(D/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
score=summary['foregroundPixelSimilarity'];n=len(scores);total=summary['knownUniqueFamilies']
head=f'''<!doctype html><html lang="ko"><meta charset="utf-8"><title>VA / CA 0.0.90 UI 실화면 대조</title><style>body{{background:#15171e;color:#e9eaf0;font:15px/1.6 Segoe UI,sans-serif;margin:30px}}h1,b{{color:#ffda65}}article{{background:#20232d;padding:20px;margin:24px 0;border-radius:10px}}.pair{{display:grid;grid-template-columns:1fr 1fr;gap:16px}}figure{{margin:0;min-width:0}}img{{width:100%;height:auto;background:#111}}a{{color:#8ecaff}}.muted{{color:#a9b1c4}}input{{width:80%;padding:12px}}summary{{cursor:pointer;padding:8px}}</style><h1>VA / CodeAssist 전체 시나리오 UI 대조</h1><p><b>비교 가능한 UI 픽셀 유사도: {score}% · {n}/{total}개 고유 UI·명령 분류</b></p><p>108/108 시나리오에 대한 대조 행을 제공합니다. 점수 산출 가능한 시나리오 {summary['scoredScenarioCount']}/108개. 나머지는 관찰 실패·해당 팝업 없음·미촬영으로 제외했습니다. 108개가 제품의 모든 UI 상태를 뜻하지 않습니다.</p><p>CA 0.0.90 최신 실험용 VS 촬영 / VA build2630 기존 실실행 촬영. VA 옵션은 올바른 페이지 선택을 검증한 17페이지 촬영본을 사용합니다. 각 사진을 누르면 원본 크기로 열립니다.</p><h2>계산 기준</h2><p>빈 배경으로 점수가 부풀지 않도록 두 이미지의 전경 합집합만 사용한 RGB 절대 오차 점수입니다. 100 × (1 − 평균 RGB 차이 / 255). 배경 최빈색 대비 24 초과 픽셀을 전경으로 잡았습니다. 원본 크기를 유지하고 큰 캔버스에 배치하며 늘이거나 줄이지 않았습니다. 고유 UI별 동일 가중 평균입니다. 한국어 CA와 영어 VA의 텍스트 차이도 오차에 포함됩니다. <b>기능·단축키 동기화율 또는 사람이 평가한 디자인 유사도가 아닙니다.</b></p><p><a href="validation/{D.name}/summary.json">계산값·제외 사유·원본 출처 JSON</a></p><input placeholder="페이지 / 명령 / feature 번호 검색" oninput="document.querySelectorAll('article').forEach(x=>x.hidden=!x.dataset.text.toLowerCase().includes(this.value.toLowerCase()))">'''
(P/'docs/ui-comparison-0.0.90.html').write_text(head+''.join(cards)+'</html>',encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k not in ['rows','uniqueScores']},ensure_ascii=False))
