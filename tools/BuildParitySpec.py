import json,pathlib
r=pathlib.Path(__file__).resolve().parents[1]
base=json.loads((r/'docs/validation/va-feature-baseline.json').read_text(encoding='utf-8-sig'))
common=['동작: 동일 입력·문맥에서 반환/변경 결과를 원본과 비교','편집: 범위·커서·선택·Undo/Redo·저장 전 버퍼 확인','접근: 메뉴·키·문맥 가용성·충돌 확인','설정: 현재 설치 값과 공장 기본값·테마별 값 구분','수명: 취소·문서/프로젝트 변경·창 닫힘 확인','지원: C++ 외 지원 언어·VS/VA 버전·Unreal 변형 확인','성능: 시작·캐시 재사용·편집·검색·메모리 비교']
known={
'Find References':{
 'source':'https://www.wholetomato.com/documentation/navigation/find-references',
 'observedSpec':['창별 D/R/W 필터 및 적용 가능한 필터만 표시','D/R/W는 새 검색마다 해당 결과 전체로 시작','프로젝트 검색 범위는 이전 검색 상태 유지; 지역 변수는 파일 범위 고정','단일 행의 복수 참조는 복수 결과로 표시','상속/override·주석/문자열·생성/구성·scope 결과 필터','결과 내 Ctrl+F·F3·Shift+F3 탐색'],
 'unknowns':['2630의 필터 초기값·정확한 색/레이아웃','주석/문자열 proximity heuristic','참조 인덱스 생성·증분 갱신·캐시 형식']},
'Access to Refactoring Commands':{
 'source':'https://www.wholetomato.com/documentation/refactoring/access-to-refactoring-and-code-generation',
 'observedSpec':['Shift+Alt+Q는 커서·선택 문맥에 맞는 가능한 명령만 표시','적용 가능성에 따라 명령 순서 결정','include·symbol·whitespace 문맥을 구분','편집기·Outline·VA View에서 명령을 제공'],
 'unknowns':['각 명령의 실제 가용성 판정과 정렬 알고리즘','VS Quick Actions 연결과 다른 VA 자체 action pipeline']},
'Options Dialog':{
 'source':'https://www.wholetomato.com/documentation/configuration/options-dialog',
 'observedSpec':['도구 창 문맥 메뉴에도 옵션 존재','다른 IDE 인스턴스는 재시작 전 변경 미반영','문서 screenshot은 최신 VA 기준일 수 있음'],
 'unknowns':['2630 전체 옵션 항목·종속성·공장 초기값','사용자 값 보존·테마 및 VS 버전별 차이']},
'Input Placeholders':{'source':'https://www.wholetomato.com/documentation/code-snippets/va-snippet-input-placeholders','observedSpec':['기본값·반복 값 공유·case-sensitive 이름/수정자·최대8입력','인라인 입력과 문맥에 따른 대화상자 전환'],'unknowns':['2630 인라인 필드 수명·중첩 입력·키/Undo','Unicode/locale별 case 변환']},
'Environment Placeholders':{'source':'https://www.wholetomato.com/documentation/code-snippets/va-snippet-environment-placeholders','observedSpec':['IDE 환경 snapshot','case-insensitive 이름·미정의 빈 값·입력보다 먼저 확장'],'unknowns':['2630 실제 중첩/escape 및 예외 처리']},
'Reserved Strings':{'source':'https://www.wholetomato.com/documentation/code-snippets/va-snippet-reserved-strings','observedSpec':['날짜·파일·프로젝트·솔루션·선택·클립보드·GUID·심볼 문맥·refactor 예약 값'],'unknowns':['전체 GUID formatting bytes','CAMEL/PASCAL 단어 분할','컴파일러 symbol context와 refactor 전용 값']},
'Sort Selected Lines':{'source':'https://www.wholetomato.com/documentation/coding-assistance/sort-selected-lines','observedSpec':['부분 마지막 줄 포함','마지막 줄 EOF이면 EOL 추가','정렬 옵션 대화상자'],'unknowns':['2630 실제 옵션·기본값·locale·stable sort·Undo']}
}
refactorPath=r/'docs/validation/refactoring-generation-spec.json'
refactor=json.loads(refactorPath.read_text(encoding='utf-8-sig')) if refactorPath.exists() else None
if refactor:
 for name,contract in refactor['features'].items():
  known[name]={'source':contract['source'],'observedSpec':contract['facts'],'unknowns':contract['unknowns'],'comparisonScenarios':contract['tests']}
