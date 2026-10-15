# 역분석 내용 적용 현황

기준 자료는 [VA 2630 정적 분석](reverse-engineering/va-2630/README.md)입니다. 다음은 그 기록의 P1~P3 후보를 독립 구현한 결과입니다.

| 후보 | 적용 내용 | 검증 |
|---|---|---|
| 문서 종료·취소·버전 관리 | 뷰 수명 연결, 이벤트/Timer 정리, revision 게시 차단, 취소 토큰 | 닫기·재열기·역순 게시 Core 회귀 테스트; VS 뷰 종료 실사용은 미확인 |
| 자동완성 선택 검증 | 항목 문자열+설명 대조, 목록 재생성 후 재선택, 제거된 항목 확정 차단 | 재정렬·삭제·음수 선택 테스트; 실제 VS 입력 확인은 남음 |
| 참조 어휘·범위 정확도 | 공통 문자열/주석 마스킹, 지역 블록 선언 연결, 멤버 접근 구분, unsaved 문서 | escape/raw 문자열·shadowing·현재 문서 테스트 |
| 파일·범위·시그니처 식별 | identity 확장, 캐시 v5, 오버로드 보존, 모호한 타입 미확정, 복수 정의 선택 | 캐시 roundtrip·타입 충돌·namespace·owner 문맥 테스트 |
| 컴파일 구성 모델 | 조건별 include/define/unresolved 값, 헤더 상대 경로 후보, `.vcxproj`/`.Build.cs` fingerprint | 타깃 분리·헤더 경로·해시 갱신 테스트; 최종 MSBuild 평가 기능은 아님 |
| 선택형 작업자 분리 | .NET Framework 작업자, 캐시 결과 검증, 실패 fallback, 종료 취소 | 실제 worker 3회 실행 및 내부 결과 identity 일치 확인 |

기존 Unreal 매크로 지원, 내장 API, 엔진 공용 캐시, IntelliSense 전환 흐름을 유지합니다. 별도 작업자는 합성 측정에서 비용이 더 커 기본 활성화하지 않았습니다.

## 추가 탐색·단축키

파일·심볼·현재 파일 메서드·대응 파일·관련 후보 검색을 추가했습니다. 기본 키 조합은 [단축키 문서](keyboard-shortcuts.md)에 정리했습니다. 이름 변경·빠른 작업은 VS 명령 가용성을 확인하고 호출합니다. 실제 VA 네이티브 리팩터링 코드 또는 Clang DLL을 이식한 것은 아닙니다.

## 남아 있는 한계

- 파서는 경량 어휘·선언 분석입니다. 전처리 조건, 복잡한 템플릿·매개변수·람다·for 초기화 선언의 모든 의미를 처리하지 않습니다.
- 일반 지역 블록을 넘어선 참조는 후보입니다. 전역 동명이인·오버로드 호출을 컴파일러 수준으로 구분하지 않습니다.
- VS C++ 편집기의 기존 Completion API 경로를 유지했습니다. VA에 비동기 Completion API 코드가 있다는 관찰만으로 C++ 편집기 통합 방식을 변경하지 않았습니다.
- VA 네이티브 함수 일부는 디컴파일해 분기를 확인했습니다. 전체 파서 의미·캐시 포맷·런타임 성능·현재 사용자 키 할당의 완전한 복원은 미확인입니다.

## 네이티브·실행 근거에 따른 추가 적용

UDELEGATE, GENERATED_UINTERFACE_BODY, GENERATED_IINTERFACE_BODY, RIGVM_METHOD를 매크로 강조와 내장 완성 후보에 추가했습니다. UDELEGATE 인수에는 기존 specifier 완성 경로를 연결했습니다. 강조는 전체 스냅샷의 주석·문자열을 마스킹하므로 여러 줄 주석에서 잘못 강조하지 않습니다.

탐색 창은 부분 문자열, 공백 AND, 쉼표 OR, `-제외`, `.시작`, `끝.`, `\\경로`, `:행` 검색을 지원합니다. 파일명 기본 검색은 디렉터리명에 자동으로 매칭하지 않습니다. [VA 실제 비교 기록](reverse-engineering/va-2630/runtime/file-search.json)에 저장한 결과 집합을 회귀 테스트로 검증했습니다. 오타를 허용하는 VA fuzzy 검색은 아직 구현하지 않았습니다.

