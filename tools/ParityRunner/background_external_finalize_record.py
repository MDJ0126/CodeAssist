import sys,json,base64
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import RUN;from background_steps import OUT,save;from adjudicate_background import update
header=RUN/'shared-custom103/PlatformHeader103.h';buildResults=json.loads((OUT/'compiler-check/external-build-result.json').read_text())
j=json.loads((OUT/'external-build.flags-transaction.json').read_text());path=Path(j['path']);assert path==RUN/'CA/compile_flags.txt';expected=('-std=c++17\n-I'+str(header.parent).replace('\\','/')+'\n').encode();assert path.read_bytes().replace(b'\r\n',b'\n')==expected
if j['original'] is None:path.unlink()
else:path.write_bytes(base64.b64decode(j['original']))
j.update(caFlagsRestored=True,nativeSystemAndSourcePathsRestored=True,nativeProjectDefinedRestored=True);save('external-build.flags-transaction.json',j)
for role in ['VA','CA']:
 after=json.loads((OUT/('VA.external-build.source-dir-after.json' if role=='VA' else 'CA.external-build-lookup.after.json')).read_text());assert after['result']['dteFile']==str(header);save(role+'.external-build.bundle.json',dict(after=after,configuration=json.loads((OUT/('VA.external-build.after-restart-options.json' if role=='VA' else 'CA.external-build-lookup.queued.json')).read_text()),nativeSourceDirectoryAdded=json.loads((OUT/'VA.external-build.add-source-dir.json').read_text()),buildResults=buildResults,restored=j,scope='actual configured external header navigation and external command-line compilation; caret positions differ'))
update(106,'different','VA는 Custom의 System include·Source files 경로 설정과 재파싱 후, CA는 compile_flags.txt의 외부 -I 설정으로 같은 외부 헤더에 실제 도착했습니다. 외부 경로를 사용한 실제 명령줄 빌드·실행도 양쪽 통과했습니다. 설정 방식과 정의 커서 위치(VA 39, CA 20)가 달라 완전 일치로 세지 않았습니다. 테스트 경로 두 그룹과 CA 플래그는 원상복구했습니다.',{x:x+'.external-build.bundle.json' for x in ['VA','CA']},dict(actualExternalHeaderReachedBoth=True,actualExternalCommandLineBuildPassedBoth=True,actualExecutablePassedBoth=True,configurationWorkflowEquivalent=False,definitionCaretEquivalent=False,settingsRestored=True))