navigationPath=r/'docs/validation/navigation-edit-visual-spec.json'
navigation=json.loads(navigationPath.read_text(encoding='utf-8-sig')) if navigationPath.exists() else None
if navigation:
 for name,contract in navigation['features'].items():
  known[name]={'source':contract['source'],'observedSpec':contract['facts'],'unknowns':contract['unknowns'],'comparisonScenarios':contract['tests']}
editorPath=r/'docs/validation/installed-editor-feature-spec.json'
editor=json.loads(editorPath.read_text(encoding='utf-8')) if editorPath.exists() else None
if editor:
 for name,contract in editor['features'].items():
  known[name]={'source':'validation/installed-editor-feature-spec.json','observedSpec':contract['facts'],'unknowns':contract['unknowns'],'comparisonScenarios':contract['cases'],'installedHelpResources':contract['resources']}
sourceLinksPath=r/'docs/validation/rea/source-links-analysis-summary.json'
sourceLinks=json.loads(sourceLinksPath.read_text(encoding='utf-8')) if sourceLinksPath.exists() else None
if sourceLinks:
 for name,contract in sourceLinks['features'].items():
  known[name]={'source':'validation/rea/source-links-analysis-summary.json','observedSpec':contract['facts'],'unknowns':contract['unknowns'],'comparisonScenarios':contract['cases']}
wholeDocsPath=r/'docs/validation/whole-scope-document-contracts.json'
wholeDocs=json.loads(wholeDocsPath.read_text(encoding='utf-8')) if wholeDocsPath.exists() else None
if wholeDocs:
 for name,contract in wholeDocs['features'].items():
  known[name]={'source':contract['source'],'observedSpec':contract['facts'],'unknowns':contract['unknowns'],'comparisonScenarios':contract['cases']}
rows=[]
for feature in base['features']:
 detail=known.get(feature['name'],{})
 rows.append({'id':feature['id'],'name':feature['name'],'catalogSource':feature['source'],'implementationState':feature['status'],'fullParityPassed':False,'requiredGates':common,'documentationSpec':detail,'nativeEvidence':[],'runtimeEvidence':[],'sourceEvidence':feature.get('sourceEvidence',[]),'implementationScope':feature.get('implementationScope'),'analysisStatus':'documented-partially' if detail else 'analysis-required','unknowns':detail.get('unknowns',['개별 공식 명세·2630 존재·네이티브/runtime 근거·초기값 미확인'])})
runtimePath=r/'docs/validation/historical-runtime-comparison.json'
runtime=json.loads(runtimePath.read_text(encoding='utf-8')) if runtimePath.exists() else None
if runtime:
 for row in rows:
  row['runtimeEvidence']=[item for item in runtime['featureLinks'] if item['featureName']==row['name']]
summary=json.loads((r/'docs/validation/rea/inspection-summary.json').read_text(encoding='utf-8-sig'))
data={'scope':'VA 2630 full reconstruction specification, incomplete analysis; product changes gated until integrated specification is sufficiently evidenced','baselineDenominator':len(rows),'fullParityCount':0,'partialImplementationCount':base['partialMappings'],'completeAnalysisCount':0,'documentedPartialCount':len(known),'features':rows,'sharedEvidence':summary,'designRequirements':['공통 command registry에서 id·menu·key·availability·order를 관리','VS 평가된 컴파일 구성 수집과 semantic backend를 구분','문서 revision·취소·edit preview·multi-document Undo 공통 처리','참조 종류·project scope·필터·clone 상태를 결과 모델에서 관리','설정 저장·테마·초기값·버전별 옵션을 명시적 schema로 관리','원본 observation oracle와 성능 예산을 UI·backend별로 연결'],'scopeLimitations':['108 공개 항목 이외 세부 옵션·신규 기능 추가 가능','정적 CIL coverage complete는 기능 복원 완료가 아님','게임 중 UI·대형 native analysis·성능 비교 보류']}
findingPath=r/'docs/validation/compiler-configuration-findings.json'
flow=json.loads(findingPath.read_text(encoding='utf-8-sig')) if findingPath.exists() else None
if flow:
 data['compilerConfigurationEvidence']='compiler-configuration-findings.json'
 data['designRequirements'].append('구성별 옵션 cache generation 검증·옵션 복사·파일별 override 적용')
 data['scopeLimitations'].append('VaCodeInspectionsAsync user_strings partial: terminal flag 불일치 2건')
pipelinePath=r/'docs/validation/inspection-pipeline-findings.json'
pipeline=json.loads(pipelinePath.read_text(encoding='utf-8-sig')) if pipelinePath.exists() else None
if pipeline:
 data['inspectionPipelineEvidence']='inspection-pipeline-findings.json'
 data['designRequirements']+=['검사 대기 큐 상한·동일 파일 중복 배제·활성 문서 지연·분석 전후 취소 검사','검사 수정의 파일별 겹침 사전 검증·span 갱신·분석 일시 중단/복구']