범위 이동은 인접한 모든 여는 중괄호 순회에서 현재 괄호·중괄호의 경계로 이동하도록 수정했습니다. 최상위에서는 인접한 바깥 중괄호 블록을 방문합니다. [단순 함수 실제 비교](reverse-engineering/va-2630/runtime/scope-navigation.json)는 일치하지만 모든 클래스·namespace 구조의 동등성은 미확인입니다. Ctrl+0 줌 초기화는 설치 도움말의 동작 설명과 [VS 2022 IWpfTextView API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.text.editor.iwpftextview?view=visualstudiosdk-2022)를 근거로 구현했고 빌드를 검증했습니다. CodeAssist UI에서 실행 확인은 남아 있습니다.

## 발견한 VA 명령별 누락 관리

[설치 바이너리 명령 문자열 목록](reverse-engineering/va-2630/command-inventory.json)의 30개를 아래처럼 추적합니다. 문자열 목록은 모든 VA 기능의 완전한 목록이 아닙니다. ‘구현’은 CodeAssist 코드가 있다는 의미이고 원본과 전체 동등하다는 의미가 아닙니다.

| VA 명령 | CodeAssist 현재 대응 | 남은 검증·구현 |
|---|---|---|
| GotoImplementation | 구성된 clangd 의미 정의 + 어휘 fallback·복수 후보 선택 | 실제 UE 구성·VA 이동 순서·UI 비교 |
| OpenFileInSolutionDialog | 파일 검색, 10개 VA 결과 비교 | fuzzy·다중 선택·외부 파일 범위·UI 비교 |
| OpenCorrespondingFile | 이름 기반 대응 파일 | 다중 확장자·프로젝트별 우선순위 비교 |
| ListMethodsInCurrentFile | 저장 전 문서 메서드 목록 | 모든 C++ 구문·목록 구조 비교 |
| GotoRelated | owner·base·타입 후보 | override·derived·호출 의미 분석 |
| FindReferences | 구성된 clangd 의미 참조 + 어휘 fallback | 인덱스 준비 상태·닫힌 파일 범위·실제 UE 검증 |
| FindReferencesResults | 도킹 결과·이전 결과 재열기·줄 이동 | UI 실행·VA 결과 그룹/필터 비교 |
| CloneFindReferencesResults | 별도 도킹 창의 결과 스냅샷 복제 | UI 실행·세션 간 결과 보존 |
| NavigateBack, NavigateForward | VS 명령 연결 | VA 방문 기록과 동등성 |
| ScopeNext, ScopePrevious | 어휘 범위 경계 이동 | 복잡한 C++ 범위와 키 충돌 |
| ResetEditorZoom | WPF 뷰 100% 초기화 | CodeAssist UI 실행 확인 |
| RefactorRename | clangd 변경 계획·파일별 preview·원문 대조·Undo, 미구성 시 VS 연결 | 실제 적용/rollback UI 검증·모든 VA 옵션 |
| RefactorContextMenu | VS Quick Actions 연결 | VA 전용 액션 목록 |
| RefactorCreateFromUsage | VS Quick Actions 연결 | 독립 코드 생성과 타깃 선택 |
| VaSnippetInsert | TPL 읽기·변수 입력·preview·삽입·커서 | 전체 reserved 변수·서식·shortcut 자동 확장·편집기 |
| SmartSelectExtend, SmartSelectShrink | 단어·call·괄호·줄·블록 확장과 복원 | 원본 두 확장 단계 비교, 나머지 구문 비교 필요 |
| SmartSelectExtendBlock, SmartSelectShrinkBlock | 중괄호 블록 확장과 복원 | 전체 VA 블록 후보 순서·UI 비교 |
| FindSelected | TextMarker 강조·다음/이전 이동·Esc 해제 | 실제 명령 라우팅·VA 옵션별 동작 비교 |
| Paste | C++ 선택 복사/잘라내기 기록·선택 붙여넣기 | 선택 없는 줄·사각 선택·UI 비교 |
| ListIncludeFiles | 주석/리터럴 제외 directive 탐색·프로젝트 상대 후보 | 외부 SDK·MSBuild 최종 include 해석 |
| VAView, VAOutline | 도킹 창·필터·심볼 이동·변경 갱신 | VA tree 구조·메뉴·드래그 수정·UI 비교 |
| ToggleRepairCase | 미구현 | 문맥에 따른 대소문자 수정 |
| ReparseCurrentFile | 명시 재분석 명령·저장 전 버퍼 심볼 갱신 | VA의 컴파일러/프로젝트 재분석 범위 비교·실제 UI 실행 |
| EnableDisable | CodeAssist 자체 IntelliSense 전환 | VA 전체 기능 on/off와 다른 동작 |
| Options | CodeAssist 자체 설정 | VA 설정 모델과 항목별 비교 |

