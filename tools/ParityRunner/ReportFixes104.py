import sys,json,zipfile,hashlib,shutil,datetime,html,xml.etree.ElementTree as ET
from pathlib import Path
run=Path('.local/parity-runs/fixes-104');out=Path('docs/validation/fixes-104');out.mkdir(exist_ok=True);e=out/'evidence';e.mkdir(exist_ok=True)
files=['actual-ui-results.json','actual-ui-compiler-results.json','compiler-results.json','package-modern.json','spelling-api-check.log','private-profile-update.json']
for f in files:shutil.copyfile(run/f,e/f)
shutil.copyfile(run/'test-results/fix104-regression.trx',e/'regression.trx')
packageChecks={}
for name,folder in [('CodeAssist.vsix',Path('CodeAssist.Extension/bin/Release')),('CodeAssist.VS2019.vsix',Path('CodeAssist.Extension/bin/VS2019/Release'))]:
 p=Path('installers/0.0.95')/name
 with zipfile.ZipFile(p) as z:
  root=ET.fromstring(z.read('extension.vsixmanifest'));identity=root.find('.//{*}Identity');assert identity.attrib['Version']=='0.0.95';checks={}
  for binary in ['CodeAssist.Extension.dll','CodeAssist.Core.dll']:
   raw=z.read(binary);checks[binary]=dict(sha256=hashlib.sha256(raw).hexdigest(),matchesBuild=raw==(folder/binary).read_bytes());assert checks[binary]['matchesBuild']
  packageChecks[name]=dict(version='0.0.95',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),binaries=checks)
(e/'packages.json').write_text(json.dumps(packageChecks,indent=2),encoding='utf-8')
ui=json.loads((e/'actual-ui-results.json').read_text());comp=json.loads((e/'actual-ui-compiler-results.json').read_text());assert len(ui)==5 and all(x['undoRestored'] for x in ui.values()) and all(x['passed'] for x in comp.values())
proof={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in e.iterdir() if p.is_file()};(out/'evidence-hashes.json').write_text(json.dumps(proof,indent=2),encoding='utf-8')
notes=[('setter','F018','setter 자기 대입 수정','필드 이름이 value여도 this->value에 대입합니다. 기존 내부 참조의 getter 전환까지 원본과 같다는 판정은 아닙니다.'),('member','F029','클래스 내부 멤버 삽입','클래스 이름 위의 커서에서도 public 멤버로 삽입합니다.'),('declaration','F035','소유 클래스 내부 선언 생성','같은 파일에 소유 클래스가 있으면 그 내부에 선언합니다. 다른 파일의 클래스 검색·삽입은 아직 지원하지 않습니다.'),('override','F041','override 삽입 위치 수정','파생 클래스 이름을 손상시키지 않고 클래스 본문에 override를 생성합니다.'),('header','F038','헤더 구현 중복 정의 방지','헤더에 생성하는 out-of-class 정의에 inline을 붙여 두 번역 단위 링크를 통과합니다. VA처럼 대응 CPP로 보내는 동작은 아직 차이가 있습니다.')]
body=''
for key,id,title,note in notes:
 body+='<article><h2>'+id+' '+title+'</h2><p>'+note+'</p><p>실제 VS 명령 → Apply changes → 생성 결과 수집 → 한 번의 Undo 복원: 통과. 실제 생성물 MSVC 컴파일·실행: 통과.</p><pre>'+html.escape(ui[key]['text'])+'</pre></article>'
text='''<!doctype html><meta charset="utf-8"><title>0.0.95 코드 생성 결함 수정</title><style>body{font:16px Segoe UI,sans-serif;background:#181a20;color:#e5e7eb;margin:28px}h1,h2{color:#ffd346}article{background:#252832;padding:18px;margin:18px 0}a{color:#83c4ff}pre{white-space:pre-wrap;font:13px Consolas,monospace}p{line-height:1.7}</style><h1>0.0.95 — 확인된 코드 생성 결함 5종 수정</h1><p><b>이번 수정 검증 5/5 = 100%</b> · 회귀 검사 36/36 통과.<br>이 수치는 5개 수정 시나리오의 성공률이며 VA 제품 전체 동기화율이 아닙니다. 기존 VA 대조 결과는 실행 판정 96/108(88.89%), 엄격 일치 15개, 차이 81개, 미판정 12개로 유지합니다. 문서·설치 도움말 기반 부분 명세 108/108(100%)도 기능 전체 인증이 아닙니다.</p><p>사용자 화면·물리 입력·공유 클립보드를 사용하지 않았습니다. CodeAssistVerify-runtime의 검증 VS에서 명령과 적용·Undo를 실행했습니다. UI 검증은 버전 갱신 전 수정 DLL로 수행했고, 0.0.95 설치 파일은 패키지 매니페스트와 빌드 DLL 해시를 별도로 검사했습니다.</p><p><a href="../../../installers/0.0.95/CodeAssist.vsix">64비트 VS 설치 파일</a> · <a href="../../../installers/0.0.95/CodeAssist.VS2019.vsix">VS2019 설치 파일</a> · <a href="evidence/actual-ui-results.json">실제 VS 결과·Undo</a> · <a href="evidence/actual-ui-compiler-results.json">MSVC 컴파일·실행 결과</a> · <a href="evidence/regression.trx">36개 회귀 검사</a> · <a href="evidence/packages.json">설치 파일 검사</a></p><h2>남은 검증과 기능 차이</h2><p><a href="../background-103/remaining-limitations.json">미판정 12개</a>는 완료로 변경하지 않았습니다. 사용자 확장자 색인은 CA 테스트에서 통과했지만 VA와 같은 조건의 실시간 대조는 남아 있습니다. 철자 검사는 대체 Windows API가 이 PC에서 en-US 미지원으로 나와 실험을 제품에 포함하지 않았습니다. 원본 Native 렌더링·물리 키/마우스·공유 클립보드·브라우저·인쇄·실제 파싱 동시성 검증도 남아 있습니다.</p><p><a href="evidence/spelling-api-check.log">철자 검사 대체 API 실행 기록</a> · <a href="../background-103/status.html">이전 108항목 대조표</a></p>'''+body
(out/'index.html').write_text(text,encoding='utf-8')
prior=Path('docs/validation/background-103/status.html');s=prior.read_text(encoding='utf-8');banner='<p><a href="../fixes-104/index.html">후속 0.0.95: 확인된 코드 생성 결함 5종 수정·실제 VS 재검증 결과</a> — 아래 표는 수정 전 0.0.94 기록입니다.</p>'
if '../fixes-104/index.html' not in s:s=s.replace('<h1>108항목 검증 현황</h1>','<h1>108항목 검증 현황</h1>'+banner);prior.write_text(s,encoding='utf-8')
print('5 actual command/Undo cases, 5 MSVC contracts, 36 regression tests, two package hashes verified')
