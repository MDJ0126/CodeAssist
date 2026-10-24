import json,re,pathlib,html,collections,datetime
r=pathlib.Path(__file__).resolve().parents[1]
v=r/'docs/validation'
raw=(v/'va-official-toc-20261017.txt').read_text(encoding='utf-8-sig')
spec=json.loads((v/'integrated-spec.json').read_text(encoding='utf-8-sig'))
parts=re.split(r'L\d+:\s*',raw)
category=''; topics=[]
for line in parts:
 m=re.search(r'## \[Button: (.*?)\s*\]',line)
 if m: category=m[1].strip()
 for n,title in re.findall(r'cite(\d+)†([^†]+)',line):
  if 5<=int(n)<=150 and category: topics.append({'id':int(n),'category':category,'title':title})
unique={}
for t in topics: unique.setdefault(t['id'],t)
topics=list(unique.values())
ranges=[(16,'Installation'),(30,'Navigation'),(48,'Refactoring'),(63,'Code Generation'),(78,'Coding Assistance'),(91,'Code Understanding'),(98,'Code Correction'),(100,'Code Inspection'),(114,'Code Snippets'),(120,'Debug Assistance'),(124,'Tool Windows'),(128,'Source Links'),(141,'Configuration'),(144,'Unreal Engine'),(147,'Archive'),(150,'Visual Assist Intelligence Function')]
for t in topics:t['category']=next(label for end,label in ranges if t['id']<=end)
def norm(s): return re.sub(r'[^a-z0-9]','',s.lower().replace('visual assist','').replace('va ','').replace('codeassist',''))
for t in topics:
 matches=[f for f in spec['features'] if norm(f['name'])==norm(t['title'])]
 t['mappedFeatures']=[f['id'] for f in matches]
 t['contracts']=[f['documentationSpec'] for f in matches if f.get('documentationSpec')]
 t['status']='부분 구현 명세 연결 · 동등성 미검증' if matches else '기존 108개 목록과 연결 미확인'
 if 'License' in t['title'] or t['title']=='Deactivate': t['status']='VA 상용 라이선스 운영 항목 · CA 적용 여부 별도 판단'
(v/'official-documentation-map-20261017.json').write_text(json.dumps({'source':'https://www.wholetomato.com/documentation','note':'Official documentation is latest-build based; installed VA is 2630. Topic mapping is not implementation proof.','topics':topics},ensure_ascii=False,indent=2),encoding='utf-8')
lines=(r/'.local/pc-verification-client/results.jsonl').read_text(encoding='utf-8').splitlines()
audit=[]; shots=[]
for line in reversed(lines):
 try: p=json.loads(line)
 except ValueError: continue
 content=p.get('result',{}).get('content',[])
 text='\n'.join(x.get('text','') for x in content)
 if 'AUDIT_JSON:' in text:
  audit=json.loads(text.split('AUDIT_JSON:',1)[1]); shots=[x['path'] for x in content if x.get('type')=='image_artifact']; break
assert len(audit)==6, len(audit)
def rows(a):
 if a['side']=='CA': return re.findall(r'dataitem \(selectable\) (.*?) C:\\',a['tree'])
 tree=a['tree']; marker='대화 상자 Find Symbol' if 'Symbol' in a['feature'] else '대화 상자 Open File in Solution'
 tree=tree[tree.index(marker):]
 count=re.search(r'\[(\d+) of \d+\]',tree)
 result=re.findall(r'목록 항목 \(selectable\) ([^\n]+)',tree)
 return result[:int(count[1])] if count else result
def symbols(a): return [x.replace('::','.').replace('operator->','->') for x in rows(a)]
cases=[]
for feature,case in [('Open File in Solution','blank'),('Open File in Solution','Fixture -cpp'),('Find Symbol in Solution','blank')]:
 pair=[next(a for a in audit if a['side']==side and a['feature']==feature and a['case']==case) for side in ['VA','CA']]
 values=[symbols(a) if 'Symbol' in feature else rows(a) for a in pair]
 # Native parent tree also contains IDE combo entries; keep only file names for file picker.
 if 'File' in feature: values=[[x for x in xs if re.fullmatch(r'.*\.(cpp|h)',x)] for xs in values]
 equal=collections.Counter(values[0])==collections.Counter(values[1])
 cases.append({'feature':feature,'case':case,'va':values[0],'ca':values[1],'match':equal})
