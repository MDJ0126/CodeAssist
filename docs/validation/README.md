# 검증 결과

검증일: 2026-10-14 (Asia/Seoul).

## 자동 검증

- Visual Studio 2022 MSBuild Release 빌드: Core, Indexing, Tests, Worker, Extension 및 VSIX 생성 성공.
- xUnit: **85개 통과, 실패 0개, 건너뜀 0개**. 주석 태그·include·선택 복원·클립보드·TPL·스니펫 날짜/시간에 더해 LSP UTF-8 프레이밍, 잘못된 응답 범위, UTF-16 이름 변경 edit 및 겹침 거부를 검증했습니다.
- 최종 VSIX: 0.0.49 매니페스트, JSON 런타임을 포함한 Release 바이너리 6개 해시 및 VSCT 단축키 등록 24개를 패키지 도구로 검증합니다. 실제 최신 결과는 [패키지 검증 기록](package.json)에 보존합니다.
- 실제 clangd 19.1.5: 오버로드 정의 분리·상속 멤버 완성·의미 참조·세 파일 이름 변경 계획과 주석/문자열 보존. [원자료](clangd-fixture.json). 닫힌 선언 헤더가 빠졌던 첫 검증 실패도 확인했으며, 열린 문서 및 직접 include의 동기화를 추가했습니다. 전체 UE 코드베이스 범위의 검증을 대신하지 않습니다.
- 실제 별도 worker: 120개 헤더·2,400개 클래스·19,200개 함수의 합성 입력을 3회 분석. 매회 내부 처리와 심볼 identity 21,600개 및 파일 목록이 일치했습니다.
- 캐시 v5의 scope·시그니처·파일 목록 roundtrip, 오래된 결과 게시 차단, 지역 shadowing, 어휘 마스킹, 타입 모호성, 구성 fingerprint, 작업자 실패 fallback을 회귀 테스트로 확인했습니다.

제한된 실행 환경에서는 testhost 연결이 90초 뒤 시간 초과되었습니다. 제한 밖에서 같은 테스트를 실행해 실제 결과를 확인했습니다. VS 편집기 뷰·키 입력은 이 테스트에 포함되지 않습니다.

## 작업자 측정

| 실행 | 내부 처리 | 별도 작업자 |
|---|---:|---:|
| 1 | 337ms | 980ms |
| 2 | 289ms | 735ms |
| 3 | 264ms | 662ms |

중앙값은 내부 289ms, 작업자 735ms입니다. 작업자 시간에는 프로세스 시작, 분석, 캐시 저장·읽기 및 조회 테이블 생성이 포함됩니다. VA와 비교한 결과나 Unreal 대형 프로젝트의 UI 성능 측정은 아닙니다. 이 입력에서는 작업자 비용이 커 기본값을 내부 처리로 유지했습니다. 원자료: [index-benchmark.json](index-benchmark.json).

재현:

```powershell
& 'C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe' CodeAssist.sln /restore /t:Rebuild /p:Configuration=Release /v:minimal /nologo
dotnet vstest CodeAssist.Core.Tests/bin/Release/net8.0/CodeAssist.Core.Tests.dll
dotnet run --project tools/IndexBenchmark -c Release -- CodeAssist.Worker/bin/Release/CodeAssist.Worker.exe docs/validation/index-benchmark.json
./tools/VerifyPackage.ps1
```

중간 VSIX 매니페스트에 이전 버전이 남아 있는 사례를 실제로 확인해 Rebuild 후 패키지 안의 버전을 검증했습니다. 릴리스 시 소스 매니페스트만 확인하지 말고 위 패키지 검증을 수행합니다.

## Visual Studio에서 남은 확인

별도 `CodeAssistVerify` 프로필에서 실제 메뉴 29개와 인덱스 준비 상태를 확인했습니다. CodeAssist 파일 검색에 `\ui-fixture main.cpp:6`을 입력한 뒤 Enter로 `main.cpp` 6행 1열에 이동했습니다. [CodeAssist 실행 기록](codeassist-runtime.json). 명시 재분석 명령을 추가하기 전의 실행 결과이며, 나머지 명령과 모든 키 라우팅의 검증을 대신하지 않습니다.

VA 원본은 비교 솔루션에서 실제 실행했습니다. 파일 검색 결과 10개와 행 이동 결과를 [실행 기록](../reverse-engineering/va-2630/runtime/file-search.json)에 저장했습니다. 이 결과를 CodeAssist 확장 UI 실행 검증으로 대신하지 않습니다.

현재 VS 프로필에서 `Alt+↓`는 VA ScopeNext 대신 VS 기본 선택 행 이동을 실행했습니다. `Ctrl+Z`로 비교 파일을 복구하고 변경 표시가 사라진 것을 확인했습니다. [단축키 충돌 기록](../reverse-engineering/va-2630/runtime/key-conflicts.json). 전체 통합을 허용해도 기존 사용자 키 배치와의 충돌이 모두 해소되는 것은 아닙니다.

아래는 아직 실행하지 않은 확인 항목입니다. 빌드와 Core 테스트 통과를 실사용 결과로 대신하지 않습니다.

