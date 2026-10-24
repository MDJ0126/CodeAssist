import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from adjudicate_background import update
native=json.loads((OUT/'VA.view-file-nav.actual-retry.json').read_text(encoding='utf-8-sig'));ca=json.loads((OUT/'CA.view-file-nav.actual-ready.json').read_text());assert native['active']['file'].endswith('ViewFileTarget103.h') and ca['result']['file'].endswith('view-file-caller.cpp')
save('VA.view-file-nav.bundle.json',dict(actual=native,scope='file-name search and open from VA View'))
save('CA.view-file-nav.bundle.json',dict(actual=ca,indexPrepared=json.loads((OUT/'CA.view-file-nav.index-preparation-retry.json').read_text()),scope='double-click the actual same file parent node in Workspace; child symbol navigation is separate'))
update(94,'different','VA View의 실제 Files in Solution 입력에서 파일명을 검색하고 Enter를 보내면 소유 헤더가 열립니다. CA 작업공간에는 같은 파일 노드가 실제 존재하지만 파일 노드 더블클릭은 파일을 열지 않고 호출자 문서를 유지했습니다. 인덱스 준비 완료를 확인한 후 실행했으며 VA View 전체 동작 인증은 아닙니다.',{x:x+'.view-file-nav.bundle.json' for x in ['VA','CA']},dict(nativeTargetFileOpened=True,caTargetFileOpened=False,targetFilePresentInCaTree=True))