helpLinksPath=r/'docs/validation/installed-help-feature-links.json'
helpLinks=json.loads(helpLinksPath.read_text(encoding='utf-8-sig')) if helpLinksPath.exists() else None
if helpLinks:
 data['installedHelpCatalog']='va-installed-help-catalog.json'
 data['installedHelpLinks']='installed-help-feature-links.json'
 data['generalParserHelpEvidence']='general-parser-help-findings.json'
 data['nativeProjectReuseReview']='rea/native-project-reuse-review.json'
 byFeature={item['featureId']:item for item in helpLinks['links']}
 for feature in rows:
  if feature['id'] in byFeature:feature['installedHelpCandidates']=byFeature[feature['id']]['candidateHelpResources']
 data['designRequirements'].append('검사 파싱과 다른 기능 파싱의 상태·동시성·옵션을 분리하고 각 서브시스템 기본값을 검증')
if refactor:
 data['refactoringGenerationEvidence']='refactoring-generation-spec.json'
 data['designRequirements']+=['서명 대응·read/write·상속·파일 배치·include 후보를 semantic 모델로 판정하고 명령별 원본 제약을 유지','생성용 snippet·옵션·참조 범위 공유와 다중 파일 편집/Undo를 통합','문서의 비교 시나리오는 실행 전까지 테스트 통과로 표시하지 않음']
