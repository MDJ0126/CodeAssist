# CodeAssist 개발용 XERJ

XERJ는 이 저장소를 분석하는 개발 도구입니다. 제품의 검색 엔진에는 통합하지 않았습니다.

- 설치: `.local/xerj/xerj.exe`, 공식 Windows x64 `v1.0.0-rc.95`.
- 릴리스 ZIP SHA-256: `23648A3B2E3965B60C4939876A1F3D43F1B9A609B61B3D406E752B8336220385` (공식 checksum과 대조).
- 서버: `127.0.0.1:9320`, 추가 리스너 9321/9322, 로컬 개발용 인증/TLS 해제.
- 기본 lexical embedder를 사용합니다. 신경망 의미 검색 성능은 검증하지 않았습니다.
- 인덱스: `codeassist-*`, 상태 폴더 `%LOCALAPPDATA%/CodeAssist/Xerj/state`. 감시 대상 내부의 상태 폴더는 XERJ가 거부하므로 외부에 저장합니다.
- `.gitignore` 및 `.xerjignore`를 따릅니다. 빌드 결과, VSIX, 로컬 데이터, REA 실행 출력, JSON은 제외합니다. JSON은 스키마 변화가 감시를 실패시켜 직접 읽습니다. 소스·Markdown 문서는 검색 대상으로 유지합니다.
- 변경·추가·삭제 반영을 위해 `--no-graph` 사용. 그래프 기반 관계 검색은 사용하지 않습니다.

```powershell
pwsh -NoProfile -File tools/Xerj.ps1 Index
pwsh -NoProfile -File tools/Xerj.ps1 Watch
pwsh -NoProfile -File tools/Xerj.ps1 Search 'clipboard history'
pwsh -NoProfile -File tools/Xerj.ps1 Def PersistentSymbolIndex
pwsh -NoProfile -File tools/Xerj.ps1 Status
```

프로젝트 `.codex/config.toml`의 `xerj_codeassist`는 `Mcp` 액션을 실행합니다. 서버가 없으면 실행 스크립트가 시작합니다. Codex가 현재 세션에 MCP 도구를 다시 불러왔는지는 별도 확인이 필요합니다. 프로젝트를 신뢰한 상태에서 새 세션을 열어 연결을 확인하십시오.

MCP 실행 시 서버와 백그라운드 감시 프로세스를 시작합니다. 수동 `Watch`는 포그라운드로 실행되며 종료는 Ctrl+C입니다. 실행 중인 서버와 감시 프로세스 ID는 `.local/xerj/server.pid`, `watch.pid`에 기록합니다.

검증: 초기 177파일/3,236레코드/코드 95파일, junk 0. JSON 제외 후 generation 3은 149파일/1,198레코드로 성공했습니다. `PersistentSymbolIndex.cs:14`, `ClipboardHistory.cs:6` 정의와 원문을 대조했습니다. Codex 등록 실행 스크립트를 통한 MCP 초기화·13개 도구 목록·검색 호출이 성공했습니다. 임시 C# 파일을 재인덱싱하여 변경 전 심볼 0건, 변경 후 심볼 2건, 삭제 후 0건을 확인했습니다. 결과는 `validation/xerj-integration.json`에 있습니다.

감시 모드 제한: 새로운 JSON 필드 구조의 추가는 `unsupported dataset/schema evolution`으로 실패했습니다. 검증 중 다른 작업에서 생성한 JSON에도 같은 오류가 발생하여 JSON 전체를 검색 대상에서 제외했습니다. 다른 형식에서도 이 오류가 발생하면 새로운 prefix와 state-dir로 전체 재구축이 필요합니다. 모든 파일 형식의 추가를 자동 반영한다고 보장하지 않습니다.

XERJ 검색은 후보 탐색입니다. 정의·참조·호출 경로는 현재 원문과 대조합니다. `rg` 대비 속도·토큰 절감, 모든 C# 심볼 정확도, 신경망 검색 품질은 미확인입니다.

공식 자료: https://github.com/xerj-org/xerj / https://xerj.org/docs/cli / https://developers.openai.com/codex/mcp
