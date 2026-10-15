# VA 통합 분석 명세

이 문서는 전체 구현 전에 사용할 분석 명세이며 **분석 완료 문서가 아닙니다**. 기존 108개 공개 기능에 동일한 완료 조건을 적용합니다.

[기계 판독 명세](validation/integrated-spec.json) · [REA 검사/통합 근거](validation/rea/inspection-summary.json)

## 완료 기준

- 동작: 동일 입력·문맥에서 반환/변경 결과를 원본과 비교
- 편집: 범위·커서·선택·Undo/Redo·저장 전 버퍼 확인
- 접근: 메뉴·키·문맥 가용성·충돌 확인
- 설정: 현재 설치 값과 공장 기본값·테마별 값 구분
- 수명: 취소·문서/프로젝트 변경·창 닫힘 확인
- 지원: C++ 외 지원 언어·VS/VA 버전·Unreal 변형 확인
- 성능: 시작·캐시 재사용·편집·검색·메모리 비교

## REA로 확인한 공통 구조

검사 Async DLL에서 타입 776개·메서드 4,843개·CIL call edge 25,967개를 추출했습니다. VS 통합 DLL에서는 타입 9개·메서드 47개·call edge 479개, 평가된 VCProjectEngine property 호출을 확인했습니다. Interop DLL에는 P/Invoke 선언 25개·native implementation 표시 39개가 있습니다. 이 수는 구조 분석 범위이며 기능 완성률이 아닙니다.

## 통합 구현 요구

- 공통 command registry에서 id·menu·key·availability·order를 관리
- VS 평가된 컴파일 구성 수집과 semantic backend를 구분
- 문서 revision·취소·edit preview·multi-document Undo 공통 처리
- 참조 종류·project scope·필터·clone 상태를 결과 모델에서 관리
- 설정 저장·테마·초기값·버전별 옵션을 명시적 schema로 관리
- 원본 observation oracle와 성능 예산을 UI·backend별로 연결
- 구성별 옵션 cache generation 검증·옵션 복사·파일별 override 적용
- 검사 대기 큐 상한·동일 파일 중복 배제·활성 문서 지연·분석 전후 취소 검사
- 검사 수정의 파일별 겹침 사전 검증·span 갱신·분석 일시 중단/복구
- 검사 파싱과 다른 기능 파싱의 상태·동시성·옵션을 분리하고 각 서브시스템 기본값을 검증
- 서명 대응·read/write·상속·파일 배치·include 후보를 semantic 모델로 판정하고 명령별 원본 제약을 유지
- 생성용 snippet·옵션·참조 범위 공유와 다중 파일 편집/Undo를 통합
- 문서의 비교 시나리오는 실행 전까지 테스트 통과로 표시하지 않음

## 문서 기반 확인 사항

### Find References

[공식 근거](https://www.wholetomato.com/documentation/navigation/find-references)

- 창별 D/R/W 필터 및 적용 가능한 필터만 표시
- D/R/W는 새 검색마다 해당 결과 전체로 시작
- 프로젝트 검색 범위는 이전 검색 상태 유지; 지역 변수는 파일 범위 고정
- 단일 행의 복수 참조는 복수 결과로 표시
- 상속/override·주석/문자열·생성/구성·scope 결과 필터
- 결과 내 Ctrl+F·F3·Shift+F3 탐색

미확인: 2630의 필터 초기값·정확한 색/레이아웃; 주석/문자열 proximity heuristic; 참조 인덱스 생성·증분 갱신·캐시 형식.

### Access to Refactoring Commands

[공식 근거](https://www.wholetomato.com/documentation/refactoring/access-to-refactoring-and-code-generation)

- Shift+Alt+Q는 커서·선택 문맥에 맞는 가능한 명령만 표시
- 적용 가능성에 따라 명령 순서 결정
- include·symbol·whitespace 문맥을 구분
- 편집기·Outline·VA View에서 명령을 제공

미확인: 각 명령의 실제 가용성 판정과 정렬 알고리즘; VS Quick Actions 연결과 다른 VA 자체 action pipeline.

### Options Dialog

[공식 근거](https://www.wholetomato.com/documentation/configuration/options-dialog)

- 도구 창 문맥 메뉴에도 옵션 존재
- 다른 IDE 인스턴스는 재시작 전 변경 미반영
- 문서 screenshot은 최신 VA 기준일 수 있음

미확인: 2630 전체 옵션 항목·종속성·공장 초기값; 사용자 값 보존·테마 및 VS 버전별 차이.

### Input Placeholders

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippet-input-placeholders)

- 기본값·반복 값 공유·case-sensitive 이름/수정자·최대8입력
- 인라인 입력과 문맥에 따른 대화상자 전환

미확인: 2630 인라인 필드 수명·중첩 입력·키/Undo; Unicode/locale별 case 변환.

### Environment Placeholders

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippet-environment-placeholders)

- IDE 환경 snapshot
- case-insensitive 이름·미정의 빈 값·입력보다 먼저 확장

미확인: 2630 실제 중첩/escape 및 예외 처리.

### Reserved Strings

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippet-reserved-strings)

- 날짜·파일·프로젝트·솔루션·선택·클립보드·GUID·심볼 문맥·refactor 예약 값

미확인: 전체 GUID formatting bytes; CAMEL/PASCAL 단어 분할; 컴파일러 symbol context와 refactor 전용 값.

### Sort Selected Lines

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/sort-selected-lines)

- 부분 마지막 줄 포함
- 마지막 줄 EOF이면 EOL 추가
- 정렬 옵션 대화상자

미확인: 2630 실제 옵션·기본값·locale·stable sort·Undo.

### Change Signature