(r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
lines=['# VA 통합 분석 명세','', '이 문서는 전체 구현 전에 사용할 분석 명세이며 **분석 완료 문서가 아닙니다**. 기존 108개 공개 기능에 동일한 완료 조건을 적용합니다.','', '[기계 판독 명세](validation/integrated-spec.json) · [REA 검사/통합 근거](validation/rea/inspection-summary.json)','', '## 완료 기준','']+['- '+x for x in common]+['','## REA로 확인한 공통 구조','', '검사 Async DLL에서 타입 776개·메서드 4,843개·CIL call edge 25,967개를 추출했습니다. VS 통합 DLL에서는 타입 9개·메서드 47개·call edge 479개, 평가된 VCProjectEngine property 호출을 확인했습니다. Interop DLL에는 P/Invoke 선언 25개·native implementation 표시 39개가 있습니다. 이 수는 구조 분석 범위이며 기능 완성률이 아닙니다.','', '## 통합 구현 요구','']+['- '+x for x in data['designRequirements']]+['','## 문서 기반 확인 사항','']
for name,detail in known.items():
 lines+=['### '+name,'','[공식 근거]('+detail['source']+')','']+['- '+x for x in detail['observedSpec']]+['','미확인: '+'; '.join(detail['unknowns'])+'.','']
lines+=['## 108개 항목 분석 장부','','| ID | 기능 | 현재 구현 | 분석 상태 |','|---|---|---|---|']
for x in rows:lines.append('| '+x['id']+' | '+x['name'].replace('|','/')+' | '+x['implementationState']+' | '+x['analysisStatus']+' |')
lines+=['','## 현재 상태','',f"원본 유사도는 미측정입니다. 완료 검증 0개, 부분 구현 {base['partialMappings']}개 / 기준 {len(rows)}개입니다. 부분 구현 개수를 원본 유사도로 환산하지 않습니다.",'', '미완료: 전체 네이티브/runtime 근거, 개별 옵션 기본값, 통합 설계 확정, 배치 구현, 실제 UnrealTestProject 성능 및 VA 원본 비교.']
if flow:
 lines+=['','## 컴파일 옵션 캐시의 메서드 수준 분석','','[REA 재구성 요약](validation/rea/compilation-options-reconstruction-summary.json) · [세부 분석 장부](validation/compiler-configuration-findings.json)','']+['- '+claim for claim in flow['findings']]+['','원본을 실행하지 않은 ILSpy 재구성입니다. REA로 원본 token·signature 및 IL 해시를 대조했으며 native 심볼/참조 인덱스의 구현을 입증하는 자료는 아닙니다.','', '미확인: '+'; '.join(flow['unknowns'])+'.']
extras=r/'docs/validation/extra-feature-candidates.json'
if extras.exists():
 lines+=['','현재 문서 목차에서 발견한 [추가 후보 장부](validation/extra-feature-candidates.json)는 설치 2630 가용성과 기존 항목 중복을 확인하기 전까지 기준 분모에 합치지 않습니다.']
if pipeline:
 lines+=['','## 검사 요청·취소·수정 적용 통합 흐름','','[메서드별 분석 장부](validation/inspection-pipeline-findings.json) · [REA 재구성 요약](validation/rea/inspection-pipeline-reconstruction-summary.json)','', '검사·IPC·결과·수정·버퍼 수명 215개와 ParserStats 2개 메서드를 토큰 단위로 재구성하고 REA로 원본 signature 및 IL 해시를 대조했습니다. 원본 코드는 실행하지 않았습니다. 215개 결과에 미해결 타입 주석 30개가 남아 있으므로 전체 의미 복원으로 보고하지 않습니다.','', '| 구간 | 확인한 흐름 |','|---|---|']
 for observation in pipeline['observations']:
  facts='; '.join(observation.get('facts',[])) or str(observation.get('constants',{}))
  lines.append('| '+observation['method']+' | '+facts.replace('|','/')+' |')
 lines+=['', '이 동시성 상한 1개·대기 큐 4개·재확인 250ms·초기 타이머 500ms는 검사 ParserStats의 근거입니다. native 전체 심볼/참조 인덱서와 현재 옵션/실제 타이머를 대신하지 않습니다.','', '미확인: '+'; '.join(pipeline['unknowns'])+'.']
if helpLinks:
 lines+=['','## 설치본 옵션·단축키 도움말 전체 장부','','2630 설치 디렉터리에서 비어 있지 않은 HTML 도움말 278개를 수집했습니다. 옵션 250개·단축키 28개이며 각 파일의 SHA-256·범주·문서 링크를 [설치 도움말 장부](validation/va-installed-help-catalog.json)에 보존했습니다. [11개 기능의 이름 일치 후보](validation/installed-help-feature-links.json)는 실제 command/registry 바인딩 검증을 대신하지 않습니다.','', '[일반 파서·검사 파서 구분 근거](validation/general-parser-help-findings.json): 설치 도움말은 검사 파싱을 다른 VA 기능 파싱과 독립적으로 설명합니다. 일반 파싱에는 멀티스레드·최대 스레드 수·우선순위 설정이 있고, 우선순위 도움말은 Normal을 기본값으로 명시합니다. 일반 파서의 기본 thread 수는 미확인입니다. 도움말에 나타난 재시작 요구도 실제 적용 검증은 남아 있습니다.','', '[REA Ghidra 프로젝트 재사용 검토](validation/rea/native-project-reuse-review.json): 설치된 6.5.0 기본 launcher는 새 rea-project와 -import 경로를 사용합니다. 외부 기존 .gpr 재사용 기능의 전체 지원 여부는 아직 검증하지 않았습니다. 게임 중 native provider를 시작해 재분석하지 않았습니다.','', '도움말 파일명·존재·문구를 실제 옵션 키/가용성/원본 실행 완료로 승격하지 않습니다. 제품 코드를 추가하거나 동기화율을 올리지 않았습니다.']
if refactor:
 lines+=['','## 리팩터링·코드 생성 비교 시나리오','','[12개 기능의 조건·제한·비교 시나리오](validation/refactoring-generation-spec.json)를 공식 문서에서 확인했습니다. 기존 문서 일부 확인 7개와 합쳐 리팩터링 외 항목을 포함한 총수는 상단 기능 표와 기계 판독 명세에 기록합니다. 전체 분석 완료 수는 여전히 0이며 실제 설치된 2630의 가용성/출력/Undo/초기값과 대조해야 합니다.','']
 for name,contract in refactor['features'].items():
  lines+=['### '+name+' 비교','','']+['- '+test+' — 실행 대기' for test in contract['tests']]
 lines+=['','CodeAssist 제품 코드를 새로 구현하지 않았으며 VS Quick Actions 연결을 해당 기능의 구현 완료로 올리지 않았습니다.']
if navigation:
 data['navigationEditVisualEvidence']='navigation-edit-visual-spec.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 탐색·편집·시각 기능 통합 계약','','[12개 항목의 조건·제약·비교 시나리오](validation/navigation-edit-visual-spec.json)는 현재 공식 문서 근거입니다. 설치 2630의 알고리즘·초기값·UI·실행 동등성은 미확인입니다.','']
 for name,contract in navigation['features'].items():
  lines+=['### '+name,'']+['- '+fact for fact in contract['facts']]+['','실행 대기: '+'; '.join(contract['tests'])+'.','']
 lines+=['전체 분석 완료 0개. 제품 코드는 추가하지 않았습니다.']
