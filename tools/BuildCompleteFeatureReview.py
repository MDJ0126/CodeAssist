import pathlib,json,re,hashlib,html,datetime,xml.etree.ElementTree as ET,collections
r=pathlib.Path(__file__).resolve().parents[1];v=r/'docs/validation'
spec=json.loads((v/'integrated-spec.json').read_text(encoding='utf-8-sig'))
previous=json.loads((v/'parity-analysis-0.0.86.json').read_text(encoding='utf-8-sig'))
prior={f['id']:f for f in previous['features']}
extra={
2:('CodeAssist.Extension/ReferenceResults.cs','private readonly ListBox list','CA 결과 창은 목록과 결과 수를 제공하며 VA의 D/R/W 필터·프로젝트 트리는 제공하지 않습니다.'),
3:('CodeAssist.Extension/NavigationCommands.cs','NavigationPicker','동일 fixture의 19개 심볼 결과는 실제 일치했습니다. 순서·이름 표기·필터 UI는 다릅니다.'),
4:('CodeAssist.Extension/GoToSymbolCommand.cs','textDocument/definition','clangd 정의 조회 후 인덱스 또는 VS 명령으로 이동합니다. 모든 VA 언어·문맥에서의 목적지 일치는 미검증입니다.'),
8:('CodeAssist.Extension/NavigationCommands.cs','SymbolKind.Function','CA는 현재 파일 함수 5개를 별도 검색창에 표시합니다. VA는 탐색 바 드롭다운입니다.'),
12:('CodeAssist.Extension/NavigationCommands.cs','SourceFiles()','실행 2개 검색 사례와 과거 관찰 10개 결과 집합은 일치합니다. Solution only 및 표시 UI는 다릅니다.'),
16:('CodeAssist.Extension/SemanticRefactoringCommands.cs','reorder only','VA는 시그니처 각 부분 변경을 제공하지만 CA 명령은 순서 변경만 받습니다. 완전 일치가 아닙니다.'),
18:('CodeAssist.Core/CppRefactoring.cs','public static string EncapsulateField','CA 함수는 필드 선언과 getter/setter 문자열만 생성합니다. VA의 프로젝트 참조 갱신과 같지 않습니다.'),
25:('CodeAssist.Extension/CompilerRename.cs','textDocument/rename','clangd rename 작업 편집을 사용합니다. 실제 다중 파일 변경·Undo 및 VA 비교는 미검증입니다.'),
60:('CodeAssist.Extension/CppSemanticClassifier.cs','class','분류기 구현과 현재 화면의 색상은 확인했습니다. 범용 문법·테마·기본색 전수 동등성은 미검증입니다.'),
93:('CodeAssist.Extension/SymbolToolWindows.cs','new SymbolPane(false)','현재 파일 심볼 트리입니다. VA Outline의 전체 편집·정렬·드래그 기능 동등성은 미검증입니다.'),
94:('CodeAssist.Extension/SymbolToolWindows.cs','new SymbolPane(true)','작업 영역 심볼 트리입니다. VA View의 파일·HCB 구성 전체와 같은 구현은 아닙니다.'),
108:('CodeAssist.Extension/ProductOptions.cs','현재 지원하지','작동하는 옵션과 미지원 범주 표시가 공존합니다. 모든 VA 옵션 및 기본값이 구현된 상태가 아닙니다.')}
testfiles=list((r/'CodeAssist.Core.Tests').glob('*.cs'))
trx=ET.parse(v/'test-results/full-comparison-20261017.trx');ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
counter=trx.find('.//t:Counters',ns).attrib
testresults=[{'name':x.attrib['testName'],'outcome':x.attrib['outcome']} for x in trx.findall('.//t:UnitTestResult',ns)]
rows=[]
for f in spec['features']:
 n=int(f['id'][-3:]);p=prior[f['id']];anchors=[]
 candidates=[x['path'] for x in p['sourceChecks']]
 if n in extra:candidates.insert(0,extra[n][0])
 for name in dict.fromkeys(candidates):
  file=r/name
  if not file.is_file():continue
  text=file.read_text(encoding='utf-8-sig');lines=text.splitlines()
  words=[x for x in re.findall(r'[A-Za-z][A-Za-z0-9_]{3,}',f['name']) if x.lower() not in {'file','from','with','code','after','between','selected','symbols'}]
  marker=extra[n][1] if n in extra and name==extra[n][0] else None
  indices=[i for i,l in enumerate(lines) if (marker in l if marker else any(w.lower() in l.lower() for w in words))]
  if not indices: indices=[i for i,l in enumerate(lines) if re.search(r'\b(public|internal|private).*\(',l)][:1]
  for i in indices[:3]:anchors.append({'path':name,'line':i+1,'sha256':hashlib.sha256(file.read_bytes()).hexdigest(),'excerpt':'\n'.join(lines[max(0,i-1):min(len(lines),i+6)]),'kind':'candidate-source-context; not execution proof'})
 scope=extra[n][2] if n in extra else p['implementationScope']
 missing='구체적인 구현 범위 설명 누락' in scope
 linked=[]
 for a in anchors:
  symbols=re.findall(r'\b([A-Z][A-Za-z0-9_]+)\s*\(',a['excerpt'])
  for tf in testfiles:
   source=tf.read_text(encoding='utf-8-sig')
   used=[s for s in symbols if s not in {'Task','Action','Func','Window','Button','CheckBox','ListBox','TextBox','Add','ShowAsync'} and re.search(r'\b'+re.escape(s)+r'\s*\(',source)]
   if used:linked.append({'path':str(tf.relative_to(r)).replace('\\','/'),'symbols':used})
 rows.append({'id':f['id'],'name':f['name'],'officialSource':f['documentationSpec']['source'],'vaRequirements':f['documentationSpec'].get('observedSpec',[]),'caScope':scope,'status':'실행 동등성 미검증' if missing else '부분 대응·제한 기록 / 전체 동등성 미검증','sourceContexts':anchors,'candidateTestFiles':list({t['path']:t for t in linked}.values()),'liveComparison':'live-comparison-20261017.json' if n in [3,12] else None,'unverified':f['documentationSpec'].get('unknowns',[]),'nextScenarios':f['documentationSpec'].get('comparisonScenarios',[]),'fullyEquivalent':False})
