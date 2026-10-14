# Visual Assist 10.9.2630.0 역분석·실행 비교 기록

분석일: 2026-10-14 (Asia/Seoul). 대상은 사용자가 설치한 Visual Assist 평가판입니다.

## 범위와 증거 수준

초기 PE·import/export·.NET IL 분석에 이어 네이티브 실행 섹션 디스어셈블, 문자열 참조와 함수 범위 수집, Ghidra 선택 함수 디컴파일 및 설치된 VA의 Visual Studio 실행 비교를 수행했습니다. 원본 바이너리를 배포 결과에 포함하지 않습니다.

전체 기능의 동등성은 **미확인**입니다. 아래 지정한 함수의 분기와 파일 검색 동작을 확인했지만, 전체 파서 의미·캐시 포맷·실제 Unreal 프로젝트 성능·모든 단축키 충돌을 확인한 것은 아닙니다. 자동 디컴파일의 타입·함수 이름은 추정 결과이므로 원래 소스 코드로 취급하지 않습니다.

## 추가 네이티브 분석과 실제 비교

- [재귀 설치 목록](complete-inventory.json): 1,052개 파일, 145,142,212바이트. native 81개, managed-or-mixed 343개, 기타 리소스 628개입니다. 초기 루트 DLL/EXE 86개 목록과 범위가 다릅니다.
- 전체 메타데이터 분석은 하위 폴더와 SourceLinks의 `.slp`를 포함한 PE 424개를 검사합니다. 재현: `dotnet run --project tools/VAStaticAnalysis -c Release -- <설치폴더> tools/obj/va-full-managed.json --all`.
- [명령 목록과 키 설정 IL](command-inventory.json): 설치 PE 파일에서 VAssistX 명령 문자열 30종과 VaOptKeyBinds의 메타데이터·문자열·호출을 수집했습니다. 이 30종이 전체 기능 목록이라는 의미는 아닙니다. 등록 리소스 및 실행 메뉴와의 대조가 추가로 필요합니다.
- [VA_X64 네이티브 보고서](native/VA_X64.dll.native.json): `.pdata` 범위 45,349개, 집중 문자열 1,764개, 직접 문자열 참조 1,300개. 수집 개수는 의미 분석 완료율이 아닙니다.
- Ghidra 12.1.4에서 함수 45,412개를 식별하고 초기 선택 함수 180개와 Unreal 매크로 관련 7개에 대해 디컴파일 결과를 생성했습니다. 두 집합은 중복될 수 있습니다. 결과·경고·실패는 `tools/obj/ghidra-core`, `tools/obj/ghidra-macros`에 보존합니다. 재현 도구는 `tools/VANativeAnalysis/Invoke-Ghidra.ps1`, 대상 RVA는 같은 폴더의 `core-targets.txt`, `macro-targets.txt`입니다.
- [디컴파일 증거 해시](native/decompilation-evidence.json)는 위 결과 각각의 RVA·성공 상태·출력 해시를 기록합니다. 두 실행에서 180/180개, 7/7개의 결과 생성이 성공했습니다. 이것은 자동 디컴파일의 의미·타입 정확성이 모두 검증됐다는 뜻이 아닙니다. 추가 문자 표 초기화 후보 RVA `0xE600`은 기존 Ghidra 함수로 해석되지 않았으며 `tools/obj/ghidra-boundary/analysis.log`에 미해결 상태를 남겼습니다.
- RVA `0x48D990`과 `0x48DB00`의 좁은/넓은 문자 판별 함수에서 UCLASS, USTRUCT, UINTERFACE, UFUNCTION, **UDELEGATE**, UENUM, UPROPERTY 비교 분기와 서로 다른 숫자 반환값을 확인했습니다. 호출되는 `0x8F3500`, `0x8F33D0`에는 대소문자 설정 분기와 뒤따르는 문자 분류 표 검사도 있습니다. 분류 표 전체 의미는 **미확인**입니다.
- RVA `0x2C1E80`에는 Unreal 매크로와 `IndentStyle`을 검사하는 경로가 있습니다. 여기서 GENERATED_UINTERFACE_BODY, GENERATED_IINTERFACE_BODY, RIGVM_METHOD를 확인했습니다. 이 경로의 관찰을 전체 파서 지원의 증명으로 확대하지 않습니다.
- [설치 VA 실제 검색 기록](runtime/file-search.json): 네이티브 C++ 비교 솔루션에서 10개 검색 결과 집합을 기록했고 `main:6` 선택 후 편집기의 행 6·열 1을 확인했습니다. 파일명 기본 검색, 공백 AND, 쉼표 OR, 제외, 시작/끝 필터, 역슬래시 경로 검색을 구현·회귀 테스트했습니다. `#VAParity`는 0개였으므로 경로 문법으로 구현하지 않았습니다.