defaultsPath=r/'docs/validation/installed-help-defaults.json'
if defaultsPath.exists():
 defaults=json.loads(defaultsPath.read_text(encoding='utf-8'))
 data['installedHelpDefaultEvidence']='installed-help-defaults.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 설치 도움말에 명시된 기본값','','[16개 기본값 장부](validation/installed-help-defaults.json)에 원본 파일 SHA-256과 적용 범위를 기록했습니다. 도움말에 명시된 값이며 실제 설정 키와 초기 프로필 동작은 미확인입니다.','', '| 도움말 자원 | 문서 기본값 | 적용 범위 |','|---|---|---|']
 for item in defaults['defaults']:
  lines.append('| '+item['resource']+' | '+str(item['helpDocumentedDefault'])+' | '+item['scope']+' |')
 lines+=['','일반 프로젝트 시작 파싱 기본 활성 및 managed 참조 열거 기본 활성은 성능 비교의 입력 조건입니다. CPU 100% 원인이나 CodeAssist 수정 효과를 입증하지 않습니다.']
keyPath=r/'docs/validation/rea/keybinding-reconstruction-summary.json'
if keyPath.exists():
 key=json.loads(keyPath.read_text(encoding='utf-8'))
 data['keybindingEvidence']='rea/keybinding-reconstruction-summary.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 단축키 설정 정적 분석','','[REA 근거](validation/rea/keybinding-reconstruction-summary.json): VaOptKeyBinds.17.dll의 88개 메서드를 재구성하고 원본 token/signature/IL 해시로 대조했습니다. 원본을 실행하지 않았으며 실제 키 배정과 기능 동등성은 미확인입니다.','']
 lines+=['- '+x['token']+': '+x['finding'] for x in key['findings']]
 lines+=['','충돌 설명 매핑 11개는 VA 기본 단축키 목록이 아닙니다. 실제 배정 루틴·초기값·키 실행 비교는 남아 있습니다.']
cachePath=r/'docs/validation/native-cache-review.json'
if cachePath.exists():
 cache=json.loads(cachePath.read_text(encoding='utf-8'))
 data['nativeCacheReview']='native-cache-review.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 네이티브 캐시 근거 검토','','[원본·재구성 해시 대조 장부](validation/native-cache-review.json): 기존 Ghidra 결과 4개를 현재 VA_X64.dll 해시와 대조했습니다. REA native provider를 새로 실행한 결과는 아닙니다.','']
 lines+=['- '+x for x in cache['observations']]
 lines+=['','FileFinder 통계 함수는 hit/miss·filesystem 호출 회피·include 해석·main/local 및 relative-directory 구분 카운터를 읽습니다. 캐시 lookup·무효화·save/load 알고리즘을 입증하지 않습니다. 저장 추적 문자열의 함수 RVA를 다음 분석 대상으로 특정했습니다.','', '전체 참조 사전 베이크, 설치 파일 크기와 참조 인덱스의 관계, CPU 100% 원인은 미확인입니다.']
savePath=r/'docs/validation/native-cache-save-flow.json'
if savePath.exists():
 data['nativeCacheSaveFlow']='native-cache-save-flow.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 캐시 저장 호출 경로','','[원본 범위 디스어셈블 근거](validation/native-cache-save-flow.json): RVA 0x378A70 및 0x3795A0을 설치 DLL에서 LLVM으로 다시 읽었습니다. 원본 실행이나 REA native provider 실행은 하지 않았습니다.','', '호출부는 object+0x00과 object+0x10의 두 컨테이너를 같은 함수에 전달합니다. callee는 UTF-16 형식 문자열 `%s§%s§%x` 및 newline을 formatting helper에 전달하고, 길이를 두 배로 하여 vtable+0x78의 간접 호출에 넘깁니다. 실제 필드 의미·writer·파일 경로·전체 형식은 미확인입니다.','', '조건부 early return에는 object+0x58에서 얻은 pointer의 길이 후보 및 global 객체의 +0x1EC9 byte가 사용됩니다. 해당 byte를 특정 옵션 이름으로 단정하지 않습니다. load·무효화·오류 처리·실제 출력 bytes 비교는 남아 있습니다.']
snapshotPath=r/'docs/validation/passive-va-data-snapshot.json'
if snapshotPath.exists():
 snap=json.loads(snapshotPath.read_text(encoding='utf-8'))
 data['passiveVADataSnapshot']='passive-va-data-snapshot.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 실제 VA 데이터 경로 읽기 전용 관찰','','[수동 관찰 장부](validation/passive-va-data-snapshot.json): 설치 확장 Data/vs17_1에서 '+str(snap['fileCount'])+'개 파일, '+str(snap['totalBytes'])+' bytes를 관찰했습니다. VA 실행·UI 조작·파일 변경 없이 파일 메타데이터와 지정한 4개 파일의 최대4096byte prefix 해시/형식 지표만 기록했습니다. 내용·개인 경로를 복사하지 않았습니다.','', '| 그룹 | 파일 수 | bytes |','|---|---|---|']
 for group,stats in snap['groupStats'].items():
  lines.append('| '+group+' | '+str(stats['files'])+' | '+str(stats['bytes'])+' |')
 lines+=['','cpp 데이터는 존재하지만 전체 참조 베이크 여부·생성 함수·정확한 스키마·FileFinder 저장과의 연결은 미확인입니다. 기존 파일의 존재는 현재 알고리즘·성능·동등성 검증을 대신하지 않습니다.']