report={'version':'0.0.86','executedAt':datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=9))).isoformat(),'cases':cases,'observedCases':len(cases),'matchedCases':sum(x['match'] for x in cases),'testedFeatureCount':2,'catalogFeatures':108,'overallSimilarityPercent':None,'fullParityCertifiedFeatures':0,'differences':['Open File: VA has Solution only; CA has no such toggle.','Current observed Fuzzy state: VA selected, CA unselected; factory defaults unverified.','File picker columns, button placement and selection icons differ.','Symbol display punctuation: VA dot and ->, CA :: and operator->; result order differs.','CA file picker title Korean but buttons/column labels English under current mixed language settings.'],'screenshots':shots,'evidence':audit}
(v/'live-comparison-20261017.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
e=html.escape
style='<style>body{font:16px/1.65 Segoe UI,sans-serif;max-width:1150px;margin:36px auto;padding:20px;background:#15171c;color:#e8e9ed}a{color:#ffdc63}details{padding:12px;border-bottom:1px solid #40434a}summary{cursor:pointer}table{width:100%;border-collapse:collapse}td,th{padding:12px;text-align:left;border:1px solid #40434a}td:last-child,th:last-child{min-width:70px;white-space:nowrap}.note{color:#ffc85b}img{max-width:100%}</style>'
page=['<!doctype html><html lang="ko"><meta charset="utf-8"><title>CodeAssist — VA 공식 문서 대조</title>'+style,'<h1>CodeAssist — VA 공식 문서 대조</h1><p><a href="https://www.wholetomato.com/documentation">VA 공식 문서</a>의 분류와 항목 순서를 따릅니다. CA 설명은 독자적으로 작성한 지원 범위와 검증 명세입니다. VA 문서는 최신 빌드 기준이며 설치된 VA 2630과 차이가 있을 수 있습니다.</p>',f'<p>공식 목차 {len(topics)}개 · 기존 기능 명세 108개. 문서 항목 수와 제품 기능 수는 같은 분모가 아닙니다.</p><p><a href="live-comparison-20261017.html">두 실행 프로그램의 실제 대조 결과</a></p>']
last=''
for t in topics:
 if t['category']!=last: last=t['category'];page.append('<h2>'+e(last)+'</h2>')
 page.append(f'<details id="topic-{t["id"]}"><summary>{e(t["title"])}</summary><p class="note">{e(t["status"])}</p>')
 if not t['contracts']:page.append('<p>CA의 이 항목별 사용 절차·실행 결과는 미확인입니다. 문서 제목만으로 지원 또는 일치를 주장하지 않습니다.</p>')
 for c in t['contracts']:
  page.append('<p>참고: <a href="'+e(c.get('source','https://www.wholetomato.com/documentation'),quote=True)+'">VA 해당 문서</a></p><h3>비교할 동작</h3><ul>')
  page.extend('<li>'+e(x)+'</li>' for x in c.get('observedSpec',[]));page.append('</ul><h3>재현·검증 항목</h3><ul>')
  page.extend('<li>'+e(x)+'</li>' for x in c.get('comparisonScenarios',[]));page.append('</ul><h3>아직 확인할 사항</h3><ul>')
  page.extend('<li>'+e(x)+'</li>' for x in c.get('unknowns',[]));page.append('</ul>')
 page.append('</details>')
page.append('</html>');(r/'docs/va-documentation-comparison.html').write_text('\n'.join(page),encoding='utf-8')
p=['<!doctype html><html lang="ko"><meta charset="utf-8"><title>VA·CA 실제 대조</title>'+style,'<h1>VA·CA 실제 대조 — 2026-10-17</h1><p>서로 다른 Visual Studio 프로세스에서 동일한 3파일 fixture를 사용해 직접 실행했습니다. 전체 108개 기능의 동등성이나 성능을 보장하는 결과가 아닙니다.</p>',f'<p>이번 결과 집합 일치: {report["matchedCases"]}/{len(cases)} ({report["matchedCases"]/len(cases)*100:.1f}%). 실행 기능 범위: 2/108 (1.85%). 전체 유사도: 미확인.</p><table><tr><th>기능 / 입력</th><th>VA</th><th>CA</th><th>결과 집합</th></tr>']
for c in cases:p.append('<tr><td>'+e(c['feature']+' / '+c['case'])+'</td><td>'+e(', '.join(c['va']))+'</td><td>'+e(', '.join(c['ca']))+'</td><td>'+('일치' if c['match'] else '불일치')+'</td></tr>')
p.append('</table><h2>실제 차이</h2><ul>');p.extend('<li>'+e(x)+'</li>' for x in report['differences']);p.append('</ul><h2>남은 검증</h2><p>나머지 106개 기능, 편집·Undo·참조 변경 정확성, C# 등 언어별 동작, 양 테마·공장 기본값, 시작/증분 인덱싱 성능을 실행 대조해야 합니다. 기존 108개 부분 구현 매핑은 완전 구현 인증이 아닙니다.</p><p><a href="va-documentation-comparison.html">공식 문서와 같은 목차의 CA 비교 명세</a> · <a href="validation/live-comparison-20261017.json">원본 접근성 트리·결과 JSON</a></p>')
for s in shots:p.append('<img alt="실제 대조 화면" src="validation/'+e(pathlib.Path(s).name)+'">')
p.append('</html>');(r/'docs/live-comparison-20261017.html').write_text('\n'.join(p),encoding='utf-8')
print(json.dumps({'topics':len(topics),'categories':len(set(t['category'] for t in topics)),'mappedTopics':sum(bool(t['mappedFeatures']) for t in topics),'cases':cases},ensure_ascii=False))
