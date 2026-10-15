import pathlib,json,hashlib
r=pathlib.Path('.')
c=json.loads((r/'docs/validation/va-installed-help-catalog.json').read_text(encoding='utf-8'))
root=pathlib.Path(c['installedRoot']);by={x['file']:x for x in c['records']};features={}
def add(name,resources,facts,cases):
 refs=[]
 for rel in resources:
  p='OptionsHelp/'+rel+'.html';assert hashlib.sha256((root/p).read_bytes()).hexdigest()==by[p]['sha256'];refs.append({'file':p,'sha256':by[p]['sha256']})
 features[name]={'resources':refs,'facts':facts,'cases':cases,'unknowns':['Actual control/registry binding and factory default unverified','Original runtime, key routing, Undo grouping and version/language edges unverified']}
add('Auto-Extend Multi-Line Comments',['Editor/CheckExtendComments'],['줄바꿈 시 block comment 및 연속 single-line comment marker를 현재 줄에서 이어받음'],['block·single-line·끝줄·선두공백 비교'])
add('Format After Paste',['Editor/CheckFormatAfterPaste'],['여러 줄 paste 이후 IDE Format Selection으로 서식 지정','Undo로 formatting 철회 가능'],['단일/다중 줄 paste 및 Undo 단계 비교'])
add('Insert () and Closing Brackets and Quotes',['Editor/CheckInsertClosingBrace','Editor/RadioInsertBraceAfterBlankLine','Editor/RadioInsertBraceOnNextLine','Editor/RadioInsertBraceOnSameLine'],['여는 delimiter 뒤 닫는 delimiter·method commit 후 괄호 삽입','Undo/Backspace 철회; 입력 중단 전 inserted delimiter 입력은 해당 문자 덮어쓰기','닫는 brace 위치는 same/next/blank-line 옵션'],['typing 중단·중복닫힘·commit·brace 위치 비교'])
add('Insert _ After m and Shift',['Editor/CheckInsertUnderscoreAfterM'],['m 입력 직후 Shift를 빠르게 눌렀다 놓으면 underscore 삽입'],['Shift 타이밍·연속입력·IME 비교'])
add('Multiple Clipboards',['Editor/CheckDisplayPasteMenu','Editor/EditPasteMenuItems'],['Ctrl+Shift+V 최근 clipboard 메뉴','표시 개수는 별도 설정이며 해당 도움말은 수치를 명시하지 않음'],['capacity·중복·선택·표준paste·Undo 비교'])
add('Repair Case',['Editor/CheckRepairCase'],['typing 중 symbol 대소문자 수정; 여러 교정 후보가 있으면 수정하지 않음','C#은 suggestion/completion 목록 활성 필요'],['단일/복수 후보·C# 조건·Undo 비교'])
add('Convert Dot to ->',['Editor/CheckConvertDotToArrow','Editor/CheckSmartConvertDotToArrow'],['적절한 문맥에서 dot를 arrow로 바꿈; operator-> smart object 변형은 별도 옵션','Delete/Undo/Backspace로 철회'],['pointer·instance·smart object·철회 비교'])
add('Surround Selection',['Editor/CheckSurroundChar1','Editor/CheckSurroundChar2','Editor/CheckSurroundChar3','Editor/CheckSurroundChar4','Editor/CheckSurroundCharSnippet'],['slash는 완전한 줄/복수 줄이면 line comment, 부분 줄이면 block comment 토글','asterisk는 block comment 토글; parentheses/braces surround','surround character가 있는 snippet 입력은 선택을 expansion으로 대체'],['부분/전체/복수 줄·기존주석·snippet·Undo 비교'])
add('Highlight Current Line',['Highlighting/CheckHighlightActiveLine','Highlighting/RadioBackground','Highlighting/RadioBackgroundOnly','Highlighting/RadioDottedBox','Highlighting/RadioRayLines','Highlighting/RadioSolidBox'],['caret 줄 표시; VA Current Line IDE 색 설정 사용','배경·색상/frame·점선·실선·margin까지 ray 형식'],['theme·포커스·색·표시방식 비교'])
add('Highlight Files in Output Window',['Highlighting/CheckHighlightBuildOutput'],['build output에서 solution 파일 경로를 다른 파일 경로와 구별해 강조'],['solution 내부/외부·상대/절대경로·출력갱신 비교'])
add('Highlight Find Results',['Highlighting/CheckHighlightFindResults'],['IDE find 후 editor matches 강조; ESC로 해제; IDE Find Result(VA) 배경색 사용'],['새검색·ESC·문서전환·theme 비교'])
add('Highlight Matching Braces and Parentheses',['Highlighting/CheckAndColorHighlightBraces'],['caret가 scope 안에 있을 때 matching delimiter 강조'],['중첩·문자열/주석 delimiter·caret 경계 비교'])
add('Highlight Mismatching Braces and Parentheses',['Highlighting/CheckAndColorHighlightMismatchingBraces'],['caret 문맥에서 균형이 맞지 않는 delimiter 강조'],['불완전코드·여는/닫는짝·macro 비교'])
add('Underline Spelling Errors',['Underlining/CheckUnderlineErrorsUsing'],['주석과 문자열 spelling 표시; 문맥 메뉴에서 교정 또는 사전 추가'],['언어사전·주석/문자열·사전수명·Undo 비교'])
add('Underline Unknown Symbols',['Underlining/CheckUnderlineMistypedSymbols'],['mistyped/context-invalid symbol 표시; IDE squiggle 표시가 비활성일 때만 적용'],['IDE표시 토글·정의중·include/macro·취소 비교'])
add('VA Memory View',['Debug/CheckNativeDebuggerStepFilter'],['debug session 시작 전에 integration 활성 필요; 완전한 분리는 IDE 재시작 필요'],['세션전/중 토글·재시작·debugger state 비교'])
add('VA Step Filter',['Debug/CheckNativeDebuggerStepFilter','Debug/RadioStepFilterDirAppdata','Debug/RadioStepFilterDirVa'],['integration 활성 시 창 숨김 상태에서도 active filter 적용','solution filter는 APPDATA 또는 .va/shared; global/builtin 상태는 APPDATA','공유 위치에 신규 solution filter 저장하려면 .vadbg 쓰기 권한 필요'],['global/solution 우선순위·창숨김·권한·세션수명 비교'])
base=json.loads((r/'docs/validation/va-feature-baseline.json').read_text(encoding='utf-8-sig'));assert set(features)<={x['name'] for x in base['features']}
a={'scope':'Shipped2630 help constraints, not source implementation or original runtime equivalence','features':features,'count':len(features),'originalHelpHashesMatched':True,'comparisonState':'unexecuted'}
(r/'docs/validation/installed-editor-feature-spec.json').write_text(json.dumps(a,ensure_ascii=False,indent=2),encoding='utf-8')
p=r/'tools/BuildParitySpec.py';s=p.read_text(encoding='utf-8');needle='rows=[]\n'
insert="editorPath=r/'docs/validation/installed-editor-feature-spec.json'\neditor=json.loads(editorPath.read_text(encoding='utf-8')) if editorPath.exists() else None\nif editor:\n for name,contract in editor['features'].items():\n  known[name]={'source':'validation/installed-editor-feature-spec.json','observedSpec':contract['facts'],'unknowns':contract['unknowns'],'comparisonScenarios':contract['cases'],'installedHelpResources':contract['resources']}\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
needle="(r/'docs/integrated-spec.md').write_text"
insert="if editor:\n data['installedEditorFeatureEvidence']='installed-editor-feature-spec.json'\n (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')\n lines+=['','## 설치 편집·수정·강조·debug 옵션 조건','','[17개 기능의 설치 도움말 조건](validation/installed-editor-feature-spec.json)에 실제 파일 SHA-256·적용 조건·비교 사례를 기록했습니다. 원본 도움말 hash는 현재 설치 파일과 일치합니다. 실제 control 바인딩·factory값·Undo/키/실행 결과는 미확인입니다.','']\n for name,contract in editor['features'].items():\n  lines+=['### '+name,'']+['- '+x for x in contract['facts']]+['','원본 비교 대기: '+'; '.join(contract['cases'])+'.','']\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
p.write_text(s,encoding='utf-8');print('Recorded',len(features),'installed-help feature constraints and verified original hashes')
