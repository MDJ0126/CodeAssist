import pathlib,json,hashlib,re,xml.etree.ElementTree as ET
r=pathlib.Path('.')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
root=r/'docs/reverse-engineering/va-2630/runtime'
files={name:json.loads((root/name).read_text(encoding='utf-8-sig')) for name in ['file-search.json','smart-select.json','scope-navigation.json','key-conflicts.json']}
source=(r/'CodeAssist.Core.Tests/NavigationQueryTests.cs').read_text(encoding='utf-8-sig')
cases={x[0]:x[1].split('|') if x[1] else [] for x in [json.loads('['+m+']') for m in re.findall(r'\[InlineData\((.*?)\)\]',source)]}
for o in files['file-search.json']['observations']:assert cases[o['query']]==o['files']
trx=r/'tools/obj/rea/runtime-comparison/historical-va-file-search.trx'
t=ET.parse(trx);ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'};counts=t.find('.//t:Counters',ns).attrib
assert counts['passed']==counts['total']=='10' and counts['failed']=='0'
rows=[]
for feature,name in [('Open File in Solution','file-search.json'),('Smart Select','smart-select.json'),('Move Scope','scope-navigation.json')]:
 rows.append({'featureName':feature,'observationPath':'../reverse-engineering/va-2630/runtime/'+name,'observationSHA256':sha(root/name),'version':files[name]['version'],'observationCount':len(files[name]['observations']),'state':'historical-partial-original-observation','codeAssistComparison':'10 filter result-set cases passed; actual CodeAssist picker UI not compared' if name=='file-search.json' else 'current product result not rerun against this observation','fullParityPassed':False})
a={'scope':'Historical original UI observations joined to current limited core filter comparison; no original VA rerun','featureLinks':rows,'keyConflictEvidence':{'path':'../reverse-engineering/va-2630/runtime/key-conflicts.json','sha256':sha(root/'key-conflicts.json'),'finding':'Alt+Down routed to VS MoveSelectedLinesDown in recorded profile; cannot count as VA ScopeNext key proof'},'currentFilterComparison':{'cases':10,'passed':10,'failed':0,'trxSHA256':sha(trx),'testSourceSHA256':sha(r/'CodeAssist.Core.Tests/NavigationQueryTests.cs'),'implementationSHA256':sha(r/'CodeAssist.Core/NavigationQuery.cs'),'candidateFiles':['FixtureActor.cpp','FixtureActor.h','main.cpp']},'fixtureCurrentSHA256':sha(r/'tests/VAParityFixture/main.cpp'),'limitations':['Historical path labels were renamed; hash refers to current evidence file, not immutable original capture.','Original screenshot provenance and exact fixture snapshot at capture not independently revalidated.','Core filter test does not compare focus, key routing, selection, settings, asynchronous loading or performance.','Smart Select has only two observed menu steps; shortcut observation differs and is excluded.','Scope navigation has one simple fixture and four movements only.']}
(r/'docs/validation/historical-runtime-comparison.json').write_text(json.dumps(a,ensure_ascii=False,indent=2),encoding='utf-8')
p=r/'tools/BuildParitySpec.py';s=p.read_text(encoding='utf-8');needle="summary=json.loads"
insert="runtimePath=r/'docs/validation/historical-runtime-comparison.json'\nruntime=json.loads(runtimePath.read_text(encoding='utf-8')) if runtimePath.exists() else None\nif runtime:\n for row in rows:\n  row['runtimeEvidence']=[item for item in runtime['featureLinks'] if item['featureName']==row['name']]\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
needle="(r/'docs/integrated-spec.md').write_text"
insert="if runtime:\n data['historicalRuntimeComparison']='historical-runtime-comparison.json'\n (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')\n lines+=['','## 기존 원본 관찰과 현재 구현의 제한 비교','','[관찰 연결·현재 결과](validation/historical-runtime-comparison.json): Open File in Solution의 기존 입력10개와 현재 NavigationQuery의 세 파일 결과 집합을 대조해10개 통과했습니다. Smart Select 메뉴2단계와 Move Scope4회 관찰도 각 기능 행에 연결했습니다.','', '기존 원본을 이번에 재실행한 자료가 아닙니다. 파일 경로 라벨 변경 이력이 있고 원본 screenshot/fixture snapshot을 독립 재검증하지 않았으므로 그 한계를 보존합니다. Alt+Down은 당시 VS 행 이동으로 처리됐고 Shift+Alt+]도 메뉴 결과와 달라 VA 키 동등성 근거에서 제외합니다.','', 'core 결과 집합의 일치만 확인했으며 picker UI·단축키·옵션·취소·성능 전체 통과로 승격하지 않습니다. 전체 동등성0/108 유지.']\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
p.write_text(s,encoding='utf-8')
print('Linked3 partial historical feature observations; independently matched10 test expectations and TRX results')
