import json,collections,hashlib,html,re
from pathlib import Path
from datetime import datetime,timezone,timedelta
from PIL import Image
root=Path('docs/validation/remaining-098-10min');p=json.loads((root/'plan.json').read_text());ids={c['feature'] for c in p['cases']};assert len(ids)==98
byrole={}
for role in ['VA','CA']:
 d=json.loads((root/role/(role+'.json')).read_text());assert d['role']==role and d['planHash']==hashlib.sha256((root/'plan.json').read_bytes()).hexdigest();assert len({r['feature'] for r in d['cases']})==len(d['cases']);byrole[role]={r['feature']:r for r in d['cases']}
prior=json.loads(Path('docs/validation/paired-098/features.json').read_text());features={int(f['id'][-3:]):f for f in prior};rows=[]
keys=['text','caret','selectionStart','selectionLength','selectedText']
def eq(a,b):return isinstance(a,dict) and isinstance(b,dict) and all(a.get(k)==b.get(k) for k in keys)
for c in p['cases']:
 n=c['feature'];v=byrole['VA'].get(n);a=byrole['CA'].get(n);status='unverified';reason='기능 결과를 판정할 어댑터·기대값 부족';prefill=None
 if not v or not a:reason='실행 결과 누락'
 elif v.get('error') or a.get('error'):reason='검사 실행 오류; 제품 불일치로 계산하지 않음'
 elif c['action']=='dialog-probe':reason='창·설정 페이지 관찰 및 취소만 수행; 기능 통과 아님'
 else:reason='입력 결과 관찰; 기능 발동·물리 입력 간섭·전체 조건 미검증'
 if n==3 and v and a and not v.get('error') and not a.get('error') and eq(v.get('before'),a.get('before')):
  def query(r):
   controls=(r.get('probe') or {}).get('controls') or []
   return next((x.get('value') for x in controls if isinstance(x,dict) and x.get('type')=='ControlType.Edit'),None)
  vq,aq=query(v),query(a);selected=v['before'].get('selectedText')
  if vq==selected and selected and aq is not None and aq!=vq:
   status='different';reason='선택 심볼에서 심볼 찾기 실행 시 초기 검색어 차이: VA는 선택값, CA는 빈 검색어';prefill=dict(VA=vq,CA=aq)
   features[n].update(state='different',reason=reason,newEvidence='remaining-098-10min/paired.json')
 rows.append(dict(id='VA-F'+str(n).zfill(3),name=features[n]['name'],state=status,reason=reason,VA=v,CA=a,prefill=prefill,fullFunctionalPass=False))
counts=collections.Counter(f['state'] for f in features.values());now=datetime.now(timezone(timedelta(hours=9)));start=datetime.fromisoformat(p['startedAt']);stats={r:dict(rows=len(byrole[r]),invoked=sum(bool(x.get('invoked')) for x in byrole[r].values()),executionErrors=sum(bool(x.get('error')) for x in byrole[r].values())) for r in ['VA','CA']}
images=[]
for role in ['VA','CA']:
 for f in (root/role).glob('*.png'):
  try:
   with Image.open(f) as im:im.verify()
   images.append(dict(role=role,file=f.relative_to(root).as_posix(),sha256=hashlib.sha256(f.read_bytes()).hexdigest()))
  except Exception:pass
summary=dict(requested=98,recordedBoth=sum(n in byrole['VA'] and n in byrole['CA'] for n in ids),actualInvokedBoth=sum(bool((byrole['VA'].get(n) or {}).get('invoked')) and bool((byrole['CA'].get(n) or {}).get('invoked')) for n in ids),roleStats=stats,newFunctionalPasses=0,newObservedDifferences=sum(r['state']=='different' for r in rows),totalMatches=counts['representative-match'],totalDifferences=counts['different'],totalUnverified=counts['unverified'],denominator=108,determinatePercent=round((108-counts['unverified'])/108*100,2),fullProductSimilarity=None,images=len(images),proposedSeconds=600,startedAt=p['startedAt'],reportedAt=now.isoformat(),elapsedSeconds=round((now-start).total_seconds()),all98FunctionallyVerified=False)
for name,obj in [('summary.json',summary),('paired.json',rows),('features.json',list(features.values())),('screenshots.json',images)]:
 (root/name).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
h='<!doctype html><html lang="ko"><meta charset="utf-8"><title>남은 98개: 실행 시도와 검증 한계</title><style>body{background:#1c1e25;color:#eee;font:15px/1.6 system-ui;margin:28px}a{color:#ffd166}pre{white-space:pre-wrap}details{border:1px solid #555;padding:10px;margin:10px 0}img{max-width:46%;vertical-align:top}.warn{color:#ff9e9e}</style><h1>남은 98개 자동 검사 결과</h1>'
h+='<p class="warn"><strong>98개 전체 기능 검증 완료가 아닙니다. 모든 항목에 대한 실행 결과·실패 기록을 모았지만, 창 열기/취소와 명령 호출을 기능 통과로 세지 않았습니다.</strong></p><pre>'+html.escape(json.dumps(summary,ensure_ascii=False,indent=2))+'</pre>'
h+='<p>CA 프로세스 종료 뒤의 RPC 실패는 실제 기능 실행으로 계산하지 않습니다. CA 종료 로그는 자동화 중복 닫기와 DialogResult 예외를 포함하며, 제품 원인으로 단정하지 않습니다. 재시작 시도 기록도 보존했습니다.</p><p><a href="ca-termination-events.json">종료 이벤트</a> · <a href="plan.json">고정 98개 계획</a> · <a href="paired.json">항목별 원본</a> · <a href="features.json">전체 108개</a></p>'
for r in rows:
 n=int(r['id'][-3:]);id='remaining-'+str(n).zfill(3);h+='<details><summary>'+html.escape(r['id']+' '+r['name']+' — '+r['state'])+'</summary><p>'+html.escape(r['reason'])+'</p><pre>'+html.escape(json.dumps(r,ensure_ascii=False,indent=2))+'</pre>'
 for phase in ['before','dialog','after']:
  h+='<div>'+''.join('<a href="'+i['file']+'"><img loading="lazy" src="'+i['file']+'"></a>' for i in images if Path(i['file']).name==id+'-'+phase+'.png')+'</div>'
 h+='</details>'
h+='</html>';(root/'index.html').write_text(h,encoding='utf-8');Path('docs/remaining-98.html').write_text('<!doctype html><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=validation/remaining-098-10min/index.html">',encoding='utf-8');print(json.dumps(summary,ensure_ascii=False))