loadPath=r/'docs/validation/native-cache-open-load-flow.json'
if loadPath.exists():
 flow=json.loads(loadPath.read_text(encoding='utf-8'))
 data['nativeCacheOpenLoadFlow']='native-cache-open-load-flow.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 캐시 파일 열기·로드 경로','','[원본 어셈블리·vtable·import 대조](validation/native-cache-open-load-flow.json): writer helper의 vtable+0xB0은 원본 PE에서 RVA0x182D00으로 연결되며 그 함수는 CreateFileW를 호출합니다. helper는 실패 반환0에 대해 Sleep(250)을 최대10회 수행합니다. 실제 실행 스레드와 지연 발생은 미확인입니다.','', '로드 RVA0x377DF0은 저장과 같은 global byte gate와 두 suffix 경로를 사용합니다. loader 호출 후 dword 비교 불일치에 별도 함수 RVA0x3780E0을 호출합니다. 비교값의 의미·reset 대상·무효화 알고리즘은 아직 입증하지 않았습니다.','', 'retry 상한의 존재만으로 VS 시작 버벅임이나 CPU100 원인을 단정하지 않습니다. 실제 VS 추적이 필요합니다.']
headerPath=r/'docs/validation/native-cache-header-reset.json'
if headerPath.exists():
 h=json.loads(headerPath.read_text(encoding='utf-8'))
 data['nativeCacheHeaderReset']='native-cache-header-reset.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 캐시 헤더 계산·초기화','','[함수별 근거](validation/native-cache-header-reset.json): RVA0x32B250은 NUL 종료 UTF-16 단위를 처리하는 32bit rolling 연산입니다. 일반 경로는 129배 누적이며 ASCII 대문자는 별도 packed-bit 처리를 합니다. 대소문자를 단순 정규화하는 hash라고 부르지 않습니다.','', 'RVA0x3780E0은 EnterCriticalSection을 호출하고 두 container의 sentinel/size를 초기화합니다. context helper 출력을 세미콜론으로 결합해 object+0x60을 갱신합니다. helper 출력의 의미와 전체 취소·예외 경로는 미확인입니다.','', 'Python 재구성의 7개 입력 실행은 원본 코드 실행·동등성 검증이 아닙니다. 이 자료로 파일 timestamp/content 변경을 모두 감지한다고 단정하지 않습니다.']
parserPath=r/'docs/validation/native-cache-parser-rules.json'
if parserPath.exists():
 parser=json.loads(parserPath.read_text(encoding='utf-8'))
 data['nativeCacheParserRules']='native-cache-parser-rules.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 캐시 기록 파싱·경계 조건','','[로더 전체 범위와 숫자 helper 근거](validation/native-cache-parser-rules.json): 헤더 newline 후 숫자 outer key·section sign·문자열·section sign·숫자 status를 읽는 정적 구조를 확인했습니다. 숫자 helper 원본 format은 `%x`이고 결과 local을 0으로 초기화합니다. 각 필드의 업무 의미는 미확인입니다.','', '헤더 newline이 없으면 항목 처리 전 cleanup으로 나갑니다. 항목 구분자가 없으면 loop를 종료하고, 마지막 newline이 없으면 buffer 끝을 사용합니다. status 0/0xFFFFFFFF는 invalid branch에서 0xFFFFFFFF 및 빈 associated string으로 처리됩니다. scanner의 실제 permissiveness·overflow·read helper encoding은 미확인입니다.','', '원본 비교 대기: '+ '; '.join(parser['comparisonCases'])+'.','', '저장·헤더·초기화·파일 열기·로드의 부분 근거를 연결했으나 전체 캐시 분석 완료나 semantic 참조 인덱스 복원으로 계산하지 않습니다.']
