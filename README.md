<p align="center">
  <img src="docs/assets/codeassist-logo.png" alt="CodeAssist 노란색 바나나 로고" width="64">
</p>
<h1 align="center">CodeAssist</h1>
<p align="center"><strong>코드를 읽고, 심볼을 찾고, 다음 코드를 완성하세요.</strong></p>
<p align="center">
  <a href="https://github.com/MDJ0126/CodeAssist/raw/refs/heads/main/installers/0.0.95/CodeAssist.vsix">다운로드</a> &middot;
  <a href="docs/index.html"><strong>사용 안내</strong></a> &middot;
  <a href="docs/feature-details.md">상세 기능</a> &middot;
  <a href="LICENSE">MIT License</a>
</p>

---

**Visual Studio에서 코드의 의미를 확인하고, 필요한 파일과 심볼로 이동하세요.**

CodeAssist는 Visual Studio 2019·2022·2026을 대상으로 제공하는 오픈소스 개발 보조 확장입니다. 현재 **C++와 Unreal Engine**을 대상으로 의미 색상, 자동완성 후보, 파일·심볼 검색과 정의·참조 탐색을 구현하고 있습니다.

<p align="center">
  <img src="docs/assets/codeassist-overview.png" alt="CodeAssist 작동 구조: 프로젝트 코드를 심볼 인덱스와 프로젝트·엔진 캐시에 저장하고 Visual Studio의 의미 색상, 자동완성 후보, 정의·참조 탐색에 연결합니다. IntelliSense는 병행하여 타입 분석과 진단을 처리합니다." width="100%">
</p>

설명 이미지는 작동 구조를 표현한 도식이며 실제 Visual Studio 화면이 아닙니다.

## 설치 후에는 어떻게 사용하나요?

1. Visual Studio에서 프로젝트를 열고 코드를 편집합니다.
2. CodeAssist가 프로젝트 심볼을 분석하고 색상과 자동완성 후보를 편집기에 연결합니다.
3. 단축키로 파일·심볼을 검색하거나 정의·참조 후보로 이동합니다.

프로젝트별 심볼 캐시와 Unreal Engine 설치별 공용 캐시를 저장합니다. 같은 엔진을 사용하는 다른 프로젝트에서는 엔진 캐시를 재사용합니다.

## 어떤 기능이 있나요?

| 기능 | 동작 |
| --- | --- |
| 의미 색상 | 클래스·타입·변수·함수와 Unreal 리플렉션 매크로를 구분해 표시합니다. |
| 자동완성 후보 | 저장된 심볼 인덱스에서 입력 접두사, 소유 타입과 상속 관계에 맞는 후보를 조회합니다. |
| 파일·심볼 검색 | 파일, 심볼과 현재 파일의 메서드를 검색합니다. |
| 정의·참조 탐색 | 정의와 참조 후보를 찾고, 대응하는 헤더·소스로 이동합니다. |
| Unreal 문맥 | 리플렉션 지정자와 include 경로에 맞는 후보를 추천합니다. |

세부 동작과 제한은 [상세 기능](docs/feature-details.md), [사용 안내](docs/index.html)와 [검증 기록](docs/validation/index.html)에서 확인하실 수 있습니다. HTML 안내는 저장소를 내려받은 뒤 브라우저에서 열어 주세요.

## 설치

버전에 맞는 **0.0.95** 설치 파일을 선택해 주세요.

| Visual Studio | 설치 파일 | 아키텍처 | 실제 IDE 실행 검증 |
| --- | --- | --- | --- |
| 2019 (16.x) | [CodeAssist.VS2019.vsix](https://github.com/MDJ0126/CodeAssist/raw/refs/heads/main/installers/0.0.95/CodeAssist.VS2019.vsix) | x86 | 빌드·패키지 확인, 실제 설치·실행 미검증 |
| 2022 (17.x) | [CodeAssist.vsix](https://github.com/MDJ0126/CodeAssist/raw/refs/heads/main/installers/0.0.95/CodeAssist.vsix) | x64 | 17.14에서 실행 검증 기록 있음 |
| 2026 (18.x) | [CodeAssist.vsix](https://github.com/MDJ0126/CodeAssist/raw/refs/heads/main/installers/0.0.95/CodeAssist.vsix) | x64 | 설치 대상 범위 포함, 실제 설치·실행 미검증 |

VS2019는 별도 SDK로 빌드한 전용 패키지를 사용합니다. ARM64는 현재 설치 대상에 포함하지 않습니다. 위 검증 기록은 모든 기능이나 0.0.95 패키지 전체의 검증 완료를 의미하지 않습니다.

1. 사용 중인 Visual Studio 버전에 맞는 VSIX를 다운로드합니다.
2. Visual Studio를 모두 종료합니다.
3. VSIX를 실행하고 해당 Visual Studio에 설치합니다.
4. Visual Studio를 다시 실행하고 프로젝트를 엽니다.

**무료로 사용할 수 있으며, 별도 구독 없이 MIT 라이선스로 배포합니다.**

## 자주 사용하는 단축키

| 단축키 | 기능 |
| --- | --- |
| `Alt+G` | 빠른 정의 탐색 |
| `F12` | CodeAssist 정의 탐색 후, 찾지 못하면 Visual Studio 기본 기능 실행 |
| `Shift+F12` | 참조 후보 검색 후, 찾지 못하면 Visual Studio 기본 참조 찾기 실행 |
| `Ctrl+Space` | 자동완성 후보 수동 호출 |

추가 탐색 단축키와 동작 범위는 [단축키 안내](docs/keyboard-shortcuts.html)를 참고해 주세요.

## Visual Studio IntelliSense와 함께 작동합니다

CodeAssist는 편집 중인 코드와 저장된 심볼 인덱스에서 표시·탐색·자동완성 후보를 먼저 구성합니다. Visual Studio IntelliSense는 병행하여 완전한 C++ 타입 추론, 템플릿·조건부 컴파일 분석과 정밀 진단을 처리합니다.

참조 검색과 자동완성 결과는 후보이며, 완전한 C++ 의미 분석과 같은 범위로 검증된 것은 아닙니다.

## 개발

요구 사항은 Visual Studio 2022, Visual Studio 확장 개발 및 C++ 데스크톱 개발 워크로드, .NET 8 SDK입니다.

`CodeAssist.sln`을 열고 `CodeAssist.Extension`을 시작 프로젝트로 설정한 뒤 `F5`를 누르면 실험용 Visual Studio에서 테스트할 수 있습니다. Release 빌드 시 `installers/<매니페스트 버전>/CodeAssist.vsix`가 갱신됩니다. VS2019 패키지도 같은 버전 폴더에 보관하며 저장소 루트에는 설치 파일을 생성하지 않습니다.

Visual Assist 10.9.2630.0의 분석 근거와 미확인 사항은 [분석 기록](docs/reverse-engineering/va-2630/index.html)에 있습니다. 일부 기능 구현을 Visual Assist 전체와의 동등성 검증으로 간주하지 않습니다.

## 라이선스

[MIT License](LICENSE)

CodeAssist는 독립 프로젝트이며 Microsoft 또는 Epic Games와 제휴하거나 공식 승인을 받은 제품이 아닙니다.