공식 [파일 탐색 문서](https://www.wholetomato.com/documentation/navigation/open-file-in-solution)와 실행 관찰을 함께 확인했습니다. fuzzy 오타 교정, 다중 선택, 정렬 상태 저장, 외부 파일 확장 범위, 모든 행·열 문법까지 동등하다고 주장하지 않습니다. `tests/VAParityFixture`는 UE 형태의 최소 C++ 스텁이며 실제 엔진 프로젝트가 아닙니다.

## Clang wrapper 추가 역분석

`valibclang2_64.dll`은 `B73B66FC925D43BCB6104819DEFC3A32` export 하나를 노출합니다. RVA `0x4E8E0`의 실제 명령은 전역 `0x183B77300`을 RAX에 읽어 반환하며 Ghidra 디컴파일도 이를 확인했습니다. 일반 libclang C API라는 가정을 하지 않습니다.

RVA `0x3690` 초기화 코드에서 0x18바이트 할당 호출, 첫 필드에 `0x182D72878` 저장, 플래그/필드 영 초기화, 전역 게시를 확인했습니다. [해당 테이블의 연속 실행 포인터 14개](native/clang-interface-table.json)를 수집했습니다. 역할·정확한 ABI 타입을 모두 복원했다는 의미는 아닙니다. 최초 Ghidra는 95,824개 함수를 식별했으며 두 leaf target을 명시 생성한 뒤 테이블 root 및 직접 callee 72개 디컴파일 결과가 생성됐습니다. [해시·RVA 근거](native/decompilation-evidence.json)의 clang/clang-interface/clang-methods-complete 그룹을 확인할 수 있습니다.

`0x4CBD0`에는 객체 offset 8/9의 상태 검사·offset 0x10의 객체 생성/저장이, `0x4CC90`에는 그 상태/객체 정리 분기가 있습니다. `0x52430`은 출력에 0x8BA18AFC00031512와 0을 쓰며, `0x52450`은 3,1,22,1,2를 씁니다. 이 숫자들의 API/LLVM 버전 의미는 호출자 검증 전 **미확인**입니다. 전체 자동 분석 로그에는 잘못된 주소/흐름 경고가 있으므로 디컴파일 성공을 의미 정확성으로 바꾸지 않습니다.

fixture의 `.uproject`는 CodeAssist 프로젝트 발견 경로를 검증하는 표식입니다. 엔진으로 빌드한 프로젝트라는 증거로 사용하지 않습니다.

분석 대상 폴더:

`C:\Users\ehdwn\AppData\Local\Microsoft\VisualStudio\17.0_ca39936f\Extensions\zgy5sun1.aim`

설치 EXE: `C:\Users\ehdwn\Downloads\VA_X_Setup2630_0.exe`.
설치 EXE와 `VA_X64.dll`의 파일 버전은 `10.9.2630.0`, Authenticode 서명 상태는 `Valid`, 서명자는 Whole Tomato Software LLC였습니다. 설치 매니페스트의 대상은 Visual Studio Community `[17.0,18.0)`, `amd64`입니다.

## 재현과 원자료

PowerShell에서 다음 명령으로 다시 수집할 수 있습니다. .NET 8 이상 SDK와 Visual Studio C++ 도구가 필요합니다. 외부 NuGet 라이브러리를 추가하지 않습니다.

```powershell
./tools/VAStaticAnalysis/Collect.ps1 `
  -ExtensionFolder 'C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936f/Extensions/zgy5sun1.aim' `
  -DumpbinPath 'C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe'
```

- `inventory.json`: 86개 DLL/EXE의 버전·크기·서명 상태.
- `managed-metadata.json`: 86개 파일의 SHA-256·PE 아키텍처·관리형 여부. VA 이름의 파일에서 관심 타입·메서드·호출·P/Invoke를 추출합니다.
- `focused-evidence.json`: 아래 분석에 사용한 타입의 메서드, RVA, 매개변수 이름, 호출 및 일부 전체 IL 명령.
- `registration.json`: pkgdef의 패키지·서비스·배경 로드 등록 발췌. 실제 사용자 레지스트리를 덤프한 자료가 아닙니다.
- `*.pe.txt`: 네이티브 4개 DLL의 `dumpbin /headers /imports /exports` 결과.

분석기는 IL 메서드만 해독하며 혼합 모듈의 네이티브 메서드 본문은 읽지 않습니다. 일반 피연산자는 원시 little-endian 16진수, 분기는 IL 대상 오프셋으로 기록합니다. `TypeSpecification`은 제네릭 타입 이름을 해독하지 않은 자리표시자입니다. 메서드 오버로드는 이름만으로 구별하지 말고 RVA·매개변수도 확인해야 합니다. call/ldftn 목록은 호출 관계 증거이며 조건·실행 빈도를 증명하지 않습니다.

## 확인된 구조

| 관찰 | 직접 근거 | 해석의 한계 |
|---|---|---|
| 핵심 `VA_X64.dll`은 AMD64 네이티브 DLL | PE machine `8664`, COM Descriptor RVA/size 0 | 내부 파서 알고리즘은 미확인 |
| 관리형 MEF 구성 요소와 네이티브 패키지가 함께 등록됨 | 매니페스트, `registration.json`의 VaMefAsync 및 VaPkgAsync64 | 각 기능의 소유 경계 전체는 미확인 |
| 코어는 `vaIPC64.dll`을 import함 | `VA_X64.dll.pe.txt` import table | 모든 인덱싱이 별도 프로세스라는 뜻은 아님 |
| IPC API에 LaunchServer/RunServer/MakeInstanceRequest 등이 있음 | `vaIPC64.dll.pe.txt` export table | 요청 포맷과 실제 실행 대상은 미확인 |
| `valibclang2_64.dll`에는 VAClang 빌드 PDB 경로가 남아 있음 | 해당 PE의 RSDS debug 항목 | 일반 자동완성이 Clang 기반이라고 단정할 수 없음 |
| 코드 검사 서버 진입점이 IPC 등록 메서드를 호출함 | `VaCodeInspectionsServer64.exe`, `Server.Program.Main` → `IPC.CIServerRegistration.main` | Main 이후의 네이티브 동작은 미확인 |

## 자동완성에서 복원한 동작

`VaMefAsync.17.dll`의 `VaCompletionUtils.CompletionListFromViewModel`은 비동기 목록의 `get_Items`와 `AttachToSession`을 호출합니다. `CompletionListFromCompletionSet`은 기존 `CompletionSet.get_Completions`와 `AttachToSession`을 호출합니다. `CompletionList<T>.AttachToSession`의 두 오버로드는 각각 기존·비동기 세션의 `Dismissed` 이벤트를 구독합니다. **두 세션 API를 다루는 코드가 존재한다는 사실**까지 확인됐으며, 이 설치 환경에서 어느 경로가 선택되는지는 미확인입니다.

`VaCompletionUtils.ValidateItemIndex`는 매개변수 이름과 전체 IL 분기를 복원했습니다. 다음은 해당 함수의 동등한 제어 흐름입니다. `ItemsMatch`와 `FindItem`의 내부 의미 및 enum 값 4의 이름은 복원하지 않았습니다.

```text
ValidateItemIndex(selectedText, index, count, getDisplayText):
    if index >= count:
        return FindItem(selectedText, count, getDisplayText, 4)
    if index < 0:
        return index
    if ItemsMatch(getDisplayText(index), selectedText, true, 4):
        return index
    return FindItem(selectedText, count, getDisplayText, 4)
```

근거: RVA `19292`, `IL_0002 bge.s IL_0019`, `IL_0006 blt.s IL_0024`, `IL_0012 ItemsMatch`, `IL_0017 brtrue.s IL_0024`, `IL_001D FindItem`, `IL_0025 ret`.

따라서 **선택 인덱스의 항목과 선택 문자열이 맞는지 확인하고, 불일치하면 다시 찾는 동작**을 확인했습니다. 음수 인덱스는 그대로 반환합니다. 이 메서드가 특정 CodeAssist 문제를 해결한다고 주장하지 않습니다. `Verify-ControlFlow.ps1`은 이 분기 모델의 4개 경로를 스텁으로 확인하며 실제 VA 메서드 실행 테스트가 아닙니다.

## 코드 검사에서 확인한 호출

`VaCodeInspectionsAsync.17.dll`의 `BufferListener.OnTimerTick`에는 UI 스레드 검사 → Timer.Stop → Timer.set_Enabled → RebuildFileParser → QueueParseTask 호출이 순서대로 존재합니다. 전체 분기는 원자료에 보존했습니다. 모든 호출이 매번 실행되는지는 조건별 추가 해독이 필요합니다.

`BufferListener.Dispose`에는 Timer.Stop, CancelMyPendingParses, CancellationCookie.Cancel, QueueDisposeTask 호출이 있습니다. `ProjectItemInspector`에는 프로젝트·파일 구성 조회, 평가된 속성 조회, include 경로 정리 호출이 있습니다. **지연 작업·취소·컴파일 구성 수집을 담당하는 코드**까지 확인됐습니다. 타이머 간격, 취소의 완료 보장, 반환 결과의 버전 검증은 미확인입니다.

## CodeAssist 적용 후보와 우선순위

본연: Unreal C++에 즉시 반응하는 무료·경량 보조 확장. 작은 프로젝트 캐시와 엔진 공용 캐시, IntelliSense로 넘기는 흐름을 유지합니다. 아래 항목은 개발 제안이며 VA와의 동등성 또는 성능이 검증된 구현은 아닙니다.

| 우선순위 | 적용 후보 | 현재 CodeAssist 근거 | 구현 후 검증 조건 |
|---|---|---|---|
| P1 | 문서 추적의 종료·취소·버전 관리 | `LiveDocumentTracker`는 150ms Timer, Changed 구독을 보유하지만 이 클래스에 Dispose/구독 해제가 없음 | 문서 닫기 후 작업·이벤트 정리, 연속 수정에서 이전 분석이 최신 상태를 덮어쓰지 않는지 검사 |
| P1 | 갱신된 자동완성 목록에서 선택 항목의 식별자 재검증 | `CodeAssistCompletionSource`, `CompletionCommandFilter`가 목록 생성·갱신·확정을 담당 | 후보 재정렬·삭제 직후 Tab/Enter가 사용자가 선택한 심볼을 확정하는지 재현. 실제 결함은 아직 미확인 |
| P1 | 참조 후보 검색의 어휘·범위 정확도 개선 | `PersistentSymbolIndex.FindReferences`는 줄 주석 제거 후 이름 정규식 검색 | 문자열·블록 주석 제외, 같은 이름의 다른 지역 변수 구분. 정밀하지 않은 결과는 계속 후보로 표시 |
| P2 | 심볼 키에 범위·파일·시그니처를 포함 | `IndexedSymbol`은 이름·소유형·형·기본형·위치를 보유. 소유형 조회 실패 시 전역 이름 조회로 fallback | 같은 이름의 클래스/지역 변수/오버로드에서 모호성을 유지하고 임의 타입을 확정하지 않는지 검사 |
| P2 | 프로젝트 컴파일 구성의 명시적 모델 | VA 검사 측의 ProjectItemInspector 호출과 CodeAssist의 `.Build.cs` 기반 모듈 탐색 | UE include/define/타깃 차이를 반영하면서 초기 응답 및 공용 엔진 캐시를 유지하는지 측정 |
| P3 | 무거운 정밀 분석의 선택적 작업자 분리 | VA IPC와 검사 서버 구조 | 먼저 CPU·메모리·UI 지연 측정. 현재 구조에서 이득이 입증될 때 도입 |

P1 → P2 순서로 독립 구현하는 것을 제안합니다. 코드 검사 서버 전체나 Clang DLL을 붙이는 대규모 변경은 현재 근거만으로 정당화하지 않습니다. 이번 변경은 분석 도구와 기록이며 CodeAssist 제품 코드는 변경하지 않았습니다.

## 후속 분석에 필요한 작업

1. 같은 작은 Unreal C++ 입력으로 VA와 CodeAssist의 후보·정의·참조 결과와 응답 시간을 기록합니다. 아직 실행하지 않았습니다.
2. 네이티브 코어는 디스어셈블러의 함수 경계·교차 참조를 통해 GetVaService/VaConnectSetup에서 기능 경계를 추적해야 합니다. 현재 import/export 분석만 완료했습니다.
3. 캐시는 파일 I/O 관찰과 생성 전후 샘플로 포맷·무효화 조건을 확인해야 합니다. 현재 캐시 구조는 복원하지 않았습니다.
4. 일반 자동완성과 코드 검사 경로를 별도로 추적합니다. 검사 측 Clang 흔적을 일반 자동완성의 근거로 사용하지 않습니다.

## 참고한 공식 자료

- [Microsoft PE/COFF 형식](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format): 헤더·데이터 디렉터리·import/export 해석 기준.
- [Microsoft PEReader](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.portableexecutable.pereader): PE·메타데이터·메서드 본문 읽기 API.
- [Microsoft MetadataReader](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.metadatareader): 타입·메서드 메타데이터 읽기 API.

제품 설명보다 로컬 설치 파일에서 추출한 증거를 판단의 근거로 사용했습니다.
