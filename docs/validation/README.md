# 검증 결과

검증일: 2026-10-14 (Asia/Seoul).

## 자동 검증

- Visual Studio 2022 MSBuild Release 빌드: Core, Indexing, Tests, Worker, Extension 및 VSIX 생성 성공.
- xUnit: **73개 통과, 실패 0개, 건너뜀 0개**. 주석 태그·include·선택 복원·클립보드·TPL·스니펫 날짜/시간에 더해 LSP UTF-8 프레이밍, 잘못된 응답 범위, UTF-16 이름 변경 edit 및 겹침 거부를 검증했습니다.
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
& 'C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe' UEAssist.sln /restore /t:Rebuild /p:Configuration=Release /v:minimal /nologo
dotnet vstest UEAssist.Core.Tests/bin/Release/net8.0/UEAssist.Core.Tests.dll
dotnet run --project tools/IndexBenchmark -c Release -- UEAssist.Worker/bin/Release/UEAssist.Worker.exe docs/validation/index-benchmark.json
./tools/VerifyPackage.ps1
```

중간 VSIX 매니페스트에 이전 버전이 남아 있는 사례를 실제로 확인해 Rebuild 후 패키지 안의 버전을 검증했습니다. 릴리스 시 소스 매니페스트만 확인하지 말고 위 패키지 검증을 수행합니다.

## Visual Studio에서 남은 확인

별도 `UEAssistVerify` 프로필에서 실제 메뉴 29개와 인덱스 준비 상태를 확인했습니다. UEAssist 파일 검색에 `\ui-fixture main.cpp:6`을 입력한 뒤 Enter로 `main.cpp` 6행 1열에 이동했습니다. [UEAssist 실행 기록](ueassist-runtime.json). 명시 재분석 명령을 추가하기 전의 실행 결과이며, 나머지 명령과 모든 키 라우팅의 검증을 대신하지 않습니다.

VA 원본은 비교 솔루션에서 실제 실행했습니다. 파일 검색 결과 10개와 행 이동 결과를 [실행 기록](../reverse-engineering/va-2630/runtime/file-search.json)에 저장했습니다. 이 결과를 UEAssist 확장 UI 실행 검증으로 대신하지 않습니다.

현재 VS 프로필에서 `Alt+↓`는 VA ScopeNext 대신 VS 기본 선택 행 이동을 실행했습니다. `Ctrl+Z`로 비교 파일을 복구하고 변경 표시가 사라진 것을 확인했습니다. [단축키 충돌 기록](../reverse-engineering/va-2630/runtime/key-conflicts.json). 전체 통합을 허용해도 기존 사용자 키 배치와의 충돌이 모두 해소되는 것은 아닙니다.

아래는 아직 실행하지 않은 확인 항목입니다. 빌드와 Core 테스트 통과를 실사용 결과로 대신하지 않습니다.

1. 문서를 두 뷰로 연 뒤 한 뷰만 닫았을 때 추적을 유지하고, 마지막 뷰를 닫으면 추적·예약 작업을 정리하는지 확인합니다.
2. 후보 선택 후 연속 입력·Backspace로 목록이 바뀔 때 Tab/Enter 확정 항목을 확인합니다.
3. 키보드 사용자 설정 또는 함께 설치된 VA가 키를 선점하는지 확인합니다.
4. 파일·심볼 창의 검색, 위/아래·Enter·Esc, 대응 파일과 메서드 이동을 확인합니다.
5. Rename·Quick Actions를 지원하는 C++ 문맥과 지원하지 않는 문맥에서 호출·가용성 안내를 확인합니다.
6. 실제 Unreal 프로젝트에서 엔진 공용 캐시 재사용, 첫 v5 재생성, 상태 표시줄 진행 및 메모리·입력 지연을 측정합니다.