auditPath=r/'docs/validation/evidence-audit-summary.json'
if auditPath.exists():
 audit=json.loads(auditPath.read_text(encoding='utf-8'))
 data['evidenceProvenanceAudit']='evidence-audit-summary.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 통합 증거 장부 검증','','[증거 연결·해시 검증 결과](validation/evidence-audit-summary.json): '+str(audit['passedCount'])+'개 검증 통과, '+str(audit['failedCount'])+'개 실패입니다. 108개 ID 중복·문서 명세 수·분석 파일 연결·원본 DLL/도움말 해시·88개 단축키 재구성 해시·캐시 어셈블리 해시와 HTML 연결을 대조했습니다.','', '이 수는 근거의 무결성 검사이며 기능 테스트 통과 수가 아닙니다. 개별 기능 장부에 연결된 원본 실행 근거 행은 '+str(audit['runtimeEvidenceRows'])+'개이고 완전 동등성은 '+str(audit['fullParityCount'])+'/108입니다. 기존 별도 runtime 자료는 해당 기능 전체 완료 조건과 대조하기 전까지 동등성 판정으로 승격하지 않습니다.']
if runtime:
 data['historicalRuntimeComparison']='historical-runtime-comparison.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 기존 원본 관찰과 현재 구현의 제한 비교','','[관찰 연결·현재 결과](validation/historical-runtime-comparison.json): Open File in Solution의 기존 입력10개와 현재 NavigationQuery의 세 파일 결과 집합을 대조해10개 통과했습니다. Smart Select 메뉴2단계와 Move Scope4회 관찰도 각 기능 행에 연결했습니다.','', '기존 원본을 이번에 재실행한 자료가 아닙니다. 파일 경로 라벨 변경 이력이 있고 원본 screenshot/fixture snapshot을 독립 재검증하지 않았으므로 그 한계를 보존합니다. Alt+Down은 당시 VS 행 이동으로 처리됐고 Shift+Alt+]도 메뉴 결과와 달라 VA 키 동등성 근거에서 제외합니다.','', 'core 결과 집합의 일치만 확인했으며 picker UI·단축키·옵션·취소·성능 전체 통과로 승격하지 않습니다. 전체 동등성0/108 유지.']
configurationPath=r/'docs/validation/rea/vs-configuration-reconstruction-summary.json'
if configurationPath.exists():
 config=json.loads(configurationPath.read_text(encoding='utf-8'))
 data['vsConfigurationEvidence']='rea/vs-configuration-reconstruction-summary.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## VS 프로젝트·파일 구성 전체 managed adapter','','[REA 재구성 근거](validation/rea/vs-configuration-reconstruction-summary.json): 관리형 VS 통합 DLL의47개 메서드를 token/signature/IL로 대조하고 재구성 파일47개 hash를 검증했습니다. 이는 검사 구성 adapter의 정적 분석 범위이며 일반 native parser 전체 구현이 아닙니다.','']
 lines+=['- '+x['token']+': '+x['fact'] for x in config['findings']]
 lines+=['','활성 구성 변경 통지 대상: '+', '.join(config['changeEventProperties'])+'.','', '원본 비교 대기: '+'; '.join(config['comparisonCases'])+'.','', 'MSBuild 실제 평가·VS property 변경 이벤트·취소/수명 runtime 검증은 미실행입니다. 제품 코드는 추가하지 않았습니다.']
fixturePath=r/'docs/validation/configuration-fixture-evaluation.json'
if fixturePath.exists():
 fixture=json.loads(fixturePath.read_text(encoding='utf-8'))
 data['configurationFixtureEvidence']='configuration-fixture-evaluation.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 조건부 프로젝트·파일 구성 비교 입력','','[독립 MSBuild 평가 결과](validation/configuration-fixture-evaluation.json): import된 BASE 정의·파일별 Debug/Release include·Win32 강제 include·대소문자 중복 파일·header metadata를 포함한 작은 프로젝트를 Debug/x64와 Release/Win32로 평가했습니다. 예상 metadata 검증을 통과했습니다. VA DLL과 현재 CodeAssist product 경로를 실행 비교한 결과는 아닙니다.','', '현재 ProjectCompilationModel source 경로는 PropertyGroup/ItemDefinitionGroup에서 raw값을 읽고 ProjectDir/SolutionDir를 치환합니다. 이 경로의 Import·ItemGroup 파일 설정·조건 평가 누락을 source에서 확인했습니다. 전체 제품의 모든 구성 경로가 없다고 단정하지 않습니다.','']
 lines+=['- '+x for x in fixture['designRequirements']]
 lines+=['','원본 adapter·VCProjectEngine와 같은 입력의 실행 비교가 남아 있으므로 전체 동등성0/108 유지.']
