# CodeAssist 문서

최신 화면: [CodeAssist Welcome·Options·검사 안내·Context Bar](interface.md).

이번 변경: [첫 실행·Welcome·상단 안내바·설정과 검증 범위](startup.md). 바나나 아이콘을 적용했습니다.

현재 버전: **0.0.55**. 범용 개발 보조 도구를 목표로 하며, 현재 C++와 Unreal에서 IntelliSense 로딩 중에도 빠르게 색상·자동완성·탐색 후보를 제공하는 무료 확장입니다.

브라우저에서 `docs/index.html`을 열어 이용하실 수 있습니다. 인터넷 연결이나 별도 서버 없이 사용할 수 있으며, 왼쪽 검색에서 문서 내용을 검색하고 테마를 전환할 수 있습니다.

- [작업 인계](handoff.md): 현재 검증 범위와 정확한 미완료 작업.
- [사용 안내](usage.md): 설치, 검색 창, 자동완성, 참조 후보.
- [단축키](keyboard-shortcuts.md): Visual Assist와 대응하는 키 조합 및 실제 동작 범위.
- [구조와 캐시](architecture.md): 문서 수명, 심볼 식별, 컴파일 구성, 별도 작업자.
- [분석 내용 적용 현황](analysis-integration.md): 역분석 근거별 구현과 한계.
- [검증 결과](validation/README.md): 빌드, 회귀 테스트, 작업자 실행 측정, IDE 확인 항목.
- [Visual Assist 정적 역분석 기록](reverse-engineering/va-2630/README.md): 원자료와 재현 도구.

CodeAssist는 독립 구현입니다. Visual Assist 바이너리를 제품에 포함하거나 런타임 의존성으로 사용하지 않습니다. 정밀 C++ 리팩터링은 Visual Studio의 기능을 호출합니다.

## 문서 갱신

편집 원본은 `.md`, 사용자용 문서는 `.html`입니다. Node.js와 `marked` 패키지가 있는 환경에서 `node tools/build-docs.cjs`로 HTML 문서를 다시 생성합니다. 스타일과 검색·테마 기능은 `docs/assets`에 있습니다.