assert len(rows)==108 and len({x['id'] for x in rows})==108
report={'version':'0.0.86','reviewedAt':datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=9))).isoformat(),'scope':'108 official-contract/source-context evidence reviews. This is NOT 108 original/product runtime comparisons.','reviewedRows':108,'requiredFeatures':108,'reviewCoveragePercent':100,'overallSimilarityPercent':None,'fullyEquivalentFeatures':0,'coreTests':counter,'sourceContextCount':sum(len(x['sourceContexts']) for x in rows),'rows':rows}
(v/'all-108-review-20261017.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
e=html.escape
css='<style>body{font:16px/1.65 Segoe UI,sans-serif;max-width:1180px;margin:32px auto;padding:16px;background:#181a20;color:#eee}a{color:#ffdb65}details{border:1px solid #444;border-radius:8px;margin:12px 0;padding:14px}summary{cursor:pointer;font-weight:600}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#101217;padding:12px;font-size:13px}input{padding:12px;width:90%;background:#252833;color:white;border:1px solid #777}small{color:#bbc0cc}.warn{color:#ffcd74}li{margin:6px 0}</style>'
out=['<!doctype html><html lang="ko"><meta charset="utf-8"><title>108개 전체 근거 대조</title>'+css,'<h1>108개 전체 근거 대조</h1><p class="warn">108개 행의 문서 요구·현재 코드·제한을 검토했습니다. 108개 실행 대조 완료 또는 동기화율 100%를 뜻하지 않습니다. 전체 유사도는 미확인입니다.</p>',f'<p>부분 명세: 108/108. CA 자체 테스트: {counter["passed"]}/{counter["total"]}. 완전 동등성 인증: 0/108. 후보 테스트 연결은 해당 기능 전체 통과를 뜻하지 않습니다.</p><p><a href="live-comparison-20261017.html">실제 VA·CA 화면 대조</a> · <a href="va-documentation-comparison.html">공식 146개 문서 목차</a> · <a href="validation/all-108-review-20261017.json">전체 원본 JSON</a></p><input id="q" placeholder="기능 이름·ID 검색"><div id="items">']
for f in rows:
 out.append('<details><summary>'+e(f['id']+' · '+f['name'])+'</summary><p class="warn">'+e(f['status'])+'</p><h3>VA 문서의 요구</h3><p><a href="'+e(f['officialSource'],quote=True)+'">공식 문서</a></p><ul>')
 out.extend('<li>'+e(x)+'</li>' for x in f['vaRequirements']);out.append('</ul><h3>CA의 현재 범위·차이</h3><p>'+e(f['caScope'])+'</p><h3>현재 코드 근거 후보</h3>')
 for a in f['sourceContexts']:out.append('<small>'+e(a['path']+':'+str(a['line']))+'</small><pre>'+e(a['excerpt'])+'</pre>')
 out.append('<h3>추가 실행 대조</h3><ul>');out.extend('<li>'+e(x)+'</li>' for x in f['nextScenarios']);out.append('</ul><p>Undo·설정 기본값·언어/VS 버전·성능 전체 검증이 남아 있습니다.</p></details>')
out.append('</div><script>q.oninput=()=>{for(const d of document.querySelectorAll("details"))d.hidden=!d.textContent.toLowerCase().includes(q.value.toLowerCase())}</script></html>')
(r/'docs/all-108-review-20261017.html').write_text('\n'.join(out),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='rows'},ensure_ascii=False))
