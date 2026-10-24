import pathlib,json,re,collections,html
r=pathlib.Path(__file__).resolve().parents[1];v=r/'docs/validation';p=v/'live-comparison-20261017.json'
d=json.loads(p.read_text(encoding='utf-8'));q=json.loads((v/'quick-live-comparison-20261017.json').read_text(encoding='utf-8'))
ca=next(x['tree'] for x in q if x['side']=='CA' and x['feature']=='List Methods in File')
va=next(x['tree'] for x in q if x['side']=='VA' and x['feature']=='List Methods in File')
can=re.findall(r'dataitem \(selectable\) (.*?) C:\\',ca)
van=re.findall(r'목록 항목 \(selectable\) ([A-Za-z_][A-Za-z0-9_:]+)\(',va)
assert len(can)==len(van)==5
new=[{'feature':'List Methods in File','case':'FixtureActor.cpp: method names and overload multiplicity','va':van,'ca':can,'match':collections.Counter(can)==collections.Counter(van)}]
dest=[]
for side in ['VA','CA']:
 t=next(x['tree'] for x in q if x['side']==side and x['case']=='toolbar-cpp-to-h')
 dest.append(re.search(r'Window: "VAParityFixture - ([^ -]+)',t)[1])
new.append({'feature':'Open Corresponding File','case':'toolbar: cpp to h','va':[dest[0]],'ca':[dest[1]],'match':dest[0]==dest[1]=='FixtureActor.h'})
d['cases']=[x for x in d['cases'] if x['feature'] not in ['List Methods in File','Open Corresponding File']]+new
d['observedCases']=len(d['cases']);d['matchedCases']=sum(x['match'] for x in d['cases']);d['testedFeatureCount']=4
d['additionalEvidence']='quick-live-comparison-20261017.json'
d['differences']+=['Method list: VA navigation-bar dropdown, CA separate picker dialog.','Observed Alt+O did not change either active file in this automation attempt; toolbar buttons changed both to FixtureActor.h. Keyboard routing is unverified.','Reference results: CA has a plain list; VA document specifies D/R/W filters and project grouping.','Change Signature: CA accepts reorder only; official VA supports editing other signature parts.','Encapsulate Field: CA generates accessor text; official VA also updates project references.']
d['differences']=list(dict.fromkeys(d['differences']));p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
h=r/'docs/live-comparison-20261017.html';s=h.read_text(encoding='utf-8')
s=s.replace('3/3 (100.0%)','5/5 (100.0%)').replace('2/108 (1.85%)','4/108 (3.70%)').replace('나머지 106개 기능','나머지 104개 기능')
extra='<section><h2>추가 실행 대조</h2><p>메서드 목록 5개(오버로드 중복 포함), 도구 모음의 cpp→h 이동이 양쪽에서 일치했습니다. 메서드 목록 UI는 서로 다릅니다. Alt+O 키 경로는 이번 자동화에서 확인하지 못했습니다.</p><p><a href="all-108-review-20261017.html">108개 전체 문서 요구·현재 코드·차이·미검증 표</a></p><p>참조 필터·시그니처 변경 범위·캡슐화 참조 치환에 기능 차이가 있습니다. 전체 동기화율은 산정하지 않았습니다.</p></section>'
if '<h2>추가 실행 대조</h2>' not in s:s=s.replace('<h2>실제 차이</h2>',extra+'<h2>실제 차이</h2>')
h.write_text(s,encoding='utf-8')
for name in ['index.html','handoff.html']:
 h=r/'docs'/name;s=h.read_text(encoding='utf-8');section='<section style="margin-left:280px;padding:16px"><h2>전체108개 근거 검토 · 실행 대조 4개</h2><p>문서 요구284개·현재 소스281개 문맥을 연결했습니다. 실행 결과5/5 일치, CA 자체267/267 통과. 전체 동기화율은 미확인입니다.</p><a href="all-108-review-20261017.html">전체108개 상세표</a> · <a href="live-comparison-20261017.html">실행 근거와 차이</a></section>'
 if '전체108개 근거 검토 · 실행 대조 4개' not in s:s=s.replace('<body>','<body>'+section,1)
 h.write_text(s,encoding='utf-8')
print('Live comparisons:',d['matchedCases'],'/',d['observedCases'],'features',d['testedFeatureCount'],'/108. Full parity remains unverified.')
