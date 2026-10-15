# 단축키

Visual Assist 공식 [단축키 안내](https://www.wholetomato.com/documentation/configuration/keyboard-shortcuts)의 키 조합을 대응하는 CodeAssist 명령에 등록했습니다. 해당 안내는 기본 및 권장 키를 함께 설명하며, 2630 설치본에서 사용자가 실제로 할당한 키는 별도로 확인하지 않았습니다.

| 키 | CodeAssist 동작 | 구현 범위 |
|---|---|---|
| Alt+G / F12 | 정의 후보 탐색 | 독립 구현, 복수 후보 선택, VS fallback |
| Shift+Alt+F / Shift+F12 | 참조 후보 검색 | 어휘·일반 지역 블록 기준 |
| Shift+Alt+O | 파일 검색 | 인덱싱 파일, 이름·경로 포함/제외·OR·앵커·행 지정 검색 |
| Shift+Alt+S | 심볼 검색 | 클래스·함수·멤버 후보 |
| Alt+O | 대응 파일 열기 | 같은 기본 이름의 파일 |
| Alt+M | 현재 파일 메서드 목록 | 저장하지 않은 문서 포함 |
| Shift+Alt+G | 관련 심볼 후보 | 이름·소유형·상속·값/반환형 관계 |
| Alt+Left / Alt+Right | 이전·다음 탐색 위치 | Visual Studio 탐색 기록 호출 |
| Alt+Down / Alt+Up | 다음·이전 범위 | 주석·문자열을 제외한 중괄호·괄호 경계, 최상위 블록 이동 |
| Ctrl+0 | 편집기 줌 100% | 현재 WPF 편집기 뷰 |
| Shift+Alt+H | Hashtags | 프로젝트 주석 태그 검색 |
| Shift+Alt+] / Shift+Alt+[ | 선택 확장·축소 | 어휘·괄호·줄·블록과 선택 복원 기록 |
| Alt+] / Alt+[ | 블록 선택 확장·축소 | 중괄호 블록과 복원 기록 |
| Ctrl+Shift+V | 다중 클립보드 | 확장 실행 중 C++ 선택 복사·잘라내기 기록 |
| Alt+K | 현재 단어 강조 | F3·Shift+F3 이동, Esc 해제 |
| Shift+Alt+R | 이름 변경 | 구성된 clangd 변경 계획·미리보기, 미구성 시 VS Rename |
| Shift+Alt+Q | 빠른 작업 | Visual Studio Quick Actions 호출 |
| Shift+Alt+C | 사용 위치의 생성 작업 | Visual Studio Quick Actions 메뉴 호출; 생성 항목은 VS 문맥에 따름 |
| Ctrl+Space | 자동완성 수동 호출 | 기존 VS 편집기 명령과 CodeAssist 후보 |
| Tab / Enter | 선택 후보 확정 | 선택 항목 재검증 |

동일한 키 조합이 VA의 모든 동작을 똑같이 재현한다는 뜻은 아닙니다. 특히 관련 심볼·범위 이동은 위 표의 범위를 지원하며, Rename 및 코드 생성에는 VA의 구현을 이식하지 않았습니다. 빠른 작업에 원하는 생성 항목이 없으면 CodeAssist가 임의로 코드를 작성하지 않습니다.

클립보드·Hashtags·선택 확장/축소·현재 단어 강조 키도 추가했습니다. 기본 조합은 [공식 키 목록](https://www.wholetomato.com/documentation/configuration/keyboard-shortcuts) 및 설치 도움말과 대조했습니다. 신규 명령의 CodeAssist UI 실행 동등성은 아직 미확인입니다.

## 키 충돌과 사용자 설정

VSCT에 기본 키를 등록했습니다. Visual Studio의 사용자 지정 할당이나 함께 설치한 Visual Assist가 우선하면 키가 CodeAssist 명령을 실행하지 않을 수 있습니다. 기존 사용자 키 설정을 프로그램에서 자동으로 덮어쓰지는 않습니다.

설치된 현재 VS에서 `Alt+Down`이 선택 행 이동을 실행하는 충돌을 직접 확인했습니다. VA 탐색 바 화살표로 확인한 [범위 이동 기록](reverse-engineering/va-2630/runtime/scope-navigation.json)과 [키 충돌 기록](reverse-engineering/va-2630/runtime/key-conflicts.json)을 구분해 보존했습니다. 괄호·중괄호 범위 설명은 [공식 Move Scope 문서](https://www.wholetomato.com/documentation/navigation/move-scope)와 함께 대조했습니다. namespace·class·전처리 조건을 포함한 모든 C++ 범위의 동등성은 미확인입니다.

`도구 → 옵션 → 환경 → 키보드`에서 `CodeAssist`로 명령을 찾아 표의 키를 지정해 주세요. 파일·심볼 검색은 Global, 편집기 동작은 Text Editor 범위로 지정하실 수 있습니다. 실제 IDE 키 동작은 [확인 목록](validation/README.md)에 남겼습니다.

## 등록 파일

명령 ID와 기본 키는 `CodeAssist.Extension/CodeAssistCommands.vsct`, 명령 실행은 `GoToSymbolCommand`, `FindReferencesCommand`, `NavigationCommands`, 후보 확정은 `CompletionCommandFilter`에 있습니다.

## 명령별 검증 상태

[기계 판독 명령 대응표](validation/command-coverage.json)에 현재 30개 명령과 24개 키 등록·제한을 보존합니다. 원본 명령 존재와 메뉴 등록은 전체 동등성의 근거가 아닙니다. Ctrl+Space·Tab·Enter는 VS 편집기 명령 경로를 사용하는 별도 항목입니다.
