import json,html,hashlib,collections,datetime,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'docs/validation/reexecution-108';B=ROOT/'docs/validation/background-103'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
start=datetime.datetime(2026,10,19,13,48,31,tzinfo=datetime.timezone(datetime.timedelta(hours=9)));now=datetime.datetime.now(start.tzinfo)
old=read(ROOT/'docs/validation/full-108-107/summary.json');spec=read(ROOT/'docs/validation/integrated-spec.json');baseline=read(B/'features.json');m=read(O/'expanded-job-feature-map.json');m.update({'remaining_external_cleanup108':[106],'remaining_enum108':[30],'remaining_newfile108':[24]})
jobrows=[];byid=collections.defaultdict(list);finished=set();attempted=set()
for p in sorted((O/'jobs').glob('*/execution.json')):
 d=read(p);name=p.parent.name;ids=m.get(name,[]);d['catalogIds']=ids;d['evidenceFile']=str(p.relative_to(O)).replace('\\','/');jobrows.append(d)
 for i in ids:
  byid[i].append(d);attempted.add(i)
  if d['status']=='completed':finished.add(i)
for file in ['progress.json','core2-progress.json','core-final-progress.json']:
 for d in read(O/file):
  ok=len(d.get('roles',{}))==2 and all(x.get('worker',{}).get('status')=='complete'for x in d['roles'].values());i=d['feature'];attempted.add(i);byid[i].append({'script':d['name'],'status':'completed'if ok else'error','evidenceFile':file})
  if ok:finished.add(i)
