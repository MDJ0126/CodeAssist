# 남은 70개 비교 항목

108개 공개 카탈로그 중 장부에 부분 구현으로 대응하지 않은 항목입니다. 세부 옵션 전체 개수나 구현 완료 개수가 아닙니다. 기존 소스 재대조와 실제 구현을 진행합니다.

|ID|기능|현재 장부|
|---|---|---|
|VA-F005|GoTo Member|부분 구현 — 인덱스 기반 멤버 선택·이동; IDE·VA 비교 미검증|
|VA-F007|Highlight Current Line|부분 구현 — VS 현재 줄 강조 옵션 연결; IDE·VA 비교 미검증|
|VA-F016|Change Signature|부분 구현 — 컴파일러가 찾은 선언·정의·호출부의 매개변수 순서 재배치; 인수 추가·삭제·타입 변경과 함수 포인터·생략 기본 인수 미지원; IDE·VA 비교 미검증|
|VA-F017|Convert Between Pointer and Instance|부분 구현 — 지역 변수의 멤버 접근 전용 포인터↔인스턴스 변환; 주소 유출·delete·포인터 연산 거부; IDE·VA 비교 미검증|
|VA-F018|Encapsulate Field|부분 구현 — 선택 필드 private 처리 및 getter/setter 생성; 외부 참조 치환 없음; IDE·VA 비교 미검증|
|VA-F019|Extract Method|부분 구현 — 전역 void 함수로 선택 문장 추출; 입력·출력 매개변수는 사용자 지정, 클래스·템플릿·제어 이전 제외; IDE·VA 비교 미검증|
|VA-F020|Introduce Variable|부분 구현 — clangd 변수 추출 변경 미리보기; IDE·VA 비교 미검증|
|VA-F021|Move Implementation to Header File|부분 구현 — clangd 소스→헤더 구현 이동 미리보기; IDE·VA 비교 미검증|
|VA-F022|Move Implementation to Source File|부분 구현 — clangd 헤더→소스 구현 이동 미리보기; IDE·VA 비교 미검증|
|VA-F023|Move Method Implementations to Source File|부분 구현 — clangd out-of-line 수정을 메모리에서 순차 합성 후 일괄 미리보기; 기존 대응 파일 필요, 200회·30초 한도; IDE·VA 비교 미검증|
|VA-F024|Move Selection to New File|부분 구현 — 선택 텍스트 새 파일 이동; include·프로젝트 추가 자동화 없음; IDE·VA 비교 미검증|
|VA-F026|Rename Files|부분 구현 — 프로젝트 항목 이름 변경; include 갱신 없음; IDE·VA 비교 미검증|
|VA-F027|Simplify Instance Declaration|부분 구현 — 동일 타입 생성자 재초기화 단순화; 지원 문법 제한; IDE·VA 비교 미검증|
|VA-F028|Add Forward Declaration|부분 구현 — 명시적으로 입력한 클래스·구조체 전방 선언; IDE·VA 비교 미검증|
|VA-F029|Add Member|부분 구현 — 현재 줄에 사용자 지정 멤버 선언 추가; 클래스 자동 탐색 없음; IDE·VA 비교 미검증|
|VA-F030|Add Missing Case Statements|부분 구현 — 선택 switch와 사용자가 입력한 scoped enum에서 누락 case 추가; 복합 enum 표현식 제한; IDE·VA 비교 미검증|
|VA-F031|Add Similar Member|부분 구현 — 선택 선언 복제 및 멤버명 교체; IDE·VA 비교 미검증|
|VA-F032|Add Using Directive|부분 구현 — 현재 줄에 using namespace 지시문 추가; 참조 축약 없음; IDE·VA 비교 미검증|
|VA-F033|Add include Directive|부분 구현 — 중복 방지 include 추가; IDE·VA 비교 미검증|
|VA-F034|Add/Remove Braces|부분 구현 — 선택 문장 괄호 추가; 단일 함수 호출 문장만 제거 허용; IDE·VA 비교 미검증|
|VA-F035|Create Declaration|부분 구현 — 선택 정의의 선언 생성·삽입; 헤더 자동 선택 없음; IDE·VA 비교 미검증|
|VA-F036|Create File|부분 구현 — 새 파일 생성·열기; IDE·VA 비교 미검증|
|VA-F038|Create Implementation|부분 구현 — 선택 선언에서 파일 끝에 구현 틀 생성; throw 미구현 본문; IDE·VA 비교 미검증|
|VA-F039|Create Method Implementations|부분 구현 — 선택한 여러 선언의 구현 틀 생성; 지원 문법 제한; IDE·VA 비교 미검증|
|VA-F040|Document Method|부분 구현 — 메서드 Doxygen 주석 생성; IDE·VA 비교 미검증|
|VA-F041|Implement Interface / Virtual Methods|부분 구현 — 사용자가 입력한 가상 선언의 override 구현 틀 삽입; 기저 클래스 자동 열거 없음; IDE·VA 비교 미검증|
|VA-F042|Acronyms|부분 구현 — 머리글자 약어 추천 후보; IDE 필터 통과 미검증; IDE·VA 비교 미검증|
|VA-F047|Insert _ After m and Shift|부분 구현 — m 다음 짧은 Shift 입력으로 밑줄 추가; 옵션 기본 꺼짐; IDE·VA 비교 미검증|
|VA-F050|Shorthand|부분 구현 — 축약어 추천 후보; IDE 필터 통과 미검증; IDE·VA 비교 미검증|
|VA-F052|Smart Suggestions|부분 구현 — bool·포인터 선언의 예상 타입 리터럴 추천; 전체 문맥 추천 아님; IDE·VA 비교 미검증|
|VA-F054|Suggestion Lists|부분 구현 — 추천 목록 사용 토글 및 C++·스니펫·컴파일러 후보 연결; IDE·VA 비교 미검증|
|VA-F056|Surround With|부분 구현 — 선택 영역 스니펫 감싸기; IDE·VA 비교 미검증|
|VA-F057|Column Indicator|부분 구현 — 설정 가능한 컬럼 기준선; IDE·VA 비교 미검증|
|VA-F058|Context Field|부분 구현 — 지연 갱신 소유 타입 문맥 필드; 전체 VA 문맥 UI와 다름; IDE·VA 비교 미검증|
|VA-F059|Definition Field|부분 구현 — 커서 단어의 인덱스 서명 표시; 오버로드 의미 선택 미구현; IDE·VA 비교 미검증|
|VA-F061|Local Symbols in Bold|부분 구현 — 렉시컬 지역 변수 식별자를 굵게 표시; IDE·VA 비교 미검증|
|VA-F062|Highlight Find Results|부분 구현 — 검색 결과 버퍼에서 VS FindWhat 리터럴 강조; 정규식 검색·정확한 콘텐츠 타입 동작 미검증; IDE·VA 비교 미검증|
|VA-F064|Highlight References to Symbol Under Cursor|부분 구현 — 지연·단일 작업·캐시를 적용한 커서 심볼 렉시컬 참조 강조; 컴파일러 동등성 미검증; IDE·VA 비교 미검증|
|VA-F065|Parameter Info|부분 구현 — 컴파일러 signatureHelp 결과 및 현재 인수 번호 표시; 타이핑 자동 팝업 없음; IDE·VA 비교 미검증|
|VA-F066|Print in Color|부분 구현 — CodeAssist 분류색을 이용한 사용자 확인 후 컬러 인쇄; 주석·문자열 전체 VS 색상 재현 아님; IDE·VA 비교 미검증|
|VA-F068|System Symbols in Italics|부분 구현 — 외부 SDK·엔진 경로 심볼 기울임; SDK 경로 휴리스틱; IDE·VA 비교 미검증|
|VA-F069|Convert Dot to ->|부분 구현 — 확정 포인터 타입에서 점 입력을 화살표로 변환; 오버로드 operator-> 제외; IDE·VA 비교 미검증|
|VA-F070|Highlight Files in Output Window|부분 구현 — 출력·검색 버퍼의 C/C++ 파일 경로 분류색; 클릭 탐색은 VS 제공; IDE·VA 비교 미검증|
|VA-F071|Highlight Matching Braces and Parentheses|부분 구현 — 문자열·주석 제외 괄호 짝 강조; IDE·VA 비교 미검증|
|VA-F072|Highlight Mismatching Braces and Parentheses|부분 구현 — 불일치·미닫힘 괄호 강조; IDE·VA 비교 미검증|
|VA-F073|Repair Case|부분 구현 — 인덱스 기반 타입명 대소문자 교정; 지역 변수 교정 제외; IDE·VA 비교 미검증|
|VA-F074|Underline Spelling Errors|부분 구현 — WPF 사전 기반 주석·문자열 소문자 단어 밑줄; OS 사전 유무·IDE 실행 미검증; IDE·VA 비교 미검증|
|VA-F078|Quick Fixes in the Editor|부분 구현 — 커서·선택 범위의 clangd quickfix 선택과 미리보기; 자체 전구 UI 미구현; IDE·VA 비교 미검증|
|VA-F079|Quick Fixes in a Tool Window|부분 구현 — 도구 창에서 clangd quickfix 선택·미리보기·적용; 프로젝트 전체 수정 목록 아님; IDE·VA 비교 미검증|
|VA-F081|for Refactoring|부분 구현 — 메서드 구현 생성의 사용자 본문 템플릿과 예약 변수; 모든 리팩터링 템플릿 아님; IDE·VA 비교 미검증|
|VA-F082|for Smart Suggestions|부분 구현 — 기본·프로젝트 스니펫 추천과 Tab·Enter·마우스 커밋 연결; 실제 IDE 입력 검증 미완료; IDE·VA 비교 미검증|
|VA-F083|for Surround With|부분 구현 — selected 템플릿만 필터해 감싸기; IDE·VA 비교 미검증|
|VA-F084|with Shortcuts|부분 구현 — 단축어+Tab 확장; 추천 목록 활성 중에는 기존 처리 우선; IDE·VA 비교 미검증|
|VA-F085|Create from Selection|부분 구현 — 선택 영역 스니펫 저장; IDE·VA 비교 미검증|
|VA-F089|Address Resolver|부분 구현 — LLVM symbolizer를 이용한 바이너리 RVA 해석; 절대 주소·런타임 모듈 자동 매핑 없음; IDE·VA 비교 미검증|
|VA-F090|PDB Explorer|부분 구현 — LLVM pdbutil publics/globals 결과 열람; 형식 트리·전체 PDB 옵션 없음; IDE·VA 비교 미검증|
|VA-F091|VA Memory View|부분 구현 — 중단 모드에서 명시 주소의 64바이트 디버거 표현식 읽기; 라이브 실행 미검증; IDE·VA 비교 미검증|
|VA-F092|VA Step Filter|부분 구현 — 함수 wildcard 기반 step 이벤트의 최대 20회 step-out; 중단점 이벤트 제외, 실제 디버깅 검증 미완료; IDE·VA 비교 미검증|
|VA-F095|Simple Link Patterns|부분 구현 — 단순 소스 링크 규칙과 커서 위치 열기; IDE·VA 비교 미검증|
|VA-F096|Complex Link Patterns|부분 구현 — 정규식·이름 있는 캡처·타임아웃 소스 링크; IDE·VA 비교 미검증|
|VA-F097|Connect to FogBugz and GitHub|부분 구현 — GitHub·FogBugz URL 규칙 예제; 서비스 API 연동 없음; IDE·VA 비교 미검증|
|VA-F098|Preview Files|부분 구현 — 2 MiB 이하 파일의 읽기 전용 색상 미리보기 도구 창; IDE·VA 비교 미검증|
|VA-F099|Open Hyperlinks in an External Browser|부분 구현 — http·https 링크 외부 열기; IDE·VA 비교 미검증|
|VA-F100|Custom Plug-ins|부분 구현 — 설치된 VSIX MEF ICodeAssistPlugin 계약·문서 편집·미리보기·Undo; 별도 플러그인 SDK 배포·호환성 검증 없음; IDE·VA 비교 미검증|
|VA-F101|Context Menus|부분 구현 — 실제 명령을 편집기 우클릭 메뉴에 배치; IDE·VA 비교 미검증|
|VA-F102|Custom File Extensions|부분 구현 — 사용자 확장자 인덱싱·작업자·캐시 fingerprint; VS 언어 서비스 연결은 사용자 설정; IDE·VA 비교 미검증|
|VA-F104|Mouse|부분 구현 — 선택 옵션 Alt+클릭 정의 이동; VA 전체 마우스 제스처 미구현; IDE·VA 비교 미검증|
|VA-F105|Multiple Platforms|부분 구현 — 타깃별 compilation database 디렉터리 선택; 원격 플랫폼 디버깅 없음; IDE·VA 비교 미검증|
|VA-F106|Non-Microsoft Build Processes|부분 구현 — 외부 빌드 시스템 compile_commands.json을 clangd에 지정; 빌드 명령은 실행 안 함; IDE·VA 비교 미검증|
|VA-F107|Parse Concurrency|부분 구현 — 파서 스레드 수 설정과 작업자 전달; IDE·VA 비교 미검증|