추가로 설치 도움말에서 FindSymbolDialog와 VAHashtags를 확인했습니다. 심볼 탐색 및 주석 전용 Hashtags 목록·줄 이동을 구현했습니다. 프로젝트 검사·AI agent·spell check·debugger 등도 이 30개 표만으로 분석 완료로 처리하지 않습니다.

## 0.0.49 추가 구현과 증거

설치 `Autotext/Latest/Cpp.tpl`을 읽어 form-feed 레코드 구분, `a:제목:shortcut:` 헤더, 반복 `$변수$`, `$selected$`, `$end$`를 확인했습니다. Core 파서와 변수·커서 계산 및 사용자 TPL 로더를 구현했습니다. 내장 템플릿 7개는 CodeAssist에서 별도로 작성했으며 원본 전체 템플릿을 복제해 배포하지 않습니다. 원본 reserved 변수의 모든 의미와 서식 엔진의 동등성은 미확인입니다.

[VA 공식 reserved string 문서](https://stage.wholetomato.com/it/documentation/code-snippets/va-snippet-reserved-strings)와 대조해 `$DATE$`를 `yyyy/MM/dd`로 수정하고 날짜·시간 값과 영문/지역화 이름, 파일·이름 변수의 UPPER/LOWER 변환을 추가했습니다. 한 번 캡처한 시각으로 확장하며, 0으로 채운 값과 채우지 않은 값을 구분하는 테스트를 통과했습니다. CAMEL/PASCAL 단어 구분, GUID, 심볼 문맥, 프로젝트 및 생성 전용 값은 아직 구현하지 않았습니다. 현재 온라인 문서는 2630 실행과 동일하다는 별도 증거가 아니므로 해당 버전 UI 비교는 미확인입니다.

[원본 Smart Select 직접 메뉴 비교](reverse-engineering/va-2630/runtime/smart-select.json)에서 `GetHealth` → `Actor.GetHealth();`를 확인하고 그 순서를 테스트했습니다. `Shift+Alt+]`는 같은 위치에서 함수 전체를 선택했으므로 이 키 결과는 VA 메뉴 동작의 증거로 사용하지 않았습니다.

Outline·Workspace는 문서/인덱스 변경 이벤트와 150ms 지연 갱신을 사용하고, 창 숨김 시 연결을 해제합니다. 분석이 진행 중이면 추가 요청을 합쳐 중복 분석이 쌓이지 않게 했습니다. 클립보드는 C++ 선택 복사·잘라내기 결과만 메모리에 최대 10개 기록하며 운영체제 클립보드를 주기적으로 읽지 않습니다. 새 UI의 빌드 통과와 실제 설치 UI 실행 동등성은 구분합니다.

## 컴파일러 의미 분석 경로

새 `CodeAssist.Semantics` 모듈은 로컬 clangd와 LSP로 통신합니다. 실제 설치된 clangd **19.1.5**에서 [독립 C++ fixture 검증](validation/clangd-fixture.json)을 수행해 int/double 오버로드 정의 분리, 상속 멤버 완성, 의미 참조, 선언·정의·사용처 세 파일 이름 변경 계획과 주석/문자열 보존을 확인했습니다. 실제 파일을 자동으로 이름 변경한 검증은 아닙니다.

정의·참조·이름 변경과 비동기 완성에 이 경로를 연결했습니다. 완성은 요청한 prefix 범위에 대한 평문 교체만 수용하며 snippet/추가 파일 edit는 제외합니다. 문서·커서·프로젝트 변경, 닫힌 뷰, 취소된 응답은 게시하지 않습니다. 이름 변경은 파일별 미리보기와 적용 전 내용 대조를 거치며, 모든 편집을 먼저 준비하고 [UndoContext](https://learn.microsoft.com/en-us/dotnet/api/envdte.undocontext.setaborted?view=visualstudiosdk-2022)로 묶도록 구현했습니다. Undo와 새 UI는 실제 VS 실행 검증이 남아 있습니다.

`compile_commands.json` 또는 `compile_flags.txt`가 있는 프로젝트에서 사용하고, 명시 `CODEASSIST_USE_CLANGD=1`로도 활성화할 수 있습니다. 컴파일 DB가 있으면 2개 작업 스레드의 background index를 사용하며 `CODEASSIST_CLANGD_BACKGROUND_INDEX=0`으로 끌 수 있습니다. 인덱싱 중이거나 background index를 끄면 닫힌 파일의 정의·참조 범위가 불완전할 수 있습니다. 정확한 UE 빌드 설정 없이 단순 `.vcxproj` 문자열 값을 완전한 컴파일 구성으로 취급하지 않습니다. [clangd 구성·설치 문서](https://clangd.llvm.org/installation)와 대조했습니다.

이 경로는 원본 VA Clang wrapper를 복제한 코드가 아닙니다. VS의 LLVM 실행 파일을 사용하므로 현재 VSIX에 78,457,440바이트의 clangd를 포함하지 않습니다. 작은 패키지 크기는 이 외부 의존성과 호스트 재사용에서 비롯되며, 그 자체가 VA 기능 완성률의 증거가 되지 않습니다.

따라서 VA 전체 기능과 동등하다고 주장하지 않습니다. 이번 버전은 분석에서 근거를 확보한 적용 후보와 대응 탐색 기능을 구현한 버전입니다.

## 범용 목표와 VA 설정 대조

CodeAssist의 목표는 Unreal에 제한되지 않는 범용 개발 보조입니다. Unreal은 주요 테스트 환경이며, 다른 언어의 지원 완료를 의미하지 않습니다. VA 전체의 동작·UI·기본 설정과 동등하게 만드는 것이 목표입니다.

VA 2630의 현재 설치 레지스트리를 [원자료](validation/va-settings.json)에 보존했습니다. 타입 `#FFD700`, 변수 `#BDB76B`, 함수 `#FF8000`, 매크로 `#BD63C5`를 CodeAssist 기본색에 적용하고, 이 색을 VS C++ 기본색으로 덮어쓰던 자동 동기화를 제거했습니다. CodeAssist 분류의 사용자 지정 색은 그대로 설정할 수 있습니다. 클립보드 기본 용량도 관찰값인 10개로 맞췄습니다.

이 값은 현재 설치 설정의 관찰값입니다. 공장 초기값·모든 테마·모든 분류의 동등성은 미확인입니다. 참조 및 검색 강조, namespace·enum 분류, 일반 매크로의 전체 분류 범위, 창 테마와 나머지 설정 항목은 추가 비교·구현이 필요합니다. 설정값을 추출한 것만으로 해당 기능을 구현했다고 처리하지 않습니다.

## VA 동기화 분모 — 2026-10-15

[VA 공식 기능 목록](https://www.wholetomato.com/features)을 브라우저로 확인하고 기능 항목 이름을 전사했습니다. 중복 VA Hashtags를 제거한 **108개 공개 기능 항목**을 비교 기준 v1으로 고정했습니다. [전체 항목과 상태](validation/va-feature-baseline.json). 직접 HTTP 다운로드는 403으로 실패하여 브라우저에서 받은 목록을 사용했습니다. 짧은 기능명 일부는 표기만 정규화했습니다.

**전체 동등성 검증률: 0/108 = 0%. 일부 대응 구현률: 24/108 = 22.2%.** 0%는 코드가 없다는 뜻이 아니라, 기능 전체의 동작·키·UI·기본값·경계 조건을 원본과 대조하여 완전 통과로 인정한 항목이 아직 없다는 뜻입니다. 일부 대응은 기존 소스와 명령 대응표를 근거로 매핑했습니다. 나머지 84개는 대응 미확인 상태이며 전부 미구현이라고 단정하지 않습니다.

108개는 현재 공식 공개 기능 목록의 분모입니다. 설치된 2630의 모든 옵션·개별 키·신규 기능까지 포괄하는 최종 분모는 아닙니다. 버전 가용성과 세부 하위 항목 확인으로 분모를 확장할 때는 기준 버전과 변경 이유를 기록합니다. 게임 중에는 화면 조작·전체 빌드·성능 측정 없이 기존 자동 테스트만 실행했고 **75개가 통과**했습니다.

## 선택한 줄 정렬 추가 — 2026-10-15

[VA 공식 정렬 동작](https://www.wholetomato.com/documentation/coding-assistance/sort-selected-lines)을 근거로 전체 선택 줄을 정렬하는 메뉴 명령을 추가했습니다. 부분 선택한 마지막 줄을 포함하고 EOF 줄에 줄바꿈을 추가합니다. 선택이 다음 줄 시작에서 끝나면 그 줄을 제외합니다. CRLF·중복 줄을 보존하며 정렬 방향과 대소문자 비교 옵션을 제공합니다. 선택이 없으면 변경하지 않습니다. 한 ITextEdit으로 반영하고 변경된 전체 범위를 선택합니다.

Core 회귀 테스트 4개 추가로 현재 79개 테스트가 통과했습니다. VA UI 전체 옵션·기본 선택값·locale 정렬·Undo·실제 VS 실행 비교는 미확인입니다. 이 항목은 일부 구현으로만 표시하며 전체 통과로 올리지 않습니다. **검증 동기화율 0/108 = 0%, 일부 대응 25/108 = 23.1%**입니다. 기존 24/108 수치는 이전 단계 기록입니다.

100% 목표는 작업 목표로 등록했으며 대형 네이티브 분석은 게임 중 보류합니다. 기존 REA 정적 Evidence를 네이티브 디컴파일 완료로 처리하지 않습니다.

## 스니펫 환경·입력 규칙 추가 — 2026-10-15

설치된 Cpp.tpl의 환경 이름 사용을 [해시와 원자료 위치](validation/snippet-input-evidence.json)에 보존했습니다. [공식 입력 규칙](https://www.wholetomato.com/documentation/code-snippets/va-snippet-input-placeholders)과 [환경 규칙](https://www.wholetomato.com/documentation/code-snippets/va-snippet-environment-placeholders)을 대조해 환경을 먼저 확장한 뒤 입력 기본값을 읽도록 구현했습니다. 환경 이름은 대소문자를 구분하지 않고 미정의 값은 빈 문자열로 처리합니다. 확장 패키지 초기화 시 환경을 캡처합니다. 입력 이름은 대소문자를 구분하며 한 번 입력한 값을 반복 위치에서 공유합니다. Upper/Lower는 전체 글자, Pascal/Camel은 첫 글자만 변환합니다. 입력 창은 예약 값을 제외한 입력이 8개를 넘으면 삽입을 막습니다.

이 단계는 REA 네이티브 분석이 아닙니다. 현재 설치 TPL 문법과 온라인 문서 기반 구현이며 2630 실행 동등성은 미확인입니다. 인라인 필드 이동·중첩 스니펫·locale별 변환·전체 예약 변수·VA 입력 창의 크기/키/테마는 남아 있습니다. 회귀 테스트 4개 추가로 전체 83개가 통과했습니다. **검증률 0/108 = 0%, 일부 대응 26/108 = 24.1%**입니다.

## 예약 문자열 범위 확장 — 2026-10-15

[공식 예약 문자열 문서](https://www.wholetomato.com/documentation/code-snippets/va-snippet-reserved-strings)와 설치 TPL의 GUID_STRING 사용을 확인했습니다. 프로젝트/솔루션 파일·이름·확장자·경로 및 UPPER/LOWER를 추가하고 DTE의 실제 문서 소유 프로젝트와 현재 솔루션 이름을 전달합니다. 소유 프로젝트가 없으면 해당 값은 빈 문자열입니다. GUID_STRING은 표준 하이픈 형식, GUID_SYMBOL은 밑줄 형식이며 한 입력 창에서 생성한 하나의 GUID를 공유합니다. 이는 원본 출력의 바이트 단위 동등성 검증을 대신하지 않습니다.

`clipboard`는 해당 변수를 사용하는 템플릿이 실제 선택될 때 읽고 입력 창의 미리보기마다 다시 읽지 않습니다. 테스트는 고정 GUID와 주입한 경로만 사용하며 사용자 환경이나 클립보드 내용을 문서·로그에 보존하지 않습니다. 현재 전체 85개 테스트와 VSIX 패키지 검증을 통과했습니다.

GUID_DEFINITION/GUID_STRUCT의 정확한 출력 형식, reserved CAMEL/PASCAL의 단어 분리, 심볼 문맥 및 코드 생성 전용 값은 남아 있습니다. 설치 자료 존재·문서 명세·구현 테스트와 실제 VA 비교는 각각 구분합니다. **동기화 검증 0/108 = 0%, 일부 대응 26/108 = 24.1%**이며 기존 항목의 깊이를 확장한 것이므로 항목 수를 추가로 올리지 않습니다.