1. 문서를 두 뷰로 연 뒤 한 뷰만 닫았을 때 추적을 유지하고, 마지막 뷰를 닫으면 추적·예약 작업을 정리하는지 확인합니다.
2. 후보 선택 후 연속 입력·Backspace로 목록이 바뀔 때 Tab/Enter 확정 항목을 확인합니다.
3. 키보드 사용자 설정 또는 함께 설치된 VA가 키를 선점하는지 확인합니다.
4. 파일·심볼 창의 검색, 위/아래·Enter·Esc, 대응 파일과 메서드 이동을 확인합니다.
5. Rename·Quick Actions를 지원하는 C++ 문맥과 지원하지 않는 문맥에서 호출·가용성 안내를 확인합니다.
6. 실제 Unreal 프로젝트에서 엔진 공용 캐시 재사용, 첫 v5 재생성, 상태 표시줄 진행 및 메모리·입력 지연을 측정합니다.

## REA 연결 검증 — 2026-10-15

공식 `morluto/rea` 저장소의 역분석 스킬과 npm `rea-agents` 6.5.0을 설치했습니다. 기존 Ghidra 12.1.4 및 JDK 21을 재사용하며, 프로젝트 내부 Node 패키지 경로로 Codex stdio MCP를 등록했습니다. 기존 Codex 설정은 setup이 백업했습니다.

등록 설정 그대로 MCP 서버를 실행해 initialize 및 tools/list에 성공했고 도구 207개를 확인했습니다. [실제 MCP 응답 목록](rea/mcp-smoke.json). 현재 대화의 도구 목록에 자동 추가된 것은 아니며, 앱 재연결 후 세션 노출은 별도 확인합니다. CLI에서도 같은 REA 패키지를 호출할 수 있습니다.

REA가 설치된 `VaOptKeyBinds.17.dll`을 읽고 정적 관리 어셈블리 분석 Evidence를 반환했습니다. [원자료](rea/va-keybindings-evidence.json). 이는 대상 실행·C# 복원·네이티브 디컴파일 완료를 의미하지 않습니다. 처음의 잘못된 DLL 경로 호출은 실패했고, 실제 파일명을 확인하여 재호출했습니다. Ghidra 제공자 점검 통과도 VA 전체 네이티브 분석 완료와 구분합니다.

다음 순서는 UnrealTestProject에서 CPU 비용의 실제 측정, 시작 시 캐시 재분석 경로의 확인, 그리고 단축키별 원본 동작 비교입니다. CPU 100%의 원인은 아직 확정하지 않았습니다.

## 성능 수정·명령 대응표·패키지 검증 — 2026-10-15

실제 `C:\Projects\UnrealTestProject`의 EngineAssociation은 5.5입니다. 설치된 UE 5.5를 사용해 프로젝트 입력 4개·선택 엔진 입력 3,962개·컴파일 구성 18개를 확인했습니다. [처리 단계 및 CPU 시간 원자료](unreal-startup.json). 마지막 측정에서 프로젝트 분석 53ms, 프로젝트 캐시 읽기 3ms, 선택 엔진 입력 열거 1,448ms, 입력 fingerprint 166ms, 같은 입력 재확인 1,539ms, 구성된 헤더 후보 생성 37ms였습니다. 반복 실행의 디스크 캐시 영향을 포함하며 이 수치를 수정 전후 전체 VS 시작 속도 개선률로 해석하지 않습니다.

코드상 엔진 캐시 존재와 무관하게 실행되던 BuildEngine·Save를 수정했습니다. 엔진 버전별 캐시가 로드되고 해당 프로젝트에서 사용하는 입력 경로·크기·수정 시각이 같으면 재분석·저장을 건너뜁니다. 입력 추가·삭제·변경 및 무시 폴더 처리의 회귀 테스트를 통과했습니다. 새 입력 기록이 없는 최초 실행에서는 한 번 분석합니다. 수정 시각과 크기가 함께 보존된 내용 변경은 이 메타데이터 검사로 감지하지 못합니다.

include root 정규화도 파일/타깃마다 반복하던 구조에서 한 번 계산한 고유 경로 목록으로 변경했습니다. 기능을 비활성화하는 수정은 하지 않았습니다. 실행 중인 VS의 프로파일·첫 엔진 분석·캐시 읽기 비용·입력 지연과 사용자가 관찰한 CPU 100%의 실제 원인 확정은 아직 남아 있습니다. 헤드리스 측정을 실제 VS UI 측정으로 대신하지 않습니다.

[현재 명령 대응표](command-coverage.json)에 30개 메뉴 명령·24개 키 등록과 제한을 보존했습니다. REA의 [VA 단축키 멤버 및 CIL Evidence](rea/va-keybinding-members.json)도 추가했습니다. 현재 75개 테스트가 통과하며 패키지 0.0.49의 바이너리 6개·키 등록 24개가 Release 빌드와 일치합니다. VA 전용 리팩터링·코드 생성 및 모든 단축키의 원본 UI 비교는 완료되지 않았습니다.