# Deliberately do not convert wrapper completion into functional validation.
quality={24:'파일 생성 대화상자 제출/파일 이동 결과 확인 필요',30:'VA 생성 확인, CA 적용 결과 미확인',32:'VA C# 추천 후보가 비어 수락 단계 중단. 최신 재시도는 Suggestions 옵션에서 필요한 체크박스를 관찰하지 못해 중단',45:'Windows 보안 전면 창으로 실제 붙여넣기 입력 차단',48:'실제 클립보드 기록/붙여넣기 검사가 아직 진행되지 않음',54:'최근 추천 수락 재검사 실패',84:'목록 촬영 범위. 실제 단축키 수락 전체 검증 아님',91:'메모리 뷰의 실제 내용·동일 디버기 주소 비교 추가 확인 필요',92:'원본 Step Filter history-shape 오류. 실행기 종료와 기능 통과는 다름',102:'CA 확장자 인덱싱 확인. VA 정의 이동은 E_FAIL/미이동',103:'Windows 보안 전면 창으로 실제 22개 키 입력 재검사 보류'}
previous=set(old['currentTurnFreshCompletedCatalogIds']);union=finished|previous
counts=collections.Counter(x['status']for x in jobrows);summary={'startedAt':start.isoformat(),'updatedAt':now.isoformat(),'elapsedSeconds':round((now-start).total_seconds(),1),'status':'incomplete-input-blocked-and-oracle-gaps','catalogTotal':108,'freshWrapperCompletedCatalogCount':len(finished),'freshWrapperCompletionPercent':round(len(finished)/108*100,2),'freshWrapperCompletedCatalogIds':sorted(finished),'priorSeparateTurnCatalogCount':len(previous),'combinedWrapperCompletionCount':len(union),'combinedWrapperCompletionPercent':round(len(union)/108*100,2),'wrapperCompletionIsFunctionalPass':False,'jobCounts':dict(counts),'qualityGaps':quality,'all108FreshExecutionVerified':False,'fullFeatureCertified':0,'previousCumulativeStrictRepresentativeMatches':17,'previousCumulativeStrictRepresentativeMatchPercent':15.74,'previousCumulativeDifferent':91,'previousLedgerIsNotNewRun':True,'testedVersions':old['testedVersions'],'wholeProductSimilarity':'not recalculated; representative result match is not whole product similarity','xerj':'사용 — BackgroundWorker native command refactor 검색. 누락 결과는 rg·현재 소스·실행 결과로 보완','jobs':jobrows}
(O/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
def a(file,label='근거'):
 return '<a href="'+html.escape(file,quote=True)+'">'+html.escape(label)+'</a>'
rows=[]
for f in spec['features']:
 i=int(f['id'][-3:]);oldrow=next(x for x in baseline if x['id']==f['id']);jobs=byid[i]
 state='실행기 종료'if i in finished else('실행 중 오류'if jobs else'이번 재실행 없음')
 if i in previous and not jobs:state='이전 회차 별도 실행 완료'
 q=quality.get(i,'동일 입력의 대표 관찰 범위. 모든 지원 언어·옵션·입력 경로 인증 아님')
 rows.append('<tr><td>'+f['id']+'</td><td>'+html.escape(f['name'])+'</td><td>'+state+'</td><td>'+html.escape(q)+'</td><td>'+'<br>'.join(a(x['evidenceFile'],x['script']+' / '+x['status'])for x in jobs)+'</td><td>'+html.escape(oldrow['state'])+'</td></tr>')
images=[]
for job in ['suite-move-all','suite-tool-fix','remaining_print108','remaining_debugfiles108']:
 for p in sorted((O/'jobs'/job).rglob('*.png'))[:2]:images.append('<figure>'+a(str(p.relative_to(O)).replace('\\','/'),job)+'<br><img src="'+str(p.relative_to(O)).replace('\\','/')+'"></figure>')
body="""<!doctype html><html lang="ko"><meta charset="utf-8"><title>108 재실행 — 결과와 차단 항목</title><style>body{background:#17191f;color:#e8eaf0;font:15px/1.6 system-ui;margin:32px}h1{color:#ffd65a}a{color:#8bc7ff}table{border-collapse:collapse;width:100%}td,th{border:1px solid #414650;padding:10px;text-align:left;vertical-align:top}th{background:#2c303c}strong{color:#ffd65a}.box{background:#242833;padding:20px;margin:18px 0}figure{display:inline-block;vertical-align:top;width:46%;margin:1%}img{max-width:100%;max-height:500px}small{color:#afb5c5}</style><h1>108개 재실행 기록</h1>"""
body+='<p>시작 '+start.isoformat()+' / 갱신 '+now.isoformat()+'</p><div class="box"><strong>전체 검사 완수 상태가 아닙니다.</strong><p>실행기 정상 종료 '+str(len(finished))+'/108개 ('+str(summary['freshWrapperCompletionPercent'])+'%). 이 수치는 기능 검증률·일치율이 아닙니다. 실제 결과 누락·무반응·부분 확인은 아래에 별도 표시했습니다.</p><p>이전 회차 19개와 합친 실행기 종료 '+str(len(union))+'/108개 ('+str(summary['combinedWrapperCompletionPercent'])+'%). 이전 기록을 이번 새 실행으로 계산하지 않았습니다.</p><p>실행 묶음 정상 종료 '+str(counts['completed'])+'/'+str(len(jobrows))+'개. 오류 '+str(counts['error'])+'개.</p></div>'
body+='<h2>입력 차단</h2><p>15:38 재확인: Windows 보안 / PickerHost PID 54948 / HWND 2956066이 전면입니다. 보안 창은 조작하지 않았습니다. 사용자에게 직접 처리를 요청했으며 답변 대기 중입니다. 실제 키 입력·클립보드 경로 검사를 통과로 처리하지 않았습니다.</p>'
body+='<h2>확보한 실제 결과</h2><ul><li>Move All: 양쪽 소스 이동, 컴파일·실행·Undo 확인.</li><li>편집기/도구 창 빠른 수정: CA nullptr 적용, 컴파일·실행·Undo 확인.</li><li>색상 인쇄: 양쪽 실제 Print-to-PDF 결과, 텍스트·색상 연산 확인.</li><li>PDB와 RVA: 동일 테스트 바이너리의 PdbTarget103 조회 확인.</li><li>외부 빌드: 양쪽 동일 외부 -I 컴파일·실행 통과. 현재 재검사 정의 이동은 VA 미이동 / CA 외부 헤더 도착.</li><li>누락 case: VA Running/Done 생성 확인. CA의 적용은 미확인.</li></ul>'
body+='<h2>기존 누적 판정과 이번 결과 구분</h2><p>기존 대표 결과 엄격 일치 17/108개(15.74%), 차이 91/108개(84.26%). 이번 재실행 전체에 대한 새 일치율은 아직 산출하지 않았습니다. 이 비율을 제품 전체 또는 UI 유사도로 사용하지 않습니다. 전체 기능 인증 0/108개. 문서 일부 분석 '+str(sum(x.get('analysisStatus')=='documented-partially'for x in spec['features']))+'/108개.</p>'
body+=''.join(images)+'<h2>108개 전체 목록</h2><table><thead><tr><th>ID</th><th>기능</th><th>이번 실행 상태</th><th>검증 범위·부족 사항</th><th>새 실행 근거</th><th>기존 누적 대표 판정</th></tr></thead><tbody>'+''.join(rows)+'</tbody></table><h2>시간·검색</h2><p>제안 시간: 첫 결과 10분 이내. 전체 완료 시각을 보장한 추정은 아니었습니다. 실제 경과 '+str(round(summary['elapsedSeconds']/60,1))+'분. 전체 검사 미완료이므로 완료 시각으로 표시하지 않습니다.</p><p>'+html.escape(summary['xerj'])+'</p><p>'+a('summary.json','기계 판독 결과')+' / '+a('evidence-sha256.json','새 근거 해시 목록')+'</p></html>'
(O/'index.html').write_text(body,encoding='utf-8')
files={}
for p in O.rglob('*'):
 if p.is_file() and p.name not in ['evidence-sha256.json'] and p.stat().st_mtime>=start.timestamp():files[str(p.relative_to(O)).replace('\\','/')]=hashlib.sha256(p.read_bytes()).hexdigest()
(O/'evidence-sha256.json').write_text(json.dumps(files,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in summary.items()if k not in ['jobs','freshWrapperCompletedCatalogIds','qualityGaps']},ensure_ascii=False,indent=2))