## 0.0.74 작업 결과

초기 미대응 70개 중 26개에 부분 구현을 추가했습니다. 나머지 44개는 이번에 구현하지 못했습니다. 부분 구현은 기능 완료나 VA 동등성 통과가 아닙니다. 전체 108개 중 부분 대응 64개이며, 완전 동등성 검증은 0개입니다. 게임 중 요청에 따라 설치·실제 IDE 디버깅·PC 제어는 하지 않았습니다.

Welcome 한국어, 설치본 내부 도움말, 새 편집 명령도 포함했습니다. [0.0.74 빌드·검증 기록](validation/feature-batch-0.0.74.json)을 참조하세요.

## 0.0.75 추가 작업

초기 70개 중 55개에 부분 구현 코드가 연결되었으며, 15개는 여전히 미구현입니다. 전체 기준 부분 대응은 93/108개이고, 완료 검증·원본 동등성 통과는 0개입니다. 이는 구현 완료 86.1% 또는 원본 유사도 86.1%라는 뜻이 아닙니다. 자동 테스트 235개 통과, Release 전체 빌드, 패키지 바이너리 6개 해시 일치를 확인했습니다. 설치·PC 제어·실제 IDE 디버깅은 하지 않았습니다.

미구현: VA-F016 Change Signature, VA-F017 Convert Between Pointer and Instance, VA-F019 Extract Method, VA-F023 Move Method Implementations to Source File, VA-F052 Smart Suggestions, VA-F054 Suggestion Lists, VA-F062 Highlight Find Results, VA-F070 Highlight Files in Output Window, VA-F074 Underline Spelling Errors, VA-F078 Quick Fixes in the Editor, VA-F079 Quick Fixes in a Tool Window, VA-F081 for Refactoring, VA-F082 for Smart Suggestions, VA-F092 VA Step Filter, VA-F100 Custom Plug-ins.

[0.0.75 검증 기록](validation/feature-batch-0.0.75.json).

## 최종 0.0.76 소스 구현 묶음

초기 미대응 70개 모두에 기본 동작의 소스·명령을 연결했습니다. 장부의 미대응 항목은 0개이며 전체 108개가 **부분 구현**입니다. 전체 기능 완료·원본 유사도 100%를 뜻하지 않습니다. 각 행에 남은 지원 제한을 명시했습니다. 실제 IDE·VA 비교는 아직 수행하지 않았습니다.

자동 테스트 247개 통과, 생성된 C++ 6개 사례의 clang++ 구문 검사 통과, Release 전체 빌드, VSIX 바이너리 6개 해시와 기존 단축키 등록 24개 일치를 확인했습니다. [0.0.76 근거](validation/feature-batch-0.0.76.json).

설치·PC 제어·실제 디버거·인쇄 명령·컴퓨터 종료는 수행하지 않았습니다. [다음 검증 순서](handoff.html#next-verification-076)를 참조하세요.