[공식 근거](https://www.wholetomato.com/documentation/refactoring/change-signature)

- 함수 이름 위치에서 선언·정의·참조 및 호출부 변경; 다중 파일 Undo 지원
- 인수 이름 변경은 선언/정의, 인수 추가는 호출부까지 변경
- 기본 인수를 서명에 추가하면 기존 호출부를 변경하지 않음; 추가 인수 호출값 기본은 TODO
- 모호한 변경은 OK 비활성화; 이름 변경과 재정렬은 구분
- C++ 가시성 변경은 지원하지 않음; 문서는 overload·system-directory 제한을 명시
- 프로젝트 검색 범위는 Find References·Rename과 공유

미확인: 2630 실제 overload 가용성 및 ambiguity 판정; 호출부 인수 대응·매크로·상속·템플릿 처리; 정확한 다중 파일 Undo·UI·locale·서식.

### Extract Method

[공식 근거](https://www.wholetomato.com/documentation/refactoring/extract-method)

- 선택 코드와 주변 문맥에서 인수/반환형을 추론하고 원래 위치를 호출로 대체
- 전역/멤버와 새 함수 내부 로컬은 인수에서 제외
- 읽기값은 값 전달, 쓰기값과 dot로 참조되는 객체는 참조 전달
- 단일 변경 값은 반환값으로 원래 함수에서 대입할 수 있음

미확인: 탈출·return·goto·예외·템플릿 문맥; 설정 기반 생성 위치·주석·서식·수정자; 2630의 부작용과 type inference 상세.

### Create from Usage

[공식 근거](https://www.wholetomato.com/documentation/code-generation/create-from-usage)

- class·enum·변수·함수 및 local/class/global 생성 선택
- 클래스 생성은 생성자 인수에서 멤버/초기화 추론; 파일명 변경 가능
- 미확정 타입은 UnknownType; 생성 문법/본문은 언어별 스니펫
- 인수 생성은 선언/정의에 반영; enum 사용 문맥 및 scope 구분

미확인: 2630 target inference와 후보 순위; 기본 생성 본문·명명·파일 배치; 복수파일·템플릿·const·overload 추론.

### Implement Interface / Virtual Methods

[공식 근거](https://www.wholetomato.com/documentation/code-generation/implement-interface-virtual-methods)

- 깊은 상속과 다중 상속의 메서드 후보; 구현된 것은 선택 불가
- pure virtual은 기본 체크, 나머지는 기본 미체크
- 필터를 바꿔도 체크 상태 유지; 기본 정렬은 선언 순서
- virtual/override 생성 키워드와 본문 snippet 설정

미확인: 인터페이스 자동 전체 적용과 pure-virtual 선택창의 정확한 조건; 중복 signature·diamond 상속·template 처리; 2630 키워드 기본값·후보 가시성.

### Encapsulate Field

[공식 근거](https://www.wholetomato.com/documentation/refactoring/encapsulate-field)

- C++ 선언 위치에서 getter/setter 생성; 이름 공백이면 해당 accessor 생략
- 필드 가시성 조정; private/protected 필드의 accessor는 기본 public
- 참조 선택 범위를 조절해 읽기/쓰기를 accessor로 변경
- 접두사 제거·대문자화 및 스니펫 GeneratedPropertyName으로 accessor 이름 구성
- C++ accessor는 선언 헤더에 배치; 배열/char pointer는 수동 보완이 필요할 수 있음

미확인: 2630 명명 규칙·복합 increment/decrement 처리; 배열·참조반환·template·visibility 레이아웃; 커서·Undo·정확한 서식.

### Introduce Variable

[공식 근거](https://www.wholetomato.com/documentation/refactoring/introduce-variable)

- 표현식의 가장 안쪽 scope에 변수 선언 후 선택 표현식 교체
- 반복 교체는 선택 이후의 현재 scope 후보에 한정
- 필요하면 한 줄 if/else/while에 braces 추가
- 빈 공간에서도 함수 반환값 capture에 이용 가능

미확인: 표현식 동등성·type inference·부작용 처리; scope 중첩·macro·brace 설정; 2630 기본 이름과 서식.

### Add Missing Case Statements

[공식 근거](https://www.wholetomato.com/documentation/code-generation/add-missing-case-statements)

- enum switch 안에서 누락된 enum case 추가; switch 키워드 자체 위치는 필수 아님

미확인: 2630 삽입 순서·본문·default·중복 값·scoped enum; nested switch 구분과 가용성.

### Convert Between Pointer and Instance

[공식 근거](https://www.wholetomato.com/documentation/refactoring/convert-between-pointer-and-instance)

- 지역 변수 선언에서 pointer/instance 전환 및 참조의 dot/arrow·dereference/address 수정
- pointer→instance는 선택한 참조를 변환하고 delete 참조 제거
- instance→pointer는 raw/unique/shared/custom 생성 선택; 기본 auto
- 지원 불가 문맥에서는 안내

미확인: 2630 lifetime·custom template·복합 참조 적용 규칙; 후보 판정·smart pointer 서식·Undo.

### Create Declaration

[공식 근거](https://www.wholetomato.com/documentation/code-generation/create-declaration)

- C/C++ 정의에서 일치 인수의 선언을 이웃 선언 근처 헤더에 생성
- 적절한 헤더 없으면 source에 생성
- 정의에서 주석이거나 없는 default 인수는 자동 노출하지 않음

미확인: 2630 파일/위치 선정·template·qualifier; forward declaration/중복 선언 가용성·Undo.

### Create Implementation

[공식 근거](https://www.wholetomato.com/documentation/code-generation/create-implementation)

- C/C++ 메서드/정적 멤버 선언에서 stub 생성
- 기본 인수는 기본적으로 block comment로 구현 인수에 유지; 옵션으로 생략
- 이웃 구현 근처/EOF 위치 설정; source 없으면 헤더에 생성
- 언어별 스니펫으로 서식; 필수 snippet 삭제 시 기본 복원

미확인: 2630 정확한 기본 본문/템플릿/선택 유지; 파일 배치·정적 멤버 header-only 판단.

### Add Forward Declaration

[공식 근거](https://www.wholetomato.com/documentation/code-generation/add-forward-declaration)

- 미확정 참조 symbol에서 forward declaration 제공
- 기존 forward 선언 또는 첫 include 그룹 뒤 등 적절한 위치; 커서 유지
- 현재 symbol 참조를 근거로 하며 다른 참조를 평가해 include 충분성을 결정하지 않음

미확인: 2630 namespace/template/enum·insertion heuristic; 이미 선언된 symbol과 가용성.

### Add include Directive

[공식 근거](https://www.wholetomato.com/documentation/code-generation/add-include-directive)

- 미확정 symbol의 파싱된 헤더 후보에서 include 생성
- 마지막 include 뒤의 ifdef 밖에 추가하고 커서 유지
- 정의 헤더 우선이며 Boost/Qt 일부는 umbrella 예외
- 기본 가장 짧은 경로; 상대/프로젝트 경로·quote/angle·filename-only 설정
- 기존 include delimiter를 따르며 없으면 IDE 설정 이용

미확인: 2630 후보 순위·umbrella 예외 목록; generated Unreal header 정렬 및 조건부/다중 include 구분; 외부 SDK·compile configuration별 가용성.

### Find by Context

[공식 근거](https://www.wholetomato.com/documentation/navigation/find-by-context)

- 같은 이름이어도 다른 scope 객체는 제외하며 활성 파일의 다음/이전 참조로 이동
- 커서 또는 선택 심볼에서 실행; 전체 파일 간 검색은 Find References에 해당

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Find Symbol in Solution

[공식 근거](https://www.wholetomato.com/documentation/navigation/find-symbol-in-solution)

- 드롭다운은 대소문자 무시; 대화상자는 다중 AND·제외·점 경계 필터와 선택적 uppercase case matching
- 기본 선택은 prefix 우선, 없으면 첫 contains; 심볼 이름 알파벳 정렬은 열 클릭으로 변경되지 않음
- solution/system 범위 및 classes/struct/namespaces 제한; 선택 초기화 후 Ctrl+Z로 이전 필터 복원
- 지역 변수 제외; 선언 위치·언어별 포함 심볼 규칙 구분

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### GoTo Implementation

[공식 근거](https://www.wholetomato.com/documentation/navigation/goto-implementation)

- Alt+G: 목적지 하나면 직접 이동, 복수면 메뉴; 구현 항목은 {...} 표시
- 활성 project/solution 범위 설정과 system symbol의 임시 범위 확대
- 주석 scope가 없으면 동명 후보 메뉴; hashtag와 프로젝트 밖 파일 지원
- C/C++에서 VA 후보가 없고 IntelliSense가 켜져 있으면 IDE GoTo Definition 호출
- 구현 검색은 선언 디렉터리와 ../src·../source 및 구성 경로 사용

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### GoTo Related

[공식 근거](https://www.wholetomato.com/documentation/navigation/goto-related)

- Shift+Alt+G: 심볼 종류별 base/derived·override·생성자·type/typedef·선언/정의·header 목적지
- 상속 관계는 하위 메뉴; 가능한 파일/줄/namespace 표시
- include에서는 관련 명령 및 해당 header를 include하는 파일; 주석에서는 가장 가능성 높은 심볼 선택
- 오버로드 목적지 순서와 call signature matching은 옵션 영향

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### List Methods in File

[공식 근거](https://www.wholetomato.com/documentation/navigation/list-methods-in-file)

- Alt+M: 편집에 따라 갱신; 기본 파일 등장 순서 및 상단 navigation bar
- 다중 AND·negative·점 경계·comma OR 필터
- class/method/macro 확장 method/member/struct/enum/define/event/property/region 항목 옵션
- namespace·parameter 표시 및 정렬 옵션; ESC/외부 클릭 닫힘

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Move Scope

[공식 근거](https://www.wholetomato.com/documentation/navigation/move-scope)

- Alt+Down/Up: enclosing brace/parenthesis 바깥 scope의 시작/끝 이동
- global scope에 도달하면 메서드 간 이동

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Open File in Solution

[공식 근거](https://www.wholetomato.com/documentation/navigation/open-file-in-solution)

- Shift+Alt+O: 다중 선택과 Ctrl+A; 범위·IDE key scheme에 따라 기본 shortcut scope 차이
- AND·negative·점 경계·comma OR·path 필터·fuzzy 옵션; base filename prefix 선택 우선
- 숨겨도 적용되는 persistent filter와 Ctrl+R 해제
- solution·directory·private/shared system·external·open 파일 공급원 및 project 중복 결합 옵션
- line/column qualifier는 형식별 조건; 기본 base filename 정렬 및 열별 reverse 정렬
- ESC/Stop은 non-solution 로딩만 중단하고 solution 파일은 유지

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Enhanced Listboxes

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/enhanced-listboxes)

- member는 dot/arrow, completion은 Ctrl+Space; private member도 목록 표시
- C/C++ 기본 활성·C# 기본 비활성이라는 문서 조건; 언어별 원본 값 별도 검증 필요
- 비상속 bold/우선 배치·축소·acronym/shorthand·종류/가시성 toolbar 필터
- Tab/Enter 비활성은 목록을 닫고 입력 문자 삽입; Ctrl 투명화; 추가 commit 문자는 IDE/언어별 차이
- include 경로·snippet 및 주석/문자열 Ctrl+Space 문맥 없는 파일 텍스트 후보
- C/C++ 데이터 공급원 선택; :: 및 C#은 default IntelliSense; 우측 텍스트 overwrite는 별도 valid symbol 여부 영향

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Smart Select

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/smart-select)

- Extend/Shrink와 Block Extend/Shrink 네 명령; Shift+Alt+]/[ 및 Alt+]/[
- 초기 범위는 커서 위치·leading whitespace·statement/block에 따라 구분
- 상대적 작은 증가·항상 word 시작·case/underscore word 분할 옵션과 종속성
- 댓글/문자열에서도 일반 extend/shrink 가능; block 명령은 block 수준 유지
- 선택이 화면 높이를 넘으면 시작 위치를 알리는 임시 창

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Enhanced Syntax Coloring

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/enhanced-syntax-coloring)

- light/dark 기본 세트를 설치 시 IDE 설정으로 결정; 대비 부족 시 black/white fallback
- IDE coloring보다 우선; source/listbox/tool window 적용 선택 및 theme 인식 영향
- 추가 요소·배경색·reset 경로는 VS 버전별 차이
- 색은 심볼 사용 앞뒤 문자 영향을 받으며 정의가 항상 결정 요인은 아님
- C/C++ macro는 해당 header include 여부와 별개로 global 문맥으로 취급

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Highlight Matching Words

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/highlight-matching-words)

- 문자열/주석 안 active word에서 시작하고 파일 내 동일 단어에도 적용; scope 무시
- 대소문자 일치·전체 단어만; read reference highlight 색 공유

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Highlight References to Symbol Under Cursor

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/highlight-references-to-symbol-under-cursor)

- 같은 semantic context 참조만; read/write 구분
- 공백 이동 시 유지·ESC 임시 해제·project 외 파일 지원
- Find References 색 공유; 문서는 일반적으로 write MistyRose/read LightCyan으로 설명하며 실제 테마별 값 별도 검증
- IDE 유사 기능을 disable/re-enable할 기회 제공

미확인: 설치 2630의 실제 동작·옵션 초기값·UI·키 충돌 미확인; native 구현·취소·성능·언어별 경계 사례 미확인.

### Auto-Extend Multi-Line Comments

[공식 근거](validation/installed-editor-feature-spec.json)

- 줄바꿈 시 block comment 및 연속 single-line comment marker를 현재 줄에서 이어받음

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Format After Paste

[공식 근거](validation/installed-editor-feature-spec.json)

- 여러 줄 paste 이후 IDE Format Selection으로 서식 지정
- Undo로 formatting 철회 가능

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Insert () and Closing Brackets and Quotes

[공식 근거](validation/installed-editor-feature-spec.json)

- 여는 delimiter 뒤 닫는 delimiter·method commit 후 괄호 삽입
- Undo/Backspace 철회; 입력 중단 전 inserted delimiter 입력은 해당 문자 덮어쓰기
- 닫는 brace 위치는 same/next/blank-line 옵션

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Insert _ After m and Shift

[공식 근거](validation/installed-editor-feature-spec.json)

- m 입력 직후 Shift를 빠르게 눌렀다 놓으면 underscore 삽입

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Multiple Clipboards

[공식 근거](validation/installed-editor-feature-spec.json)

- Ctrl+Shift+V 최근 clipboard 메뉴
- 표시 개수는 별도 설정이며 해당 도움말은 수치를 명시하지 않음

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Repair Case

[공식 근거](validation/installed-editor-feature-spec.json)

- typing 중 symbol 대소문자 수정; 여러 교정 후보가 있으면 수정하지 않음
- C#은 suggestion/completion 목록 활성 필요

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Convert Dot to ->

[공식 근거](validation/installed-editor-feature-spec.json)

- 적절한 문맥에서 dot를 arrow로 바꿈; operator-> smart object 변형은 별도 옵션
- Delete/Undo/Backspace로 철회

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Surround Selection

[공식 근거](validation/installed-editor-feature-spec.json)

- slash는 완전한 줄/복수 줄이면 line comment, 부분 줄이면 block comment 토글
- asterisk는 block comment 토글; parentheses/braces surround
- surround character가 있는 snippet 입력은 선택을 expansion으로 대체

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Highlight Current Line

[공식 근거](validation/installed-editor-feature-spec.json)

- caret 줄 표시; VA Current Line IDE 색 설정 사용
- 배경·색상/frame·점선·실선·margin까지 ray 형식

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Highlight Files in Output Window

[공식 근거](validation/installed-editor-feature-spec.json)

- build output에서 solution 파일 경로를 다른 파일 경로와 구별해 강조

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Highlight Find Results

[공식 근거](validation/installed-editor-feature-spec.json)

- IDE find 후 editor matches 강조; ESC로 해제; IDE Find Result(VA) 배경색 사용

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Highlight Matching Braces and Parentheses

[공식 근거](validation/installed-editor-feature-spec.json)

- caret가 scope 안에 있을 때 matching delimiter 강조

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Highlight Mismatching Braces and Parentheses

[공식 근거](validation/installed-editor-feature-spec.json)

- caret 문맥에서 균형이 맞지 않는 delimiter 강조

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Underline Spelling Errors

[공식 근거](validation/installed-editor-feature-spec.json)

- 주석과 문자열 spelling 표시; 문맥 메뉴에서 교정 또는 사전 추가

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Underline Unknown Symbols

[공식 근거](validation/installed-editor-feature-spec.json)

- mistyped/context-invalid symbol 표시; IDE squiggle 표시가 비활성일 때만 적용

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### VA Memory View

[공식 근거](validation/installed-editor-feature-spec.json)

- debug session 시작 전에 integration 활성 필요; 완전한 분리는 IDE 재시작 필요

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### VA Step Filter

[공식 근거](validation/installed-editor-feature-spec.json)

- integration 활성 시 창 숨김 상태에서도 active filter 적용
- solution filter는 APPDATA 또는 .va/shared; global/builtin 상태는 APPDATA
- 공유 위치에 신규 solution filter 저장하려면 .vadbg 쓰기 권한 필요

미확인: Actual control/registry binding and factory default unverified; Original runtime, key routing, Undo grouping and version/language edges unverified.

### Simple Link Patterns

[공식 근거](validation/rea/source-links-analysis-summary.json)

- 설치 도움말: comment keyword는 세미콜론 분리·대소문자 무시, suffix는 단일 문자 집합
- value 양끝 공백 무시·복수단어는 quote·타입 불일치면 link 미생성
- 정적 line cache는 comment classification 범위를 검사하고 별도 plain-text 허용 gate가 있음

미확인: 실제2630 UI·factory설정·원본 실행 비교 미실행; 미해결 decompiler 타입·regex group 의미·plugin lifecycle 추가 확인 필요.

### Complex Link Patterns

[공식 근거](validation/rea/source-links-analysis-summary.json)

- HwPrefs.GetRegexPattern은 RE= 접두사를 대소문자 무시로 판정하고 뒤 pattern 반환
- CreateRegex는 IgnoreCase/Singleline 사용; runtime은 Compiled 추가
- timeout 생성자를 reflection으로 시도하고 실패하면 timeout 없는 생성자로 fallback
- marker match 시간이 threshold 이상이면 해당 regex/group 슬롯 비활성화; 기본 getter fallback100ms는 실제 전체 실행시간 상한 보장 아님

미확인: 실제2630 UI·factory설정·원본 실행 비교 미실행; 미해결 decompiler 타입·regex group 의미·plugin lifecycle 추가 확인 필요.

### Custom Plug-ins

[공식 근거](validation/rea/source-links-analysis-summary.json)

- REA metadata: IPluginFactory Name/CreatePlugin, IPlugin Flags/GetTooltip/GetUrl/Execute/ContextExecute/GetRegex/Configure 계약 존재
- ValidateType은 분리 AppDomain helper에서 검증하고 finally에 validation domain 참조를 해제
- 검색 경로는 known directories·AppDomain base/relative path·추가 plugin paths를 모음
- SourceLinksAST에는 automation/test helper가 포함되므로 grammar parser로 해석하지 않음

미확인: 실제2630 UI·factory설정·원본 실행 비교 미실행; 미해결 decompiler 타입·regex group 의미·plugin lifecycle 추가 확인 필요.

### Open Hyperlinks in an External Browser

[공식 근거](validation/rea/source-links-analysis-summary.json)

- 설치 도움말: plugin이 URL을 제공하면 behavior 설정으로 internal/external browser 결정
- plugin이 override한 preference는 read-only로 표시됨
- URL template은 Value 및 선택 Keyword placeholder를 사용; 실제 launch/escape 처리는 미확인

미확인: 실제2630 UI·factory설정·원본 실행 비교 미실행; 미해결 decompiler 타입·regex group 의미·plugin lifecycle 추가 확인 필요.

### GoTo Member

[공식 근거](https://www.wholetomato.com/documentation/navigation/goto-member)

- 현재 type/variable 또는 enclosing structure 공백에서 해당 member 탐색; enum/interface/namespace/struct/typedef 지원
- base member 포함 옵션·다중문자/negative/점prefix 필터·복사·Find References·type별 정렬

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Navigate Back and Forward

[공식 근거](https://www.wholetomato.com/documentation/navigation/navigate-back-and-forward)

- 단순 caret 이동 waypoint는 제외; Alt+Left/Right는 다른 명령 바인딩 영향을 받음
- Alt+Right의 Edit.CompleteWord 바인딩 제거 조건; waypoint 종류의 icon 표는 별도 확인 필요

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Open Corresponding File

[공식 근거](https://www.wholetomato.com/documentation/navigation/open-corresponding-file)

- Alt+O: 대응 하나면 직접 열고 복수면 선택 메뉴; C++ 및 C# design/codebehind 등 관계 지원
- 현재 directory 우선; 없으면 project·parent·inc/include 검색; type 미확정이면 유사prefix 후보

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### VA Hashtags

[공식 근거](https://www.wholetomato.com/documentation/navigation/va-hashtags)

- 주석 내 공백없는 tag; 대소문자 무시·다중tag·Goto exact/창 substring 검색
- 자동갱신·tag/file 그룹·AND/negative/OR필터·Shift+Alt+H/F8/Shift+F8
- 숨김은 tag/file/recursive directory/project 범위; tool window만 영향·Goto/완성 가능
- solution .vaHashtags 외부변경은 IDE재시작; read-only면 규칙 저장 불가

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### VA Navigation Bar

[공식 근거](https://www.wholetomato.com/documentation/navigation/va-navigation-bar)

- source창 상단/하단·표시 옵션; context/method list/scope arrows/definition/Go 구성
- Go 기본은 implementation이며 related로 변경 가능; namespace 표시 문맥메뉴
- 공식 현재 문서의 다중 project 전환은2630 가용성 별도 확인

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Rename

[공식 근거](https://www.wholetomato.com/documentation/refactoring/rename)

- 선언/정의/참조 및 선택적 comment/string·상속chain 변경; project/solution 범위
- 이전 Rename/ChangeSignature/FindReferences scope 공유; 동일줄 복수ref는 복수항목
- scope변경으로 결과 재검색하면 제외선택 소실; preview double-click은 원본파일focus 유지
- Stop/Ctrl+Break로 검색중단 가능; 불완전검색 commit의 실패 가능성 문서 경고

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Move Implementation to Source File

[공식 근거](https://www.wholetomato.com/documentation/refactoring/move-implementation-to-source-file)

- header에 declaration 남김; class/overload 인접배치·CreateImplementation snippet 사용
- 기본 인접배치 또는EOF 옵션; inline template는 먼저 class밖 header로 이동하고 다음 실행에 source로 이동
- 이동 후 VA history로 source/header 왕복

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Move Selection to New File

[공식 근거](https://www.wholetomato.com/documentation/refactoring/move-selection-to-new-file)

- 전체줄 선택만; destination은 미존재파일·header면 현재파일 include 옵션
- 언어·header/source별 생성snippet으로 새파일 내용 구성

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Rename Files

[공식 근거](https://www.wholetomato.com/documentation/refactoring/rename-files)

- 관련 C++ h/cpp 및 C# designer/resx 함께 rename; include/project/solution/source-control 반영
- 기존directory 유지; rename전 저장·rename후 변경내용은 자동저장 안함
- 단일Undo 불가; nonproject/solution없는 파일에서는 의도 확인

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Move Method Implementations to Source File

[공식 근거](https://www.wholetomato.com/documentation/refactoring/move-method-implementations-to-source-file)

- class이름에서 모든method 이동 command·선택dialog
- nested class 선택 시 두번째dialog; template/배치 규칙은 단일move와 공유

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Move Implementation to Header File

[공식 근거](https://www.wholetomato.com/documentation/refactoring/move-implementation-to-header-file-class-declaration)

- free function는 header, member는 class내부; 기존declaration 대체·없어도 생성
- declaration의 preprocessor/default값 보존·parameter이름은 implementation 사용
- 연관성이 충분한 선행주석 이동·이동구현 선택 유지·생성snippet 사용

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Simplify Instance Declaration

[공식 근거](https://www.wholetomato.com/documentation/refactoring/simplify-instance-declaration)

- local object declaration에서만 제공; 생성된 instance를 불필요하게 다시 설정하는 declaration 단순화

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Add Member

[공식 근거](https://www.wholetomato.com/documentation/code-generation/add-member)

- class reference에서 signaturedialog 후method/variable 추가; C++method는 inline header
- CreateImplementation snippet 사용·system directory class에는 제공안함·VS현대버전 한Undo

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Add Similar Member

[공식 근거](https://www.wholetomato.com/documentation/code-generation/add-similar-member)

- class아닌 memberreference에서 기존signature dialog·원member 근처에stub추가·body복사안함
- base member에서 실행하면 base class에 추가·systemdir 제외·현대VS 한Undo

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Document Method

[공식 근거](https://www.wholetomato.com/documentation/code-generation/document-method)

- 선언/정의에서 method 앞 commentblock·return/parameter 각각의 문서항목 생성
- DocumentMethod snippet과 refactoring전용 reserved값으로형식변경

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Create File

[공식 근거](https://www.wholetomato.com/documentation/code-generation/create-file)

- 현재파일 directory에 생성·현재project에추가·C++/C#/idl/rc/xaml관련파일에서제공
- 짝없는 h/cpp는 대응파일이 기본후보·cpp생성시同名headerinclude·header면 include옵션
- CreateFile snippet이 초기내용/caret 위치 결정

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Create Method Implementations

[공식 근거](https://www.wholetomato.com/documentation/code-generation/create-method-implementations)

- class declaration에서미구현method선택·구현된method는목록제외
- 대응source없으면 declaration아래생성·생성snippet·인접/EOF 옵션

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Add/Remove Braces

[공식 근거](https://www.wholetomato.com/documentation/code-generation/addremove-braces)

- 단일statement add/remove/toggle·nested if/else/for/while 문맥
- 복수statement compound는remove불가·indent만으로복수줄add불가
- source상단일macro가복수statement로확장하면 remove가동작변경가능하다는문서경고

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Add Using Directive

[공식 근거](https://www.wholetomato.com/documentation/code-generation/add-using-directive)

- C# common.NET type 사용시목록suggestion·C#listbox활성필요
- accept하면상단using삽입·caret위치유지

미확인: Current official documentation may differ from2630; installed availability and output need comparison; Native algorithm·factory options·UI·key routing·Undo/performance not fully verified.

### Acronyms

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/acronyms)

- 축소 목록 옵션이 필요하며 첫 글자와 대문자/underscore 경계로 후보를 좁힘
- project 및 최근 사용 symbol 중심; 파일/심볼 검색 dialog와 method list에서는 사용하지 않음

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Shorthand

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/shorthand)

- 축소 목록에서 여러 부분 문자열로 후보 검색; acronym 우선과 최근 사용 영향
- 목록 밖에서는 첫 입력 글자로 시작하는 symbol로 제한; Ctrl+Space로 전체 completion 전환

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Reset Editor Zoom

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/reset-editor-zoom)

- editor scope에서100%로 복원; Ctrl+0은 추천키 수락 조건
- VS2015+는 새 창의 global zoom도100%로 설정

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Smart Suggestions

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/smart-suggestions)

- 생성자·enum 대입/비교·인자 타입에 맞는 local 변수·주석 hashtag를 제안
- 타입 이름의 Smart Suggestions snippet으로 값을 확장; 인자 comma+space는 다음 제안 유도

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Suggestion Lists

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/suggestion-lists)

- 최근/주변 symbol과 code bits; C#에서는 IDE 목록에 병합하거나 VA 목록 사용
- symbol 정의 중에는 자동 선택하지 않고 Down으로 선택; parameter list 제안은 global scope 및 Tab/Enter commit
- 오른쪽이 알려진 symbol이면 유지하고 모르는 잔여 문자열은 제거

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Surround With

[공식 근거](https://www.wholetomato.com/documentation/coding-assistance/surround-with)

- 선택을 snippet 코드로 감싸며 메뉴와 지정 surround 문자로 접근
- 단순 Surround Selection과 별개이며 Outline과 editor에서 가용 메뉴가 다름

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Column Indicator

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/column-indicator)

- 지정 column의 뒤쪽 경계에 표시; 위치/색 옵션
- 가변폭 글꼴은 m 글자 폭의 배수로 위치 산정

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Context Field

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/context-field)

- symbol 문맥·scope 표시, unknown이면 비움; global scope는 현재 파일명
- field 폭 조절·scope 표시 축소·method dropdown·scope arrows·복사 메뉴

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Definition Field

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/definition-field)

- symbol 정의·scope 상세·include 실제 경로 표시; 복수정의 목록은 정보용으로 선택해도 이동 안함
- unknown은 비움; typedef/macro를 적당히 평가하며 aggressive 평가 옵션 별도

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Local Symbols in Bold

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/local-symbols-in-bold)

- enhanced coloring이 필요하며 현재 파일에 정의된 이름에 bold 적용
- semantic scope와 별개로 동명 occurrence 모두 bold 가능; inherited listbox bold와 다른 옵션

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Parameter Info

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/parameter-info)

- method 뒤 여는 괄호 또는 Ctrl+Shift+Space; 현재 인자 bold·위/아래로 overload 탐색
- C# 공급원은 default IntelliSense; disable 반영에는 IDE 재시작 조건

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Print in Color

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/print-in-color)

- VS2015+ print에 VA 색을 포함하며 IDE 버전별 지원이 다름
- 출력 selection만 따로 파싱하므로 잘린 문맥의 색이 원본과 다를 수 있음

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Quick Info

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/quick-info)

- IDE tooltip 보완/대체·source 주석·닫는 brace/구조 keyword 문맥·Doxygen 형식 옵션
- IDE 버전별 공급원과 중복주석 조건이 다름; mouse 이동/ESC로 해제

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### System Symbols in Italics

[공식 근거](https://www.wholetomato.com/documentation/code-understanding/system-symbols-in-italics)

- system include와 Program Files의 symbol 등은 italics 후보이며 project override는 구별
- system 선언 DB는 solutions 간 공유 읽기 전용이며 처음 포함한 solution에서 파싱

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Code Inspection Settings

[공식 근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection)

- 개별 검사 활성·quickfix 조정·level1~5 설정; level은 UI 색이며5는 dim 처리
- editor에서 개별 검사를 enable/disable할 수 없고 tool window에서는 disable만 가능

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Code Issues in the Editor

[공식 근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection)

- solid 또는 dim은 fix 가능, dotted는 불가; underline과 scrollbar 위치 표시 별도
- scrollbar의 중복 issue 세부정보는 중복 제거; 표시 비활성이어도 결과 창에서 접근

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Quick Fixes in the Editor

[공식 근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection)

- hover tomato 또는 issue 앞부분 caret에서 Shift+Alt+Q로 fix 접근
- 그룹 fix는 editor Undo 한 번으로 복원; 파서는 다른 VA 파서와 독립

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Quick Fixes in a Tool Window

[공식 근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection)

- 현재 파일 issue 기본 line 정렬; 단일/복수 선택과 checker/level 그룹 fix
- tracking·열 정렬·double click 이동; fix Undo는 결과 창이 아니라 editor에서 수행

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### VA Snippets

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/introduction-to-va-snippets)

- shortcut·title-only·surround·smart suggestion·refactoring 유형을 공유할 수 있음
- 편집 즉시 반영·IDE 간 사용자 snippet 공유·reserved 값과 caret 위치 확장

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### for Refactoring

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-for-refactoring-and-code-generation)

- 지정 title로 필수 snippet을 찾고 삭제 시 기본 snippet 재생성
- refactoring 전용 reserved 값이 있으며 일반 symbol-context reserved는 null 확장

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### for Smart Suggestions

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-for-smart-suggestions)

- class/loop/switch 및 타입 이름 snippet이 제안 값을 제공
- 목록은 알파벳 정렬이므로 template 내 값 순서는 영향 없음

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### for Surround With

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-for-surround-with)

- selected reserved 값은 현재 선택 또는 빈 문자열; 같은 surround 문자가 복수면 메뉴
- surround 문자 실행은 관련 옵션 필요; title accelerator 및 메뉴/shortcut 겸용

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### with Shortcuts

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-with-shortcuts)

- shortcut은 case-sensitive; 중복 shortcut은 모두 표시하지만 동일 shortcut-title 쌍은 하나만 표시
- 수락하면 shortcut을 expanded 코드로 교체; 제목 없으면 코드 앞부분 표시

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Create from Selection

[공식 근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-create-from-selection)

- editor 선택으로 snippet editor를 열고 title/shortcut/description 지정
- input placeholder와 end reserved를 편집하여 입력 dialog와 최종 caret 구성

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Address Resolver

[공식 근거](https://www.wholetomato.com/documentation/debug-assistance/address-resolver)

- EXE/DLL/PDB 여러 모듈의 주소를 symbol로 해석; 올바른 실행 base 필요
- PDB 없이 exports 모드·절대/relative 주소 형식; 미해결 주소는???·비주소 텍스트는 유지
- symbol server는 VS 설정 사용; 주소에서 symbol 단방향 해석

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### PDB Explorer

[공식 근거](https://www.wholetomato.com/documentation/debug-assistance/pdb-explorer)

- 한 모듈의 import/export 및 release PDB symbol 탐색; running process 없이도 가능
- debugger 이동에는 base 필요·종류/unit/zero/duplicate address 필터·열 정렬

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### VA Outline

[공식 근거](https://www.wholetomato.com/documentation/tool-window/va-outline)

- 현재 문서와 양방향 동기화·현재 scope bold·SAL 표시 제거
- 다중 선택·refactor 가용성·source 편집/drag/drop·comment/region 노드·hover
- AutoUpdate 꺼지면 focus 시 갱신; 자동expand는 기존노드를 자동collapse하지 않음

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### VA View

[공식 근거](https://www.wholetomato.com/documentation/tool-window/va-view)

- 파일/심볼 dropdown·MRU·hover class browser와 include hierarchy 구성
- MRU는 solution별 저장·재오픈 복원·여러 IDE는 마지막 종료가 저장
- HCB는 hover symbol 기준이며 공백/주석/unknown에서 갱신하지 않음

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Connect to FogBugz and GitHub

[공식 근거](https://www.wholetomato.com/documentation/source-links/built-in-plug-ins)

- issue/case tooltip은 plugin 설정과 read 권한 필요; 열기는 별도 browser login 필요할 수 있음
- 외부 서비스 API/인증의 현재 상태와2630 호환성은 미확인

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Preview Files

[공식 근거](https://www.wholetomato.com/documentation/source-links/built-in-plug-ins)

- File Viewer는 comment 경로의 text/HTML/XAML/RTF/image/video 등을 preview
- relative 경로는 활성문서 기준·공백 quote/%20·환경값·HTML anchor 지원

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Context Menus

[공식 근거](https://www.wholetomato.com/documentation/configuration/context-menus)

- IDE context 메뉴를 확장할 때 VA submenu 구분
- 별도 VA 메뉴는 지정 modified mouse로 호출 가능

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Custom File Extensions

[공식 근거](https://www.wholetomato.com/documentation/configuration/custom-file-extensions)

- 무시 확장자는 세미콜론 구분·이미 열린 파일은 다시 열어 반영
- extensionless C++ header는 VA 옵션과 IDE 언어 mapping 필요; custom extension 절차는 IDE별 차이

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Keyboard Shortcuts

[공식 근거](https://www.wholetomato.com/documentation/configuration/keyboard-shortcuts)

- 기본키가 비어 있을 때만 설치 배정하며 기존 배정을 덮어쓰지 않음
- 추천키로 scope 확대·충돌키 재배정; global/editor scope 구분·현재키 조회

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Mouse

[공식 근거](https://www.wholetomato.com/documentation/configuration/mouse)

- modified click은 GoTo/Related/VA 메뉴에 연결하며 editor scope
- modified wheel은 no-op 또는 selection/block 확장·축소; Ctrl+Alt+wheel zoom은 유지

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Multiple Platforms

[공식 근거](https://www.wholetomato.com/documentation/configuration/project-directories)

- 편집 platform은 build platform 변경만으로 전환되지 않으며 C++ directories에서 선택 후 재시작
- platform별 header 검색 경로와 정의를 구별해야 함

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Non-Microsoft Build Processes

[공식 근거](https://www.wholetomato.com/documentation/configuration/project-directories)

- IDE 프로젝트 설정이 비어 있는 외부 build 환경은 Custom directories로 보완
- 검색 순서 유지·한 custom set 보존·변경 인식 Rebuild 조건; 모든 build system 자동 지원은 미확인

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

### Parse Concurrency

[공식 근거](https://www.wholetomato.com/documentation/configuration/parse-concurrency)

- 현재 공식 문서 기본은 모든 CPU/core; single 또는 maximum threads 제한 가능
- thread 제한 변경은 IDE 재시작; 검사 전용 max1/queue4와 일반 파서 설정은 별개

미확인: 현재 공식 문서와 설치2630 차이·factory값·native 흐름·UI/키/Undo/성능 전체 대조 미완료.

## 108개 항목 분석 장부

| ID | 기능 | 현재 구현 | 분석 상태 |
|---|---|---|---|
| VA-F001 | Find by Context | unverified | documented-partially |
| VA-F002 | Find References | partial | documented-partially |
| VA-F003 | Find Symbol in Solution | partial | documented-partially |
| VA-F004 | GoTo Implementation | partial | documented-partially |
| VA-F005 | GoTo Member | unverified | documented-partially |
| VA-F006 | GoTo Related | partial | documented-partially |
| VA-F007 | Highlight Current Line | unverified | documented-partially |
| VA-F008 | List Methods in File | partial | documented-partially |
| VA-F009 | Move Scope | partial | documented-partially |
| VA-F010 | Navigate Back and Forward | partial | documented-partially |
| VA-F011 | Open Corresponding File | partial | documented-partially |
| VA-F012 | Open File in Solution | partial | documented-partially |
| VA-F013 | VA Hashtags | partial | documented-partially |
| VA-F014 | VA Navigation Bar | unverified | documented-partially |
| VA-F015 | Access to Refactoring Commands | partial | documented-partially |
| VA-F016 | Change Signature | unverified | documented-partially |
| VA-F017 | Convert Between Pointer and Instance | unverified | documented-partially |
| VA-F018 | Encapsulate Field | unverified | documented-partially |
| VA-F019 | Extract Method | unverified | documented-partially |
| VA-F020 | Introduce Variable | unverified | documented-partially |
| VA-F021 | Move Implementation to Header File | unverified | documented-partially |
| VA-F022 | Move Implementation to Source File | unverified | documented-partially |
| VA-F023 | Move Method Implementations to Source File | unverified | documented-partially |
| VA-F024 | Move Selection to New File | unverified | documented-partially |
| VA-F025 | Rename | partial | documented-partially |
| VA-F026 | Rename Files | unverified | documented-partially |
| VA-F027 | Simplify Instance Declaration | unverified | documented-partially |
| VA-F028 | Add Forward Declaration | unverified | documented-partially |
| VA-F029 | Add Member | unverified | documented-partially |
| VA-F030 | Add Missing Case Statements | unverified | documented-partially |
| VA-F031 | Add Similar Member | unverified | documented-partially |
| VA-F032 | Add Using Directive | unverified | documented-partially |
| VA-F033 | Add include Directive | unverified | documented-partially |
| VA-F034 | Add/Remove Braces | unverified | documented-partially |
| VA-F035 | Create Declaration | unverified | documented-partially |
| VA-F036 | Create File | unverified | documented-partially |
| VA-F037 | Create from Usage | partial | documented-partially |
| VA-F038 | Create Implementation | unverified | documented-partially |
| VA-F039 | Create Method Implementations | unverified | documented-partially |
| VA-F040 | Document Method | unverified | documented-partially |
| VA-F041 | Implement Interface / Virtual Methods | unverified | documented-partially |
| VA-F042 | Acronyms | unverified | documented-partially |
| VA-F043 | Auto-Extend Multi-Line Comments | unverified | documented-partially |
| VA-F044 | Enhanced Listboxes | unverified | documented-partially |
| VA-F045 | Format After Paste | unverified | documented-partially |
| VA-F046 | Insert () and Closing Brackets and Quotes | unverified | documented-partially |
| VA-F047 | Insert _ After m and Shift | unverified | documented-partially |
| VA-F048 | Multiple Clipboards | partial | documented-partially |
| VA-F049 | Reset Editor Zoom | partial | documented-partially |
| VA-F050 | Shorthand | unverified | documented-partially |
| VA-F051 | Smart Select | partial | documented-partially |
| VA-F052 | Smart Suggestions | unverified | documented-partially |
| VA-F053 | Sort Selected Lines | partial | documented-partially |
| VA-F054 | Suggestion Lists | unverified | documented-partially |
| VA-F055 | Surround Selection | unverified | documented-partially |
| VA-F056 | Surround With | unverified | documented-partially |
| VA-F057 | Column Indicator | unverified | documented-partially |
| VA-F058 | Context Field | unverified | documented-partially |
| VA-F059 | Definition Field | unverified | documented-partially |
| VA-F060 | Enhanced Syntax Coloring | partial | documented-partially |
| VA-F061 | Local Symbols in Bold | unverified | documented-partially |
| VA-F062 | Highlight Find Results | unverified | documented-partially |
| VA-F063 | Highlight Matching Words | partial | documented-partially |
| VA-F064 | Highlight References to Symbol Under Cursor | unverified | documented-partially |
| VA-F065 | Parameter Info | unverified | documented-partially |
| VA-F066 | Print in Color | unverified | documented-partially |
| VA-F067 | Quick Info | unverified | documented-partially |
| VA-F068 | System Symbols in Italics | unverified | documented-partially |
| VA-F069 | Convert Dot to -> | unverified | documented-partially |
| VA-F070 | Highlight Files in Output Window | unverified | documented-partially |
| VA-F071 | Highlight Matching Braces and Parentheses | unverified | documented-partially |
| VA-F072 | Highlight Mismatching Braces and Parentheses | unverified | documented-partially |
| VA-F073 | Repair Case | unverified | documented-partially |
| VA-F074 | Underline Spelling Errors | unverified | documented-partially |
| VA-F075 | Underline Unknown Symbols | unverified | documented-partially |
| VA-F076 | Code Inspection Settings | unverified | documented-partially |
| VA-F077 | Code Issues in the Editor | unverified | documented-partially |
| VA-F078 | Quick Fixes in the Editor | unverified | documented-partially |
| VA-F079 | Quick Fixes in a Tool Window | unverified | documented-partially |
| VA-F080 | VA Snippets | partial | documented-partially |
| VA-F081 | for Refactoring | unverified | documented-partially |
| VA-F082 | for Smart Suggestions | unverified | documented-partially |
| VA-F083 | for Surround With | unverified | documented-partially |
| VA-F084 | with Shortcuts | unverified | documented-partially |
| VA-F085 | Create from Selection | unverified | documented-partially |
| VA-F086 | Environment Placeholders | partial | documented-partially |
| VA-F087 | Input Placeholders | partial | documented-partially |
| VA-F088 | Reserved Strings | partial | documented-partially |
| VA-F089 | Address Resolver | unverified | documented-partially |
| VA-F090 | PDB Explorer | unverified | documented-partially |
| VA-F091 | VA Memory View | unverified | documented-partially |
| VA-F092 | VA Step Filter | unverified | documented-partially |
| VA-F093 | VA Outline | partial | documented-partially |
| VA-F094 | VA View | partial | documented-partially |
| VA-F095 | Simple Link Patterns | unverified | documented-partially |
| VA-F096 | Complex Link Patterns | unverified | documented-partially |
| VA-F097 | Connect to FogBugz and GitHub | unverified | documented-partially |
| VA-F098 | Preview Files | unverified | documented-partially |
| VA-F099 | Open Hyperlinks in an External Browser | unverified | documented-partially |
| VA-F100 | Custom Plug-ins | unverified | documented-partially |
| VA-F101 | Context Menus | unverified | documented-partially |
| VA-F102 | Custom File Extensions | unverified | documented-partially |
| VA-F103 | Keyboard Shortcuts | partial | documented-partially |
| VA-F104 | Mouse | unverified | documented-partially |
| VA-F105 | Multiple Platforms | unverified | documented-partially |
| VA-F106 | Non-Microsoft Build Processes | unverified | documented-partially |
| VA-F107 | Parse Concurrency | unverified | documented-partially |
| VA-F108 | Options Dialog | unverified | documented-partially |

## 현재 상태

동기화 검증 **0/108 = 0%**, 일부 대응 구현 **26/108 = 24.1%**. 전체 분석을 완료한 항목도 아직 0개입니다. 문서 일부 확인을 완료로 승격하지 않습니다. 제품 코드는 이 단계에서 추가하지 않았습니다.

미완료: 전체 네이티브/runtime 근거, 개별 옵션 기본값, 통합 설계 확정, 배치 구현, 실제 UnrealTestProject 성능 및 VA 원본 비교.

## 컴파일 옵션 캐시의 메서드 수준 분석

[REA 재구성 요약](validation/rea/compilation-options-reconstruction-summary.json) · [세부 분석 장부](validation/compiler-configuration-findings.json)

- 빈 configurationName·비 VCProject·비 VCProjectEngine 입력은 null 반환
- 키는 프로젝트 파일·configurationName·C/C++ 모드의 결합; cache 사용 여부는 OptionsCloneable 호출 결과에도 의존
- 캐시 generation을 읽고 프로젝트 옵션 계산 후 generation이 같을 때만 게시
- 캐시 값을 CopyOptions한 뒤 WalkVCFile로 파일 옵션 반영
- 프로젝트·파일 옵션 수집 예외는 로그와 null 반환
- include 경로 정규화 후 active configuration 기록 및 프로젝트 속성 변경 이벤트 연결

원본을 실행하지 않은 ILSpy 재구성입니다. REA로 원본 token·signature 및 IL 해시를 대조했으며 native 심볼/참조 인덱스의 구현을 입증하는 자료는 아닙니다.

미확인: OptionsCloneable 판정 세부 조건 및 옵션 전체 초기값; Native 심볼/참조 인덱스와의 관계·성능; VS 실제 호출 스레드와 모든 이벤트 경로; 다른 언어/빌드 플랫폼의 동일성.

현재 문서 목차에서 발견한 [추가 후보 장부](validation/extra-feature-candidates.json)는 설치 2630 가용성과 기존 항목 중복을 확인하기 전까지 기준 분모에 합치지 않습니다.

## 검사 요청·취소·수정 적용 통합 흐름

[메서드별 분석 장부](validation/inspection-pipeline-findings.json) · [REA 재구성 요약](validation/rea/inspection-pipeline-reconstruction-summary.json)

검사·IPC·결과·수정·버퍼 수명 215개와 ParserStats 2개 메서드를 토큰 단위로 재구성하고 REA로 원본 signature 및 IL 해시를 대조했습니다. 원본 코드는 실행하지 않았습니다. 215개 결과에 미해결 타입 주석 30개가 남아 있으므로 전체 의미 복원으로 보고하지 않습니다.

| 구간 | 확인한 흐름 |
|---|---|
| BufferListener.QueueParseTask | UI thread entry captures snapshot and parser/path; prior cancellation cookie cancelled; pending queue pruned before adding new entry; worker waits for parallel slot and same-path exclusion; cancellation checked before and after parse; PW results fetch may be attempted; live parser fallback; finally releases active filename slot and signals event; queued entry removed |
| BufferListener.OnTextBufferChanged / OnTimerTick | edited trigger spans invalidated and line numbers updated; timer callback only queues parsing for enabled active listener; missing parser rebuilt when inspections enabled |
| FileParser_client.Parse | per-file crash limit checked; cancellation propagated; server crash increments per-file counter and marks client broken |
| QuickFixNavigator.ApplyFixes | UI-thread requirement; overlapping replacements rejected per file before application; fix groups sorted; spans recreated where needed; file opening can temporarily stop global parsing; finally resets stop flag and reactivates listener |
| ParserStats | {'max_in_parallel': 1, 'max_queued': 4, 'recheck_period_ms': 250, 'timer_interval_ms': 500, 'timer_penalty_queue_full': 1.0, 'timer_penalty_max_parses': 1.0} |

이 동시성 상한 1개·대기 큐 4개·재확인 250ms·초기 타이머 500ms는 검사 ParserStats의 근거입니다. native 전체 심볼/참조 인덱서와 현재 옵션/실제 타이머를 대신하지 않습니다.

미확인: Native parser server internals; General indexer/Find References bake cache; Inspection Undo rollback for every action; Unresolved decompiler type annotations; Timing multipliers/options at actual runtime; Thread switching and all cross-buffer event interactions.

## 설치본 옵션·단축키 도움말 전체 장부

2630 설치 디렉터리에서 비어 있지 않은 HTML 도움말 278개를 수집했습니다. 옵션 250개·단축키 28개이며 각 파일의 SHA-256·범주·문서 링크를 [설치 도움말 장부](validation/va-installed-help-catalog.json)에 보존했습니다. [11개 기능의 이름 일치 후보](validation/installed-help-feature-links.json)는 실제 command/registry 바인딩 검증을 대신하지 않습니다.

[일반 파서·검사 파서 구분 근거](validation/general-parser-help-findings.json): 설치 도움말은 검사 파싱을 다른 VA 기능 파싱과 독립적으로 설명합니다. 일반 파싱에는 멀티스레드·최대 스레드 수·우선순위 설정이 있고, 우선순위 도움말은 Normal을 기본값으로 명시합니다. 일반 파서의 기본 thread 수는 미확인입니다. 도움말에 나타난 재시작 요구도 실제 적용 검증은 남아 있습니다.

[REA Ghidra 프로젝트 재사용 검토](validation/rea/native-project-reuse-review.json): 설치된 6.5.0 기본 launcher는 새 rea-project와 -import 경로를 사용합니다. 외부 기존 .gpr 재사용 기능의 전체 지원 여부는 아직 검증하지 않았습니다. 게임 중 native provider를 시작해 재분석하지 않았습니다.

도움말 파일명·존재·문구를 실제 옵션 키/가용성/원본 실행 완료로 승격하지 않습니다. 제품 코드를 추가하거나 동기화율을 올리지 않았습니다.

## 리팩터링·코드 생성 비교 시나리오

[12개 기능의 조건·제한·비교 시나리오](validation/refactoring-generation-spec.json)를 공식 문서에서 확인했습니다. 기존 문서 일부 확인 7개와 합쳐 리팩터링 외 항목을 포함한 총수는 상단 기능 표와 기계 판독 명세에 기록합니다. 전체 분석 완료 수는 여전히 0이며 실제 설치된 2630의 가용성/출력/Undo/초기값과 대조해야 합니다.

### Change Signature 비교


- 기본 인수 추가와 필수 인수 추가의 호출부 차이 — 실행 대기
- 여러 인수 이름 변경+재정렬 모호성 — 실행 대기
- 파생 클래스·외부 헤더·선택 호출부·Undo — 실행 대기
### Extract Method 비교


- read/write 혼합·멤버·로컬·단일 반환값 — 실행 대기
- 단일문/다중문·early return·매크로 선택 경계 — 실행 대기
### Create from Usage 비교


- new Foo와 new Foo()·상수 생성자 인수 — 실행 대기
- 클래스 멤버/지역/전역/인수/enum 선택 — 실행 대기
- 추론 실패 UnknownType 및 snippet customizations — 실행 대기
### Implement Interface / Virtual Methods 비교


- 다중/diamond 상속·이미 구현한 메서드 — 실행 대기
- 필터 반복 후 체크 유지·정렬 전환 — 실행 대기
- 추상/구체 혼합과 virtual/override 설정 — 실행 대기
### Encapsulate Field 비교


- m_nItemsCount·hWindow·밑줄 이름 — 실행 대기
- getter/setter 한쪽 생략·복합 참조 — 실행 대기
- 배열/char pointer 제한과 선택 참조 범위 — 실행 대기
### Introduce Variable 비교


- 동일 표현식의 선택 전/후와 다른 scope — 실행 대기
- 단일문 while/if·부작용 표현식 — 실행 대기
- 반환값 capture와 residual parentheses — 실행 대기
### Add Missing Case Statements 비교


- 일부/all enum 누락·default 존재 — 실행 대기
- 같은 값 alias·scoped enum·중첩 switch — 실행 대기
### Convert Between Pointer and Instance 비교


- new/delete와 address/dereference 혼합 — 실행 대기
- raw/unique/shared/custom 및 explicit type — 실행 대기
- 지원 불가 변수·escape하는 참조 — 실행 대기
### Create Declaration 비교


- 헤더 존재/없음·이웃 순서 — 실행 대기
- 기본 인수 주석·static/const/template — 실행 대기
### Create Implementation 비교


- source 존재/없음·EOF/이웃 배치 — 실행 대기
- default 인수 옵션·정적 멤버·snippet 삭제 — 실행 대기
### Add Forward Declaration 비교


- pointer와 by-value 다른 사용 혼재 — 실행 대기
- namespace/template·include group·커서 유지 — 실행 대기
### Add include Directive 비교


- ifdef 속 include·중복·umbrella·generated header — 실행 대기
- 여러 include roots와 경로 스타일 옵션 — 실행 대기
- 설정별 후보 부재·커서 유지 — 실행 대기

CodeAssist 제품 코드를 새로 구현하지 않았으며 VS Quick Actions 연결을 해당 기능의 구현 완료로 올리지 않았습니다.

## 탐색·편집·시각 기능 통합 계약

[12개 항목의 조건·제약·비교 시나리오](validation/navigation-edit-visual-spec.json)는 현재 공식 문서 근거입니다. 설치 2630의 알고리즘·초기값·UI·실행 동등성은 미확인입니다.

### Find by Context

- 같은 이름이어도 다른 scope 객체는 제외하며 활성 파일의 다음/이전 참조로 이동
- 커서 또는 선택 심볼에서 실행; 전체 파일 간 검색은 Find References에 해당

실행 대기: 동명 지역/멤버/중첩 scope 비교; 첫/마지막 참조의 경계 및 선택 방향 비교.

### Find Symbol in Solution

- 드롭다운은 대소문자 무시; 대화상자는 다중 AND·제외·점 경계 필터와 선택적 uppercase case matching
- 기본 선택은 prefix 우선, 없으면 첫 contains; 심볼 이름 알파벳 정렬은 열 클릭으로 변경되지 않음
- solution/system 범위 및 classes/struct/namespaces 제한; 선택 초기화 후 Ctrl+Z로 이전 필터 복원
- 지역 변수 제외; 선언 위치·언어별 포함 심볼 규칙 구분

실행 대기: prefix/contains·대소문자·부정/경계 조합 비교; 선택 초기화·이전 필터 복원 및 system 범위 비교.

### GoTo Implementation

- Alt+G: 목적지 하나면 직접 이동, 복수면 메뉴; 구현 항목은 {...} 표시
- 활성 project/solution 범위 설정과 system symbol의 임시 범위 확대
- 주석 scope가 없으면 동명 후보 메뉴; hashtag와 프로젝트 밖 파일 지원
- C/C++에서 VA 후보가 없고 IntelliSense가 켜져 있으면 IDE GoTo Definition 호출
- 구현 검색은 선언 디렉터리와 ../src·../source 및 구성 경로 사용

실행 대기: 단일/복수/overload 및 주석 scope 비교; project 제한·system 예외·후보 없음 fallback 비교.

### GoTo Related

- Shift+Alt+G: 심볼 종류별 base/derived·override·생성자·type/typedef·선언/정의·header 목적지
- 상속 관계는 하위 메뉴; 가능한 파일/줄/namespace 표시
- include에서는 관련 명령 및 해당 header를 include하는 파일; 주석에서는 가장 가능성 높은 심볼 선택
- 오버로드 목적지 순서와 call signature matching은 옵션 영향

실행 대기: 다이아몬드 상속·override·overload 메뉴 비교; include 역방향 의존성·주석 후보 비교.

### List Methods in File

- Alt+M: 편집에 따라 갱신; 기본 파일 등장 순서 및 상단 navigation bar
- 다중 AND·negative·점 경계·comma OR 필터
- class/method/macro 확장 method/member/struct/enum/define/event/property/region 항목 옵션
- namespace·parameter 표시 및 정렬 옵션; ESC/외부 클릭 닫힘

실행 대기: macro 메서드·namespace·parameter 표시 조합 비교; 편집 revision 갱신 및 ESC/포커스 종료 비교.

### Move Scope

- Alt+Down/Up: enclosing brace/parenthesis 바깥 scope의 시작/끝 이동
- global scope에 도달하면 메서드 간 이동

실행 대기: 괄호와 brace 혼합 중첩 및 global 이동 비교; 문자열/주석 내 delimiter와 불완전 코드 비교.

### Open File in Solution

- Shift+Alt+O: 다중 선택과 Ctrl+A; 범위·IDE key scheme에 따라 기본 shortcut scope 차이
- AND·negative·점 경계·comma OR·path 필터·fuzzy 옵션; base filename prefix 선택 우선
- 숨겨도 적용되는 persistent filter와 Ctrl+R 해제
- solution·directory·private/shared system·external·open 파일 공급원 및 project 중복 결합 옵션
- line/column qualifier는 형식별 조건; 기본 base filename 정렬 및 열별 reverse 정렬
- ESC/Stop은 non-solution 로딩만 중단하고 solution 파일은 유지

실행 대기: 다중 project 중복/결합·공급원 변경 및 취소 비교; persistent filter 숨김/해제·line/column 조건 비교; fuzzy·case·prefix·다중 선택 비교.

### Enhanced Listboxes

- member는 dot/arrow, completion은 Ctrl+Space; private member도 목록 표시
- C/C++ 기본 활성·C# 기본 비활성이라는 문서 조건; 언어별 원본 값 별도 검증 필요
- 비상속 bold/우선 배치·축소·acronym/shorthand·종류/가시성 toolbar 필터
- Tab/Enter 비활성은 목록을 닫고 입력 문자 삽입; Ctrl 투명화; 추가 commit 문자는 IDE/언어별 차이
- include 경로·snippet 및 주석/문자열 Ctrl+Space 문맥 없는 파일 텍스트 후보
- C/C++ 데이터 공급원 선택; :: 및 C#은 default IntelliSense; 우측 텍스트 overwrite는 별도 valid symbol 여부 영향

실행 대기: member/completion/suggestion 전환 및 필터 조합 비교; commit 설정·우측 텍스트 overwrite·취소 비교; C++/C#·::·include·주석 데이터 공급원 비교.

### Smart Select

- Extend/Shrink와 Block Extend/Shrink 네 명령; Shift+Alt+]/[ 및 Alt+]/[
- 초기 범위는 커서 위치·leading whitespace·statement/block에 따라 구분
- 상대적 작은 증가·항상 word 시작·case/underscore word 분할 옵션과 종속성
- 댓글/문자열에서도 일반 extend/shrink 가능; block 명령은 block 수준 유지
- 선택이 화면 높이를 넘으면 시작 위치를 알리는 임시 창

실행 대기: 네 명령의 교차 사용 및 외부 선택 후 shrink 비교; leading whitespace·문자열/주석·case/underscore 옵션 비교.

### Enhanced Syntax Coloring

- light/dark 기본 세트를 설치 시 IDE 설정으로 결정; 대비 부족 시 black/white fallback
- IDE coloring보다 우선; source/listbox/tool window 적용 선택 및 theme 인식 영향
- 추가 요소·배경색·reset 경로는 VS 버전별 차이
- 색은 심볼 사용 앞뒤 문자 영향을 받으며 정의가 항상 결정 요인은 아님
- C/C++ macro는 해당 header include 여부와 별개로 global 문맥으로 취급

실행 대기: light/dark·unknown theme·contrast fallback 비교; IDE reset·추가 요소·창별 적용 비교; 동명 다른 종류 심볼 및 include 없는 macro 색 비교.

### Highlight Matching Words

- 문자열/주석 안 active word에서 시작하고 파일 내 동일 단어에도 적용; scope 무시
- 대소문자 일치·전체 단어만; read reference highlight 색 공유

실행 대기: 주석/문자열에서 동명 다른 scope·대소문자·부분 단어 비교; code로 이동 및 선택·ESC 수명 비교.

### Highlight References to Symbol Under Cursor

- 같은 semantic context 참조만; read/write 구분
- 공백 이동 시 유지·ESC 임시 해제·project 외 파일 지원
- Find References 색 공유; 문서는 일반적으로 write MistyRose/read LightCyan으로 설명하며 실제 테마별 값 별도 검증
- IDE 유사 기능을 disable/re-enable할 기회 제공

실행 대기: 동명 shadowing·read/write·공백·ESC 비교; IDE 중복 표시·테마 및 프로젝트 외 파일 비교.

전체 분석 완료 0개. 제품 코드는 추가하지 않았습니다.

## 설치 도움말에 명시된 기본값

[16개 기본값 장부](validation/installed-help-defaults.json)에 원본 파일 SHA-256과 적용 범위를 기록했습니다. 도움말에 명시된 값이며 실제 설정 키와 초기 프로필 동작은 미확인입니다.

| 도움말 자원 | 문서 기본값 | 적용 범위 |
|---|---|---|
| OptionsHelp/Goto/CheckGotoProjectFilter.html | Project | GoTo destination scope; system symbols may temporarily expand scope |
| OptionsHelp/Goto/SelectSymbolOnGoto.html | True | Select symbol after GoTo |
| OptionsHelp/Hashtags/MinLenOfHashtags.html | 3 | Minimum hashtag length excluding leading hash |
| OptionsHelp/ListBoxes/OverwriteTextListbox.html | replace-unless-right-text-is-known-symbol | C/C++ enhanced listbox commit; never/always alternatives |
| OptionsHelp/PerfGeneral/CheckParseAllFilesOnOpen.html | True | Project opening parse including forced includes; startup cost relevant |
| OptionsHelp/PerfGeneral/CheckWatchFilesExternally.html | False | Reparse headers modified externally during IDE session |
| OptionsHelp/PerfGeneral/ComboThreadPriority.html | Normal | General parser threads; requires IDE restart |
| OptionsHelp/PerfGeneral/RespectVsCodeExcludedFiles.html | False | Initial parse honors .vscode/settings.json files.exclude only when enabled |
| OptionsHelp/PerfGeneral/UserDataDir.html | LOCALAPPDATA/VS-specific-extension/Data | Symbol database location example is VS2012+; directory change requires restart |
| OptionsHelp/ProjectsAndFiles/CheckEnumerateVslangRefs.html | True | Full project-reference enumeration; managed/native solution performance distinction |
| OptionsHelp/ProjectsAndFiles/CheckParseAllFilesEmptySolution.html | True | Parse sibling files when no solution or empty solution |
| OptionsHelp/ProjectsAndFiles/CheckParseAllFilesWithSolution.html | False | Parse sibling files with a loaded solution |
| OptionsHelp/ProjectsAndFiles/CheckParseFilesWithoutExtensions.html | False | Extensionless project files treated as C/C++ headers when enabled |
| OptionsHelp/UnrealEngine/CheckAllowSnippetsInUnrealMarkup.html | False | Snippet shortcuts inside UE macros |
| OptionsHelp/UnrealEngine/CheckColorInsideUnrealMarkup.html | False | UE macro arguments extra coloring |
| OptionsHelp/UnrealEngine/RadioIndexPluginsRef.html | referenced-or-enabled-by-default | UE plugin directory parsing and indexing |

일반 프로젝트 시작 파싱 기본 활성 및 managed 참조 열거 기본 활성은 성능 비교의 입력 조건입니다. CPU 100% 원인이나 CodeAssist 수정 효과를 입증하지 않습니다.

## 단축키 설정 정적 분석

[REA 근거](validation/rea/keybinding-reconstruction-summary.json): VaOptKeyBinds.17.dll의 88개 메서드를 재구성하고 원본 token/signature/IL 해시로 대조했습니다. 원본을 실행하지 않았으며 실제 키 배정과 기능 동등성은 미확인입니다.

- 0x06000090: 番号-index registry fields Command/Binding/Enabled/CommandScope; empty command or shortcut rejects record; semicolon scopes Global/Text Editor; undefined Enabled remains distinct
- 0x06000093: Shortcut chords preserve comma-space sequence; plus components uppercase and sort before comparison; component order does not matter but chord order does
- 0x06000094: FindBestMatch scores command and shortcut 10 each, scope equality 1 each; exact 22 returns immediately; ties retain earliest encountered
- 0x0600008e: RemoveExistingScopes removes only intersecting Global/Text Editor flags
- 0x06000092: Only defined Enabled state is written by WriteStatusToRegistry
- 0x06000078: GetDefaultMapping lists 11 existing IDE command conflict descriptions; these are not VA shortcut assignments
- 0x06000057: Dialog OK handler returns OK and closes; this handler alone does not apply IDE key bindings

충돌 설명 매핑 11개는 VA 기본 단축키 목록이 아닙니다. 실제 배정 루틴·초기값·키 실행 비교는 남아 있습니다.

## 네이티브 캐시 근거 검토

[원본·재구성 해시 대조 장부](validation/native-cache-review.json): 기존 Ghidra 결과 4개를 현재 VA_X64.dll 해시와 대조했습니다. REA native provider를 새로 실행한 결과는 아닙니다.

- Statistics function reads cache-hit/miss, filesystem avoidance and include-resolution counters.
- Four entry counts are reported for main/local caches with and without relative-directory context.
- Shared SRW lock protects traversal when collecting directory/entry statistics.
- A registry-reading helper is invoked for NewFileFinderCacheTest in a one-time initialization branch; flag meaning/default runtime unverified.

FileFinder 통계 함수는 hit/miss·filesystem 호출 회피·include 해석·main/local 및 relative-directory 구분 카운터를 읽습니다. 캐시 lookup·무효화·save/load 알고리즘을 입증하지 않습니다. 저장 추적 문자열의 함수 RVA를 다음 분석 대상으로 특정했습니다.

전체 참조 사전 베이크, 설치 파일 크기와 참조 인덱스의 관계, CPU 100% 원인은 미확인입니다.

## 캐시 저장 호출 경로

[원본 범위 디스어셈블 근거](validation/native-cache-save-flow.json): RVA 0x378A70 및 0x3795A0을 설치 DLL에서 LLVM으로 다시 읽었습니다. 원본 실행이나 REA native provider 실행은 하지 않았습니다.

호출부는 object+0x00과 object+0x10의 두 컨테이너를 같은 함수에 전달합니다. callee는 UTF-16 형식 문자열 `%s§%s§%x` 및 newline을 formatting helper에 전달하고, 길이를 두 배로 하여 vtable+0x78의 간접 호출에 넘깁니다. 실제 필드 의미·writer·파일 경로·전체 형식은 미확인입니다.

조건부 early return에는 object+0x58에서 얻은 pointer의 길이 후보 및 global 객체의 +0x1EC9 byte가 사용됩니다. 해당 byte를 특정 옵션 이름으로 단정하지 않습니다. load·무효화·오류 처리·실제 출력 bytes 비교는 남아 있습니다.

## 실제 VA 데이터 경로 읽기 전용 관찰

[수동 관찰 장부](validation/passive-va-data-snapshot.json): 설치 확장 Data/vs17_1에서 620개 파일, 47438073 bytes를 관찰했습니다. VA 실행·UI 조작·파일 변경 없이 파일 메타데이터와 지정한 4개 파일의 최대4096byte prefix 해시/형식 지표만 기록했습니다. 내용·개인 경로를 복사하지 않았습니다.

| 그룹 | 파일 수 | bytes |
|---|---|---|
| fileids.va | 1 | 1449042 |
| version.dat | 1 | 9 |
| cache | 3 | 0 |
| cpp | 459 | 45837515 |
| DbInfo | 6 | 2270 |
| net | 98 | 0 |
| Proj | 2 | 20 |
| ProjectInfo | 1 | 6690 |
| Proj_3001577562 | 48 | 142484 |
| typeHist | 1 | 43 |

cpp 데이터는 존재하지만 전체 참조 베이크 여부·생성 함수·정확한 스키마·FileFinder 저장과의 연결은 미확인입니다. 기존 파일의 존재는 현재 알고리즘·성능·동등성 검증을 대신하지 않습니다.

## 캐시 파일 열기·로드 경로

[원본 어셈블리·vtable·import 대조](validation/native-cache-open-load-flow.json): writer helper의 vtable+0xB0은 원본 PE에서 RVA0x182D00으로 연결되며 그 함수는 CreateFileW를 호출합니다. helper는 실패 반환0에 대해 Sleep(250)을 최대10회 수행합니다. 실제 실행 스레드와 지연 발생은 미확인입니다.

로드 RVA0x377DF0은 저장과 같은 global byte gate와 두 suffix 경로를 사용합니다. loader 호출 후 dword 비교 불일치에 별도 함수 RVA0x3780E0을 호출합니다. 비교값의 의미·reset 대상·무효화 알고리즘은 아직 입증하지 않았습니다.

retry 상한의 존재만으로 VS 시작 버벅임이나 CPU100 원인을 단정하지 않습니다. 실제 VS 추적이 필요합니다.

## 캐시 헤더 계산·초기화

[함수별 근거](validation/native-cache-header-reset.json): RVA0x32B250은 NUL 종료 UTF-16 단위를 처리하는 32bit rolling 연산입니다. 일반 경로는 129배 누적이며 ASCII 대문자는 별도 packed-bit 처리를 합니다. 대소문자를 단순 정규화하는 hash라고 부르지 않습니다.

RVA0x3780E0은 EnterCriticalSection을 호출하고 두 container의 sentinel/size를 초기화합니다. context helper 출력을 세미콜론으로 결합해 object+0x60을 갱신합니다. helper 출력의 의미와 전체 취소·예외 경로는 미확인입니다.

Python 재구성의 7개 입력 실행은 원본 코드 실행·동등성 검증이 아닙니다. 이 자료로 파일 timestamp/content 변경을 모두 감지한다고 단정하지 않습니다.

## 캐시 기록 파싱·경계 조건

[로더 전체 범위와 숫자 helper 근거](validation/native-cache-parser-rules.json): 헤더 newline 후 숫자 outer key·section sign·문자열·section sign·숫자 status를 읽는 정적 구조를 확인했습니다. 숫자 helper 원본 format은 `%x`이고 결과 local을 0으로 초기화합니다. 각 필드의 업무 의미는 미확인입니다.

헤더 newline이 없으면 항목 처리 전 cleanup으로 나갑니다. 항목 구분자가 없으면 loop를 종료하고, 마지막 newline이 없으면 buffer 끝을 사용합니다. status 0/0xFFFFFFFF는 invalid branch에서 0xFFFFFFFF 및 빈 associated string으로 처리됩니다. scanner의 실제 permissiveness·overflow·read helper encoding은 미확인입니다.

원본 비교 대기: Missing header newline; Missing first or second section-sign; Final entry with no newline; Invalid/empty hexadecimal field; Status0/status0xffffffff/status1/status0xfffffffe; Header-context mismatch; Partial read failure and retry exhaustion.

저장·헤더·초기화·파일 열기·로드의 부분 근거를 연결했으나 전체 캐시 분석 완료나 semantic 참조 인덱스 복원으로 계산하지 않습니다.

## 통합 증거 장부 검증

[증거 연결·해시 검증 결과](validation/evidence-audit-summary.json): 149개 검증 통과, 0개 실패입니다. 108개 ID 중복·문서 명세 수·분석 파일 연결·원본 DLL/도움말 해시·88개 단축키 재구성 해시·캐시 어셈블리 해시와 HTML 연결을 대조했습니다.

이 수는 근거의 무결성 검사이며 기능 테스트 통과 수가 아닙니다. 개별 기능 장부에 연결된 원본 실행 근거 행은 3개이고 완전 동등성은 0/108입니다. 기존 별도 runtime 자료는 해당 기능 전체 완료 조건과 대조하기 전까지 동등성 판정으로 승격하지 않습니다.

## 기존 원본 관찰과 현재 구현의 제한 비교

[관찰 연결·현재 결과](validation/historical-runtime-comparison.json): Open File in Solution의 기존 입력10개와 현재 NavigationQuery의 세 파일 결과 집합을 대조해10개 통과했습니다. Smart Select 메뉴2단계와 Move Scope4회 관찰도 각 기능 행에 연결했습니다.

기존 원본을 이번에 재실행한 자료가 아닙니다. 파일 경로 라벨 변경 이력이 있고 원본 screenshot/fixture snapshot을 독립 재검증하지 않았으므로 그 한계를 보존합니다. Alt+Down은 당시 VS 행 이동으로 처리됐고 Shift+Alt+]도 메뉴 결과와 달라 VA 키 동등성 근거에서 제외합니다.

core 결과 집합의 일치만 확인했으며 picker UI·단축키·옵션·취소·성능 전체 통과로 승격하지 않습니다. 전체 동등성0/108 유지.

## VS 프로젝트·파일 구성 전체 managed adapter

[REA 재구성 근거](validation/rea/vs-configuration-reconstruction-summary.json): 관리형 VS 통합 DLL의47개 메서드를 token/signature/IL로 대조하고 재구성 파일47개 hash를 검증했습니다. 이는 검사 구성 adapter의 정적 분석 범위이며 일반 native parser 전체 구현이 아닙니다.

- 0x0600001d: Cache invalidation clears project options, sanitize, directory-exists, unavailable-file-tool, makefile settings and project-file caches, then increments generation under shared lock.
- 0x06000017: Every ItemPropertyChange2 clears caches; SomethingChanged only for active configuration and listed property names.
- 0x06000019: Makefile cache key projectPath+NUL+configuration; compute outside lock, admit only unchanged generation.
- 0x0600001a: Microsoft.Build.Evaluation loads project with Configuration/Platform globals; ClCompile/ClInclude evaluated metadata yields file forced includes, extra include directories, definitions; first duplicate full path wins with OrdinalIgnoreCase; UnloadProject in finally.
- 0x0600001c: Per-file makefile lookup overrides forced includes, directories and preprocessor definitions; exceptions logged and caught.
- 0x06000012: Sanitize cache key combines include text, project directory and relative-path flag; cache-generation check controls admission.
- 0x0600000e: Evaluated rule CompileAs distinguishes C/C++; non-C LanguageStandard maps14/17/20/latest99; AdditionalOptions can override standard.

활성 구성 변경 통지 대상: WindowsTargetPlatformVersion, AdditionalIncludeDirectories, NMakePreprocessorDefinitions, CompileAsManaged, LanguageStandard, PlatformToolset, PreprocessorDefinitions, TargetMachine, CLRSupport, CompileAs, ForcedIncludeFiles, IncludePath, NMakeIncludeSearchPath, NMakeForcedIncludes, AdditionalOptions.

원본 비교 대기: Change active/inactive configuration properties and observe cache invalidation vs reparse event; Conditional evaluated per-file metadata in makefile ClCompile/ClInclude items; Duplicate file paths with differing case; Invalidate while a cache miss calculation is pending; CompileAsC vs CompileAsCpp with .c extension fallback; LanguageStandard versus trailing /std additional options; MSBuild missing project/invalid path/exception cleanup.

MSBuild 실제 평가·VS property 변경 이벤트·취소/수명 runtime 검증은 미실행입니다. 제품 코드는 추가하지 않았습니다.

## 조건부 프로젝트·파일 구성 비교 입력

[독립 MSBuild 평가 결과](validation/configuration-fixture-evaluation.json): import된 BASE 정의·파일별 Debug/Release include·Win32 강제 include·대소문자 중복 파일·header metadata를 포함한 작은 프로젝트를 Debug/x64와 Release/Win32로 평가했습니다. 예상 metadata 검증을 통과했습니다. VA DLL과 현재 CodeAssist product 경로를 실행 비교한 결과는 아닙니다.

현재 ProjectCompilationModel source 경로는 PropertyGroup/ItemDefinitionGroup에서 raw값을 읽고 ProjectDir/SolutionDir를 치환합니다. 이 경로의 Import·ItemGroup 파일 설정·조건 평가 누락을 source에서 확인했습니다. 전체 제품의 모든 구성 경로가 없다고 단정하지 않습니다.

- Evaluated project configuration and per-file metadata require a separate collector from raw generated-project candidate scanning.
- Preserve Configuration/Platform and imported metadata inheritance; do not collapse all targets into one current context.
- Apply per-file overrides to copied immutable option snapshot; cache admission must reject changed generation.
- Keep raw candidate fallback visibly incomplete when compiler/evaluation backend unavailable.

원본 adapter·VCProjectEngine와 같은 입력의 실행 비교가 남아 있으므로 전체 동등성0/108 유지.

## 전체108개 기능 통합 구현 설계안

[기능별 배치·공통 계약](validation/integrated-architecture-design.json)은 CodeAssist의 독립 설계안입니다. 원본 내부 구조·구현 완료·분석 완료를 주장하는 자료가 아닙니다.11개 책임 범위에108개 항목을 중복/누락 없이 배치하고10개 공통 계약과 남은 원본 검증 조건을 연결했습니다.

| 모듈 | 기능 범위 | 책임 |
|---|---|---|
| navigation | 1–14 | Semantic locations·query·scope·history·파일 공급원 |
| refactoring | 15–41 | 문맥별 action 판정·preview·파일 배치·참조 변화·Undo |
| editing-completion | 42–56 | Completion/suggestion 공급원·commit·선택·clipboard·surround |
| understanding | 57–68 | 색상·강조·semantic context·tooltip·print/RTF |
| correction | 69–75 | 수정 판정·brace/spelling/symbol 오류·output file links |
| inspection | 76–79 | 검사 요청·결과 수명·quickfix span·겹침·취소 |
| snippets | 80–88 | 언어·문맥별 template·필드 수명·reserved/environment expansion |
| debugging | 89–92 | Debugger session·address/PDB/memory/step context |
| tool-windows | 93–94 | Outline/View 결과 모델·정렬·필터·문서 및 solution 수명 |
| source-links | 95–100 | 패턴 매칭·preview·browser/plugin 경계·취소 |
| integration | 101–108 | Command/key/mouse·확장자·platform/build adapter·옵션·동시성 |

### 공통 계약

- **CommandRegistry**: ID/menu/key/scope/availability/order/help from one schema; include IDE conflict ownership and profile-specific localized gestures — 미확인: Original native availability and ordering per command
- **WorkspaceConfiguration**: Immutable active workspace/configuration/platform/language/file option snapshot; VS evaluated adapter and external build adapter; raw fallback marked incomplete — 미확인: Generic native parser compile flags and event ordering
- **AnalysisScheduler**: Subsystem-specific cancellation/revision/queue budgets; same-file exclusion where proven; reject stale result publication — 미확인: General parser limits; inspection limit must not apply globally
- **CacheServices**: Separate FileFinder lookup persistence, semantic symbol/reference index, compiler option caches; independent schema/context/generation/invalidation — 미확인: Semantic index layout and every cache invalidation condition
- **EditTransaction**: Snapshot version check, overlap detection, preview, coordinated multi-file Undo, caret/selection restore; cancellation before applying edits — 미확인: Original Undo grouping and refactoring-specific rollback edge cases
- **SemanticBackend**: Language-specific symbol identity/type/hierarchy/read-write/overloads and diagnostics; compiler and textual candidate results remain distinguishable — 미확인: All native semantics, C# provider behavior and incomplete-code heuristics
- **SettingsSchema**: Stored values, installation-help defaults and observed factory-reset values separate; version/theme/language constraints and dependent options explicit — 미확인: Most exact setting bindings and full factory palette
- **UIResultModels**: Stable identity, result kind, scope, filters, selection, sorting and cloning state independent from view; lazy data loading and cancellation — 미확인: Exact layout/icons/focus/accessibility/async behavior
- **SnippetEngine**: Environment snapshot then reserved/template expansion; per-invocation shared input and GUID; inline field and dialog state independent from text mutation — 미확인: All reserved values, nested/escaped templates and inline Undo
- **ValidationOracle**: Per-feature original input/output/version/settings/caret/Undo/UI/key/performance evidence; full parity only when all relevant gates pass — 미확인: Required original runtime comparisons remain unexecuted

제품 코드를 변경하지 않았습니다. 원본 입력·키·UI·설정·Undo·성능의 전체 분석/비교가 충족되어야 통합 구현 결과를 동등성으로 판정합니다.

## 설치 편집·수정·강조·debug 옵션 조건

[17개 기능의 설치 도움말 조건](validation/installed-editor-feature-spec.json)에 실제 파일 SHA-256·적용 조건·비교 사례를 기록했습니다. 원본 도움말 hash는 현재 설치 파일과 일치합니다. 실제 control 바인딩·factory값·Undo/키/실행 결과는 미확인입니다.

### Auto-Extend Multi-Line Comments

- 줄바꿈 시 block comment 및 연속 single-line comment marker를 현재 줄에서 이어받음

원본 비교 대기: block·single-line·끝줄·선두공백 비교.

### Format After Paste

- 여러 줄 paste 이후 IDE Format Selection으로 서식 지정
- Undo로 formatting 철회 가능

원본 비교 대기: 단일/다중 줄 paste 및 Undo 단계 비교.

### Insert () and Closing Brackets and Quotes

- 여는 delimiter 뒤 닫는 delimiter·method commit 후 괄호 삽입
- Undo/Backspace 철회; 입력 중단 전 inserted delimiter 입력은 해당 문자 덮어쓰기
- 닫는 brace 위치는 same/next/blank-line 옵션

원본 비교 대기: typing 중단·중복닫힘·commit·brace 위치 비교.

### Insert _ After m and Shift

- m 입력 직후 Shift를 빠르게 눌렀다 놓으면 underscore 삽입

원본 비교 대기: Shift 타이밍·연속입력·IME 비교.

### Multiple Clipboards

- Ctrl+Shift+V 최근 clipboard 메뉴
- 표시 개수는 별도 설정이며 해당 도움말은 수치를 명시하지 않음

원본 비교 대기: capacity·중복·선택·표준paste·Undo 비교.

### Repair Case

- typing 중 symbol 대소문자 수정; 여러 교정 후보가 있으면 수정하지 않음
- C#은 suggestion/completion 목록 활성 필요

원본 비교 대기: 단일/복수 후보·C# 조건·Undo 비교.

### Convert Dot to ->

- 적절한 문맥에서 dot를 arrow로 바꿈; operator-> smart object 변형은 별도 옵션
- Delete/Undo/Backspace로 철회

원본 비교 대기: pointer·instance·smart object·철회 비교.

### Surround Selection

- slash는 완전한 줄/복수 줄이면 line comment, 부분 줄이면 block comment 토글
- asterisk는 block comment 토글; parentheses/braces surround
- surround character가 있는 snippet 입력은 선택을 expansion으로 대체

원본 비교 대기: 부분/전체/복수 줄·기존주석·snippet·Undo 비교.

### Highlight Current Line

- caret 줄 표시; VA Current Line IDE 색 설정 사용
- 배경·색상/frame·점선·실선·margin까지 ray 형식

원본 비교 대기: theme·포커스·색·표시방식 비교.

### Highlight Files in Output Window

- build output에서 solution 파일 경로를 다른 파일 경로와 구별해 강조

원본 비교 대기: solution 내부/외부·상대/절대경로·출력갱신 비교.

### Highlight Find Results

- IDE find 후 editor matches 강조; ESC로 해제; IDE Find Result(VA) 배경색 사용

원본 비교 대기: 새검색·ESC·문서전환·theme 비교.

### Highlight Matching Braces and Parentheses

- caret가 scope 안에 있을 때 matching delimiter 강조

원본 비교 대기: 중첩·문자열/주석 delimiter·caret 경계 비교.

### Highlight Mismatching Braces and Parentheses

- caret 문맥에서 균형이 맞지 않는 delimiter 강조

원본 비교 대기: 불완전코드·여는/닫는짝·macro 비교.

### Underline Spelling Errors

- 주석과 문자열 spelling 표시; 문맥 메뉴에서 교정 또는 사전 추가

원본 비교 대기: 언어사전·주석/문자열·사전수명·Undo 비교.

### Underline Unknown Symbols

- mistyped/context-invalid symbol 표시; IDE squiggle 표시가 비활성일 때만 적용

원본 비교 대기: IDE표시 토글·정의중·include/macro·취소 비교.

### VA Memory View

- debug session 시작 전에 integration 활성 필요; 완전한 분리는 IDE 재시작 필요

원본 비교 대기: 세션전/중 토글·재시작·debugger state 비교.

### VA Step Filter

- integration 활성 시 창 숨김 상태에서도 active filter 적용
- solution filter는 APPDATA 또는 .va/shared; global/builtin 상태는 APPDATA
- 공유 위치에 신규 solution filter 저장하려면 .vadbg 쓰기 권한 필요

원본 비교 대기: global/solution 우선순위·창숨김·권한·세션수명 비교.


## Source Links·플러그인 정적 분석

[REA 근거](validation/rea/source-links-analysis-summary.json): 본체208타입/1438메서드/9399call edge, API11타입/75메서드/470call edge를 추출했습니다. 선택66메서드 재구성을 원본token/signature/IL 및 재구성hash로 대조했습니다. 미해결 타입 주석 0개가 남아 있습니다.

### Simple Link Patterns

- 설치 도움말: comment keyword는 세미콜론 분리·대소문자 무시, suffix는 단일 문자 집합
- value 양끝 공백 무시·복수단어는 quote·타입 불일치면 link 미생성
- 정적 line cache는 comment classification 범위를 검사하고 별도 plain-text 허용 gate가 있음

원본 비교 대기: keyword/suffix 공백·quote·타입 불일치; 동일 줄 edit/재배치·plain text gate·comment classification.

### Complex Link Patterns

- HwPrefs.GetRegexPattern은 RE= 접두사를 대소문자 무시로 판정하고 뒤 pattern 반환
- CreateRegex는 IgnoreCase/Singleline 사용; runtime은 Compiled 추가
- timeout 생성자를 reflection으로 시도하고 실패하면 timeout 없는 생성자로 fallback
- marker match 시간이 threshold 이상이면 해당 regex/group 슬롯 비활성화; 기본 getter fallback100ms는 실제 전체 실행시간 상한 보장 아님

원본 비교 대기: RE= 대소문자·빈/잘못된pattern·group; timeout 생성 실패fallback·긴match·재활성화 수명.

### Custom Plug-ins

- REA metadata: IPluginFactory Name/CreatePlugin, IPlugin Flags/GetTooltip/GetUrl/Execute/ContextExecute/GetRegex/Configure 계약 존재
- ValidateType은 분리 AppDomain helper에서 검증하고 finally에 validation domain 참조를 해제
- 검색 경로는 known directories·AppDomain base/relative path·추가 plugin paths를 모음
- SourceLinksAST에는 automation/test helper가 포함되므로 grammar parser로 해석하지 않음

원본 비교 대기: 올바른/잘못된 type·의존성 누락·validation 예외; plugin settings scope·설정변경·dispose·reload.

### Open Hyperlinks in an External Browser

- 설치 도움말: plugin이 URL을 제공하면 behavior 설정으로 internal/external browser 결정
- plugin이 override한 preference는 read-only로 표시됨
- URL template은 Value 및 선택 Keyword placeholder를 사용; 실제 launch/escape 처리는 미확인

원본 비교 대기: plugin override·internal/external·URL expansion; 실행실패·keyboard/mouse/QuickInfo·선택 URI.

원본 DLL·plugin·automation helper는 실행하지 않았습니다. 실제 URL 열기·network·credential 접근을 수행하지 않았습니다. 문서 조건과 정적 흐름은 기능 동등성 증명이 아닙니다.

## 남은 기능의 공식 계약 일괄 조사

[공식 문서별 근거·경계 조건](validation/whole-scope-document-contracts.json): 현재공식문서의동작명세이며2630실행완료와구분합니다.

### GoTo Member

- 현재 type/variable 또는 enclosing structure 공백에서 해당 member 탐색; enum/interface/namespace/struct/typedef 지원
- base member 포함 옵션·다중문자/negative/점prefix 필터·복사·Find References·type별 정렬

[공식근거](https://www.wholetomato.com/documentation/navigation/goto-member) · 원본비교대기: 각 구조종류·inherited toggle·동명member·type/name sort.

### Navigate Back and Forward

- 단순 caret 이동 waypoint는 제외; Alt+Left/Right는 다른 명령 바인딩 영향을 받음
- Alt+Right의 Edit.CompleteWord 바인딩 제거 조건; waypoint 종류의 icon 표는 별도 확인 필요

[공식근거](https://www.wholetomato.com/documentation/navigation/navigate-back-and-forward) · 원본비교대기: 편집/파일전환/Find reference waypoint와 cursor column 복원; keyboard conflict·history branch·닫힌문서.

### Open Corresponding File

- Alt+O: 대응 하나면 직접 열고 복수면 선택 메뉴; C++ 및 C# design/codebehind 등 관계 지원
- 현재 directory 우선; 없으면 project·parent·inc/include 검색; type 미확정이면 유사prefix 후보

[공식근거](https://www.wholetomato.com/documentation/navigation/open-corresponding-file) · 원본비교대기: 다중 대응·project외·이름접두사·경로동명 후보.

### VA Hashtags

- 주석 내 공백없는 tag; 대소문자 무시·다중tag·Goto exact/창 substring 검색
- 자동갱신·tag/file 그룹·AND/negative/OR필터·Shift+Alt+H/F8/Shift+F8
- 숨김은 tag/file/recursive directory/project 범위; tool window만 영향·Goto/완성 가능
- solution .vaHashtags 외부변경은 IDE재시작; read-only면 규칙 저장 불가

[공식근거](https://www.wholetomato.com/documentation/navigation/va-hashtags) · 원본비교대기: 주석/전처리·case·편집갱신·숨김범위; 외부변경·read-only·solution/global 전환.

### VA Navigation Bar

- source창 상단/하단·표시 옵션; context/method list/scope arrows/definition/Go 구성
- Go 기본은 implementation이며 related로 변경 가능; namespace 표시 문맥메뉴
- 공식 현재 문서의 다중 project 전환은2630 가용성 별도 확인

[공식근거](https://www.wholetomato.com/documentation/navigation/va-navigation-bar) · 원본비교대기: 문서·theme·namespace·Go설정·project 전환.

### Rename

- 선언/정의/참조 및 선택적 comment/string·상속chain 변경; project/solution 범위
- 이전 Rename/ChangeSignature/FindReferences scope 공유; 동일줄 복수ref는 복수항목
- scope변경으로 결과 재검색하면 제외선택 소실; preview double-click은 원본파일focus 유지
- Stop/Ctrl+Break로 검색중단 가능; 불완전검색 commit의 실패 가능성 문서 경고

[공식근거](https://www.wholetomato.com/documentation/refactoring/rename) · 원본비교대기: 상속·주석proximity·범위/제외·취소; 다중project·중단검색·preview focus·Undo.

### Move Implementation to Source File

- header에 declaration 남김; class/overload 인접배치·CreateImplementation snippet 사용
- 기본 인접배치 또는EOF 옵션; inline template는 먼저 class밖 header로 이동하고 다음 실행에 source로 이동
- 이동 후 VA history로 source/header 왕복

[공식근거](https://www.wholetomato.com/documentation/refactoring/move-implementation-to-source-file) · 원본비교대기: template 두단계·overload·EOF·snippet·Undo.

### Move Selection to New File

- 전체줄 선택만; destination은 미존재파일·header면 현재파일 include 옵션
- 언어·header/source별 생성snippet으로 새파일 내용 구성

[공식근거](https://www.wholetomato.com/documentation/refactoring/move-selection-to-new-file) · 원본비교대기: 부분줄·기존destination·언어template·include·Undo.

### Rename Files

- 관련 C++ h/cpp 및 C# designer/resx 함께 rename; include/project/solution/source-control 반영
- 기존directory 유지; rename전 저장·rename후 변경내용은 자동저장 안함
- 단일Undo 불가; nonproject/solution없는 파일에서는 의도 확인

[공식근거](https://www.wholetomato.com/documentation/refactoring/rename-files) · 원본비교대기: related files·include·SCC·충돌·read-only·save/Undo.

### Move Method Implementations to Source File

- class이름에서 모든method 이동 command·선택dialog
- nested class 선택 시 두번째dialog; template/배치 규칙은 단일move와 공유

[공식근거](https://www.wholetomato.com/documentation/refactoring/move-method-implementations-to-source-file) · 원본비교대기: 중첩class·선택subset·template·Undo.

### Move Implementation to Header File

- free function는 header, member는 class내부; 기존declaration 대체·없어도 생성
- declaration의 preprocessor/default값 보존·parameter이름은 implementation 사용
- 연관성이 충분한 선행주석 이동·이동구현 선택 유지·생성snippet 사용

[공식근거](https://www.wholetomato.com/documentation/refactoring/move-implementation-to-header-file-class-declaration) · 원본비교대기: 선언없음·default이름차이·전처리·주석·선택복원.

### Simplify Instance Declaration

- local object declaration에서만 제공; 생성된 instance를 불필요하게 다시 설정하는 declaration 단순화

[공식근거](https://www.wholetomato.com/documentation/refactoring/simplify-instance-declaration) · 원본비교대기: local/nonlocal·타입/initializer·주석·Undo.

### Add Member

- class reference에서 signaturedialog 후method/variable 추가; C++method는 inline header
- CreateImplementation snippet 사용·system directory class에는 제공안함·VS현대버전 한Undo

[공식근거](https://www.wholetomato.com/documentation/code-generation/add-member) · 원본비교대기: reference·method/field·systemdir·snippet·Undo.

### Add Similar Member

- class아닌 memberreference에서 기존signature dialog·원member 근처에stub추가·body복사안함
- base member에서 실행하면 base class에 추가·systemdir 제외·현대VS 한Undo

[공식근거](https://www.wholetomato.com/documentation/code-generation/add-similar-member) · 원본비교대기: base/derived·stub/body·배치·systemdir·Undo.

### Document Method

- 선언/정의에서 method 앞 commentblock·return/parameter 각각의 문서항목 생성
- DocumentMethod snippet과 refactoring전용 reserved값으로형식변경

[공식근거](https://www.wholetomato.com/documentation/code-generation/document-method) · 원본비교대기: declaration/definition·return/parameter·snippet·Undo.

### Create File

- 현재파일 directory에 생성·현재project에추가·C++/C#/idl/rc/xaml관련파일에서제공
- 짝없는 h/cpp는 대응파일이 기본후보·cpp생성시同名headerinclude·header면 include옵션
- CreateFile snippet이 초기내용/caret 위치 결정

[공식근거](https://www.wholetomato.com/documentation/code-generation/create-file) · 원본비교대기: 짝없는파일·언어별·project외·include·caret.

### Create Method Implementations

- class declaration에서미구현method선택·구현된method는목록제외
- 대응source없으면 declaration아래생성·생성snippet·인접/EOF 옵션

[공식근거](https://www.wholetomato.com/documentation/code-generation/create-method-implementations) · 원본비교대기: 일부기구현·대응없음·overload·배치·Undo.

### Add/Remove Braces

- 단일statement add/remove/toggle·nested if/else/for/while 문맥
- 복수statement compound는remove불가·indent만으로복수줄add불가
- source상단일macro가복수statement로확장하면 remove가동작변경가능하다는문서경고

[공식근거](https://www.wholetomato.com/documentation/code-generation/addremove-braces) · 원본비교대기: dangling else·nested·복수macro·toggle·Undo.

### Add Using Directive

- C# common.NET type 사용시목록suggestion·C#listbox활성필요
- accept하면상단using삽입·caret위치유지

[공식근거](https://www.wholetomato.com/documentation/code-generation/add-using-directive) · 원본비교대기: C#listbox설정·동명namespace·기존using·caret·Undo.

### Acronyms

- 축소 목록 옵션이 필요하며 첫 글자와 대문자/underscore 경계로 후보를 좁힘
- project 및 최근 사용 symbol 중심; 파일/심볼 검색 dialog와 method list에서는 사용하지 않음

[공식근거](https://www.wholetomato.com/documentation/coding-assistance/acronyms) · 원본비교대기: camel/Pascal/underscore·중복 약어·최근 사용·목록별 가용성.

### Shorthand

- 축소 목록에서 여러 부분 문자열로 후보 검색; acronym 우선과 최근 사용 영향
- 목록 밖에서는 첫 입력 글자로 시작하는 symbol로 제한; Ctrl+Space로 전체 completion 전환

[공식근거](https://www.wholetomato.com/documentation/coding-assistance/shorthand) · 원본비교대기: 부분 문자열·prefix 제한·약어 충돌·우선 선택.

### Reset Editor Zoom

- editor scope에서100%로 복원; Ctrl+0은 추천키 수락 조건
- VS2015+는 새 창의 global zoom도100%로 설정

[공식근거](https://www.wholetomato.com/documentation/coding-assistance/reset-editor-zoom) · 원본비교대기: 추천키 충돌·창별/global zoom·focus.

### Smart Suggestions

- 생성자·enum 대입/비교·인자 타입에 맞는 local 변수·주석 hashtag를 제안
- 타입 이름의 Smart Suggestions snippet으로 값을 확장; 인자 comma+space는 다음 제안 유도

[공식근거](https://www.wholetomato.com/documentation/coding-assistance/smart-suggestions) · 원본비교대기: 타입/overload·enum·사용자 snippet·comma 입력.

### Suggestion Lists

- 최근/주변 symbol과 code bits; C#에서는 IDE 목록에 병합하거나 VA 목록 사용
- symbol 정의 중에는 자동 선택하지 않고 Down으로 선택; parameter list 제안은 global scope 및 Tab/Enter commit
- 오른쪽이 알려진 symbol이면 유지하고 모르는 잔여 문자열은 제거

[공식근거](https://www.wholetomato.com/documentation/coding-assistance/suggestion-lists) · 원본비교대기: 정의/참조 문맥·C# 공급원·commit·우측 문자열.

### Surround With

- 선택을 snippet 코드로 감싸며 메뉴와 지정 surround 문자로 접근
- 단순 Surround Selection과 별개이며 Outline과 editor에서 가용 메뉴가 다름

[공식근거](https://www.wholetomato.com/documentation/coding-assistance/surround-with) · 원본비교대기: 문자·메뉴·다중 snippet·선택/Undo.

### Column Indicator

- 지정 column의 뒤쪽 경계에 표시; 위치/색 옵션
- 가변폭 글꼴은 m 글자 폭의 배수로 위치 산정

[공식근거](https://www.wholetomato.com/documentation/code-understanding/column-indicator) · 원본비교대기: 가변/고정폭·zoom·theme·가로 스크롤.

### Context Field

- symbol 문맥·scope 표시, unknown이면 비움; global scope는 현재 파일명
- field 폭 조절·scope 표시 축소·method dropdown·scope arrows·복사 메뉴

[공식근거](https://www.wholetomato.com/documentation/code-understanding/context-field) · 원본비교대기: 동명 scope·unknown·global·resize·IME/Unicode.

### Definition Field

- symbol 정의·scope 상세·include 실제 경로 표시; 복수정의 목록은 정보용으로 선택해도 이동 안함
- unknown은 비움; typedef/macro를 적당히 평가하며 aggressive 평가 옵션 별도

[공식근거](https://www.wholetomato.com/documentation/code-understanding/definition-field) · 원본비교대기: 복수 정의·include 경로·macro/typedef·복사.

### Local Symbols in Bold

- enhanced coloring이 필요하며 현재 파일에 정의된 이름에 bold 적용
- semantic scope와 별개로 동명 occurrence 모두 bold 가능; inherited listbox bold와 다른 옵션

[공식근거](https://www.wholetomato.com/documentation/code-understanding/local-symbols-in-bold) · 원본비교대기: 동명 다른 scope·다른 파일의 class member·theme.

### Parameter Info

- method 뒤 여는 괄호 또는 Ctrl+Shift+Space; 현재 인자 bold·위/아래로 overload 탐색
- C# 공급원은 default IntelliSense; disable 반영에는 IDE 재시작 조건

[공식근거](https://www.wholetomato.com/documentation/code-understanding/parameter-info) · 원본비교대기: 중간 인자 복귀·overload·macro·C#·재시작.

### Print in Color

- VS2015+ print에 VA 색을 포함하며 IDE 버전별 지원이 다름
- 출력 selection만 따로 파싱하므로 잘린 문맥의 색이 원본과 다를 수 있음

[공식근거](https://www.wholetomato.com/documentation/code-understanding/print-in-color) · 원본비교대기: 전체/일부 출력·theme·VS버전·context.

### Quick Info

- IDE tooltip 보완/대체·source 주석·닫는 brace/구조 keyword 문맥·Doxygen 형식 옵션
- IDE 버전별 공급원과 중복주석 조건이 다름; mouse 이동/ESC로 해제

[공식근거](https://www.wholetomato.com/documentation/code-understanding/quick-info) · 원본비교대기: VS버전·IDE 병합/대체·source/header 주석·macro·ESC.

### System Symbols in Italics

- system include와 Program Files의 symbol 등은 italics 후보이며 project override는 구별
- system 선언 DB는 solutions 간 공유 읽기 전용이며 처음 포함한 solution에서 파싱

[공식근거](https://www.wholetomato.com/documentation/code-understanding/system-symbols-in-italics) · 원본비교대기: 동명 override·NuGet/Vcpkg·ProgramFiles·재파싱.

### Code Inspection Settings

- 개별 검사 활성·quickfix 조정·level1~5 설정; level은 UI 색이며5는 dim 처리
- editor에서 개별 검사를 enable/disable할 수 없고 tool window에서는 disable만 가능

[공식근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection) · 원본비교대기: checker/level·설정 reset·조정·source suppression.

### Code Issues in the Editor

- solid 또는 dim은 fix 가능, dotted는 불가; underline과 scrollbar 위치 표시 별도
- scrollbar의 중복 issue 세부정보는 중복 제거; 표시 비활성이어도 결과 창에서 접근

[공식근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection) · 원본비교대기: 표시 토글·겹친 issue·level5·tracking.

### Quick Fixes in the Editor

- hover tomato 또는 issue 앞부분 caret에서 Shift+Alt+Q로 fix 접근
- 그룹 fix는 editor Undo 한 번으로 복원; 파서는 다른 VA 파서와 독립

[공식근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection) · 원본비교대기: 다중 fix·겹침·stale span·취소·Undo.

### Quick Fixes in a Tool Window

- 현재 파일 issue 기본 line 정렬; 단일/복수 선택과 checker/level 그룹 fix
- tracking·열 정렬·double click 이동; fix Undo는 결과 창이 아니라 editor에서 수행

[공식근거](https://www.wholetomato.com/documentation/code-inspection/introduction-to-code-inspection) · 원본비교대기: 그룹/복수 선택·sort·tracking·Undo.

### VA Snippets

- shortcut·title-only·surround·smart suggestion·refactoring 유형을 공유할 수 있음
- 편집 즉시 반영·IDE 간 사용자 snippet 공유·reserved 값과 caret 위치 확장

[공식근거](https://www.wholetomato.com/documentation/code-snippets/introduction-to-va-snippets) · 원본비교대기: 유형별 메뉴·수정 반영·언어·직렬화·caret.

### for Refactoring

- 지정 title로 필수 snippet을 찾고 삭제 시 기본 snippet 재생성
- refactoring 전용 reserved 값이 있으며 일반 symbol-context reserved는 null 확장

[공식근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-for-refactoring-and-code-generation) · 원본비교대기: title 변경/삭제·언어별 필수값·reserved 문맥.

### for Smart Suggestions

- class/loop/switch 및 타입 이름 snippet이 제안 값을 제공
- 목록은 알파벳 정렬이므로 template 내 값 순서는 영향 없음

[공식근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-for-smart-suggestions) · 원본비교대기: 타입/typedef·빈 제안·삭제와 재생성·정렬.

### for Surround With

- selected reserved 값은 현재 선택 또는 빈 문자열; 같은 surround 문자가 복수면 메뉴
- surround 문자 실행은 관련 옵션 필요; title accelerator 및 메뉴/shortcut 겸용

[공식근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-for-surround-with) · 원본비교대기: 선택 없음·부분/전체 줄·문자 충돌·accelerator.

### with Shortcuts

- shortcut은 case-sensitive; 중복 shortcut은 모두 표시하지만 동일 shortcut-title 쌍은 하나만 표시
- 수락하면 shortcut을 expanded 코드로 교체; 제목 없으면 코드 앞부분 표시

[공식근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-with-shortcuts) · 원본비교대기: case·중복 쌍·language·commit·Undo.

### Create from Selection

- editor 선택으로 snippet editor를 열고 title/shortcut/description 지정
- input placeholder와 end reserved를 편집하여 입력 dialog와 최종 caret 구성

[공식근거](https://www.wholetomato.com/documentation/code-snippets/va-snippets-create-from-selection) · 원본비교대기: 여러 줄·공백·문자 escape·입력·caret.

### Address Resolver

- EXE/DLL/PDB 여러 모듈의 주소를 symbol로 해석; 올바른 실행 base 필요
- PDB 없이 exports 모드·절대/relative 주소 형식; 미해결 주소는???·비주소 텍스트는 유지
- symbol server는 VS 설정 사용; 주소에서 symbol 단방향 해석

[공식근거](https://www.wholetomato.com/documentation/debug-assistance/address-resolver) · 원본비교대기: base/ASLR·PDB 불일치·여러 모듈·입력 형식·실패.

### PDB Explorer

- 한 모듈의 import/export 및 release PDB symbol 탐색; running process 없이도 가능
- debugger 이동에는 base 필요·종류/unit/zero/duplicate address 필터·열 정렬

[공식근거](https://www.wholetomato.com/documentation/debug-assistance/pdb-explorer) · 원본비교대기: release/debug PDB·base·중복주소·module refresh·disassembly 이동.

### VA Outline

- 현재 문서와 양방향 동기화·현재 scope bold·SAL 표시 제거
- 다중 선택·refactor 가용성·source 편집/drag/drop·comment/region 노드·hover
- AutoUpdate 꺼지면 focus 시 갱신; 자동expand는 기존노드를 자동collapse하지 않음

[공식근거](https://www.wholetomato.com/documentation/tool-window/va-outline) · 원본비교대기: 편집/선택/Undo·comment 포함·자동갱신·다중노드.

### VA View

- 파일/심볼 dropdown·MRU·hover class browser와 include hierarchy 구성
- MRU는 solution별 저장·재오픈 복원·여러 IDE는 마지막 종료가 저장
- HCB는 hover symbol 기준이며 공백/주석/unknown에서 갱신하지 않음

[공식근거](https://www.wholetomato.com/documentation/tool-window/va-view) · 원본비교대기: MRU 수명·다중 IDE·overload·include hierarchy·focus.

### Connect to FogBugz and GitHub

- issue/case tooltip은 plugin 설정과 read 권한 필요; 열기는 별도 browser login 필요할 수 있음
- 외부 서비스 API/인증의 현재 상태와2630 호환성은 미확인

[공식근거](https://www.wholetomato.com/documentation/source-links/built-in-plug-ins) · 원본비교대기: 공유/사용자 설정·권한 실패·tooltip 취소·browser login.

### Preview Files

- File Viewer는 comment 경로의 text/HTML/XAML/RTF/image/video 등을 preview
- relative 경로는 활성문서 기준·공백 quote/%20·환경값·HTML anchor 지원

[공식근거](https://www.wholetomato.com/documentation/source-links/built-in-plug-ins) · 원본비교대기: 로컬/network·경로공백·환경값·미존재파일·형식별 preview.

### Context Menus

- IDE context 메뉴를 확장할 때 VA submenu 구분
- 별도 VA 메뉴는 지정 modified mouse로 호출 가능

[공식근거](https://www.wholetomato.com/documentation/configuration/context-menus) · 원본비교대기: symbol/selection/file 문맥·focus·modified mouse.

### Custom File Extensions

- 무시 확장자는 세미콜론 구분·이미 열린 파일은 다시 열어 반영
- extensionless C++ header는 VA 옵션과 IDE 언어 mapping 필요; custom extension 절차는 IDE별 차이

[공식근거](https://www.wholetomato.com/documentation/configuration/custom-file-extensions) · 원본비교대기: ignore/reopen·extensionless·custom 언어·VS버전.

### Keyboard Shortcuts

- 기본키가 비어 있을 때만 설치 배정하며 기존 배정을 덮어쓰지 않음
- 추천키로 scope 확대·충돌키 재배정; global/editor scope 구분·현재키 조회

[공식근거](https://www.wholetomato.com/documentation/configuration/keyboard-shortcuts) · 원본비교대기: 기존키 충돌·추천 수락·scope·locale·다중 shortcut.

### Mouse

- modified click은 GoTo/Related/VA 메뉴에 연결하며 editor scope
- modified wheel은 no-op 또는 selection/block 확장·축소; Ctrl+Alt+wheel zoom은 유지

[공식근거](https://www.wholetomato.com/documentation/configuration/mouse) · 원본비교대기: click 조합·wheel 방향·selection·IDE 충돌.

### Multiple Platforms

- 편집 platform은 build platform 변경만으로 전환되지 않으며 C++ directories에서 선택 후 재시작
- platform별 header 검색 경로와 정의를 구별해야 함

[공식근거](https://www.wholetomato.com/documentation/configuration/project-directories) · 원본비교대기: Win32/x64/custom·build/edit 차이·재시작.

### Non-Microsoft Build Processes

- IDE 프로젝트 설정이 비어 있는 외부 build 환경은 Custom directories로 보완
- 검색 순서 유지·한 custom set 보존·변경 인식 Rebuild 조건; 모든 build system 자동 지원은 미확인

[공식근거](https://www.wholetomato.com/documentation/configuration/project-directories) · 원본비교대기: solution없음·custom 경로순서·환경값·Rebuild.

### Parse Concurrency

- 현재 공식 문서 기본은 모든 CPU/core; single 또는 maximum threads 제한 가능
- thread 제한 변경은 IDE 재시작; 검사 전용 max1/queue4와 일반 파서 설정은 별개

[공식근거](https://www.wholetomato.com/documentation/configuration/parse-concurrency) · 원본비교대기: 설정/재시작·프로젝트 시작/편집·검사동시성·실제CPU.


## 심볼 DB 생성·공유 모델: 사전 베이크 가설 수정

[공식 모델과 설치 모듈 크기](validation/shared-symbol-database-model.json): [공식 Project Directories](https://www.wholetomato.com/documentation/configuration/project-directories)는 미리 만든 심볼 DB를 배포하지 않는다고 설명합니다. system 선언은 solution 간 공유 읽기전용 DB, dynamic 선언은 변경가능 DB에 보관하며 project 최초 열기·header 변경에 파싱합니다.2630 native 내부 schema와 모든 수명 검증은 별도입니다.

기존 설치 inventory는1052파일/145,142,212bytes이며 큰 모듈은 valibclang2_64.dll64,157,280bytes, VA_X64.dll21,643,360bytes입니다. 이 크기를 사용자 프로젝트 참조 사전 베이크의 근거로 사용하지 않습니다. 확장자 DB파일이 없다는 관찰만으로 embedded 자료 전체 부재를 입증하지 않습니다.