designPath=r/'docs/validation/integrated-architecture-design.json'
if designPath.exists():
 design=json.loads(designPath.read_text(encoding='utf-8'))
 data['integratedArchitectureDesign']='integrated-architecture-design.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 전체108개 기능 통합 구현 설계안','','[기능별 배치·공통 계약](validation/integrated-architecture-design.json)은 CodeAssist의 독립 설계안입니다. 원본 내부 구조·구현 완료·분석 완료를 주장하는 자료가 아닙니다.11개 책임 범위에108개 항목을 중복/누락 없이 배치하고10개 공통 계약과 남은 원본 검증 조건을 연결했습니다.','', '| 모듈 | 기능 범위 | 책임 |','|---|---|---|']
 for module in design['modules']:
  lines.append('| '+module['id']+' | '+str(module['firstFeature'])+'–'+str(module['lastFeature'])+' | '+module['responsibility']+' |')
 lines+=['','### 공통 계약','']
 for contract in design['sharedContracts']:
  lines+=['- **'+contract['name']+'**: '+contract['contract']+' — 미확인: '+contract['unknown']]
 lines+=['','제품 코드를 변경하지 않았습니다. 원본 입력·키·UI·설정·Undo·성능의 전체 분석/비교가 충족되어야 통합 구현 결과를 동등성으로 판정합니다.']
if editor:
 data['installedEditorFeatureEvidence']='installed-editor-feature-spec.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 설치 편집·수정·강조·debug 옵션 조건','','[17개 기능의 설치 도움말 조건](validation/installed-editor-feature-spec.json)에 실제 파일 SHA-256·적용 조건·비교 사례를 기록했습니다. 원본 도움말 hash는 현재 설치 파일과 일치합니다. 실제 control 바인딩·factory값·Undo/키/실행 결과는 미확인입니다.','']
 for name,contract in editor['features'].items():
  lines+=['### '+name,'']+['- '+x for x in contract['facts']]+['','원본 비교 대기: '+'; '.join(contract['cases'])+'.','']
if sourceLinks:
 data['sourceLinksEvidence']='rea/source-links-analysis-summary.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## Source Links·플러그인 정적 분석','','[REA 근거](validation/rea/source-links-analysis-summary.json): 본체208타입/1438메서드/9399call edge, API11타입/75메서드/470call edge를 추출했습니다. 선택66메서드 재구성을 원본token/signature/IL 및 재구성hash로 대조했습니다. 미해결 타입 주석 '+str(sourceLinks['unresolvedTypeWarnings'])+'개가 남아 있습니다.','']
 for name,contract in sourceLinks['features'].items():
  lines+=['### '+name,'']+['- '+x for x in contract['facts']]+['','원본 비교 대기: '+'; '.join(contract['cases'])+'.','']
 lines+=['원본 DLL·plugin·automation helper는 실행하지 않았습니다. 실제 URL 열기·network·credential 접근을 수행하지 않았습니다. 문서 조건과 정적 흐름은 기능 동등성 증명이 아닙니다.']
if wholeDocs:
 data['wholeScopeDocumentEvidence']='whole-scope-document-contracts.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 남은 기능의 공식 계약 일괄 조사','','[공식 문서별 근거·경계 조건](validation/whole-scope-document-contracts.json): 현재공식문서의동작명세이며2630실행완료와구분합니다.','']
 for name,contract in wholeDocs['features'].items():
  lines+=['### '+name,'']+['- '+x for x in contract['facts']]+['','[공식근거]('+contract['source']+') · 원본비교대기: '+'; '.join(contract['cases'])+'.','']
databaseModelPath=r/'docs/validation/shared-symbol-database-model.json'
if databaseModelPath.exists():
 data['sharedSymbolDatabaseModel']='shared-symbol-database-model.json'
 (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 lines+=['','## 심볼 DB 생성·공유 모델: 사전 베이크 가설 수정','','[공식 모델과 설치 모듈 크기](validation/shared-symbol-database-model.json): [공식 Project Directories](https://www.wholetomato.com/documentation/configuration/project-directories)는 미리 만든 심볼 DB를 배포하지 않는다고 설명합니다. system 선언은 solution 간 공유 읽기전용 DB, dynamic 선언은 변경가능 DB에 보관하며 project 최초 열기·header 변경에 파싱합니다.2630 native 내부 schema와 모든 수명 검증은 별도입니다.','', '기존 설치 inventory는1052파일/145,142,212bytes이며 큰 모듈은 valibclang2_64.dll64,157,280bytes, VA_X64.dll21,643,360bytes입니다. 이 크기를 사용자 프로젝트 참조 사전 베이크의 근거로 사용하지 않습니다. 확장자 DB파일이 없다는 관찰만으로 embedded 자료 전체 부재를 입증하지 않습니다.']
(r/'docs/integrated-spec.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Specification rows:',len(rows),'Documented partial rows:',len(known),'Complete analysis:',0)
