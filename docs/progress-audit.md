# 원본 유사도·구현 기능 수 — 0.0.73

|지표|현재 결과|
|---|---|
|VA 원본과 유사도|미측정 — 전체 원본 비교 근거가 없어 백분율 확정 불가|
|구현 기능 수|완료 검증 0개 · 부분 구현 108개 / 기준 108개|

부분 구현 수는 소스 대응을 확인한 절대 개수이며, 유사도나 완성률이 아닙니다. 이전 장부에서 누락된 기존 코드도 포함합니다. 완료 검증 0개는 기능이 하나도 작동하지 않는다는 뜻이 아니라, 각 항목의 전체 검증 조건을 통과한 근거가 아직 없다는 뜻입니다. 목표 80%는 아직 달성하지 않았습니다.

## 이번 통합 변경

- 자동 괄호·따옴표와 중괄호 위치, 함수 괄호 간격 옵션을 추가했습니다.
- 주석 이어쓰기, 문자로 선택 영역 감싸기, 붙여넣기 후 VS 서식 적용을 옵션에 연결했습니다.
- Smart Select의 작은 단위 확장·현재 단어 시작·대소문자 경계·밑줄 경계를 독립 옵션으로 연결했습니다.
- 심볼 후보 Quick Info와 한국어/영어 번역 항목을 추가했습니다.
- 기본 실행에서 꺼진 제한적 입력 진단을 추가했습니다.

## 실제 확인과 남은 검증

회귀 테스트 164개 통과 및 VSIX 바이너리 6개 일치 확인은 전체 기능 개수에 가산하지 않습니다. 두 번째 도구 모음 버튼으로 파일 찾기 창을 실제 열었습니다. Welcome은 문서 탭에 표시됩니다. 실제 VS 명령 체인에서 괄호 입력·삭제와 Undo/Redo 커서, 선택 감싸기와 Undo/Redo 선택 복원, 주석 전환·이어쓰기, 중괄호 배치의 14개 시나리오가 모두 통과했습니다. 0.0.72의 주석 예외를 재현한 실패 기록과 0.0.73 수정 후 결과를 모두 보존했습니다. 동작이 확인되지 않은 항목을 통과로 승격하지 않았습니다.

REA MCP로 원본 DLL의 대화상자 리소스를 읽고 SHA256을 검증한 뒤 Editor와 Smart Select의 순서·문구·컨트롤 ID를 확인했습니다. 리소스 추출은 전체 바이너리 디컴파일이나 기능 복원 완료를 의미하지 않습니다.

[기능 검증표](validation/feature-verification-matrix.json) · [원본 옵션 리소스 근거](validation/rea/options-pe-resources-20261015.json) · [해석한 원본 대화상자](validation/rea/options-dialog-layout-20261015.json) · [회귀 테스트 원본](validation/test-results/integrated-editor-0.0.70.trx)

## 기존 부분 구현 38개

|ID|기능|
|---|---|
|VA-F001|Find by Context|
|VA-F002|Find References|
|VA-F003|Find Symbol in Solution|
|VA-F004|GoTo Implementation|
|VA-F006|GoTo Related|
|VA-F008|List Methods in File|
|VA-F009|Move Scope|
|VA-F010|Navigate Back and Forward|
|VA-F011|Open Corresponding File|
|VA-F012|Open File in Solution|
|VA-F013|VA Hashtags|
|VA-F014|VA Navigation Bar|
|VA-F015|Access to Refactoring Commands|
|VA-F025|Rename|
|VA-F037|Create from Usage|
|VA-F043|Auto-Extend Multi-Line Comments|
|VA-F044|Enhanced Listboxes|
|VA-F045|Format After Paste|
|VA-F046|Insert () and Closing Brackets and Quotes|
|VA-F048|Multiple Clipboards|
|VA-F049|Reset Editor Zoom|
|VA-F051|Smart Select|
|VA-F053|Sort Selected Lines|
|VA-F055|Surround Selection|
|VA-F060|Enhanced Syntax Coloring|
|VA-F063|Highlight Matching Words|
|VA-F067|Quick Info|
|VA-F075|Underline Unknown Symbols|
|VA-F076|Code Inspection Settings|
|VA-F077|Code Issues in the Editor|
|VA-F080|VA Snippets|
|VA-F086|Environment Placeholders|
|VA-F087|Input Placeholders|
|VA-F088|Reserved Strings|
|VA-F093|VA Outline|
|VA-F094|VA View|
|VA-F103|Keyboard Shortcuts|
|VA-F108|Options Dialog|

[실제 VS 편집 통합 14개 시나리오](validation/editor-integration-0.0.73.json) · [수정 전 실패 기록](validation/editor-integration-0.0.72-failed.json) · [설치 해시 검증](validation/installed-0.0.73.json)

[원본 VA 비활성·CodeAssist 활성 상태 확인](validation/va-isolation-0.0.73.json). 원본 동일 입력 비교는 수행하지 않았습니다.


## 0.0.74 추가 부분 구현

초기 미대응 70개 중 26개에 소스 구현을 추가했습니다. 미대응 44개가 남았습니다. [상세 장부](remaining-features.md)와 [빌드·자동 검증 기록](validation/feature-batch-0.0.74.json)을 참조하세요. 자동 테스트 206개 통과, Release 전체 빌드 성공, 패키지 바이너리 6개 해시 및 키 등록 24개 확인. 원본 유사도와 신규 기능의 실제 IDE 동작은 미검증입니다.


## 0.0.75 최신 소스 상태

부분 대응 93개/108개, 초기 70개 중 55개 부분 대응 및 15개 미구현. 완료 검증 0개이며 원본 유사도는 미측정입니다. [검증 범위와 제한](validation/feature-batch-0.0.75.json)을 확인하세요.


## 0.0.76 최종 소스 대응

초기 70개 모두 기본 소스를 연결해 전체 108개가 부분 대응입니다. **미대응 소스 0개와 완료 검증 108개는 다릅니다.** 완료 검증 0개, 원본 유사도 미측정. 자동 테스트 247개와 실제 C++ 구문 6개가 통과했습니다. [지원 제한·검증 대기](validation/feature-batch-0.0.76.json).
