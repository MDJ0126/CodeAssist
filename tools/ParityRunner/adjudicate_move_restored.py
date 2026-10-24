import sys,json
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import ROOT,read,update
files={}
for role in ['VA','CA']:
 data={n:read(role+'.move-restored-source.'+n+'.buffer.json') for n in ['move-restored103.h','move-restored103.cpp']};data['compiler']=read('compiler-check/move-restored-source-result.json')[role];data['undo']={n:read(role+'.move-restored-source.'+n+'.undo.json') for n in ['move-restored103.h','move-restored103.cpp']}
 if role=='CA':
  arm=read('CA.move-restored-source.restore-armed.json');data['ownerFocusRestore']=arm
  from pathlib import Path
  log=Path(arm['result']['log']);data['actualFocusRestore']=json.loads(log.read_text());assert data['actualFocusRestore']['hasAggregateFocus']
 assert data['compiler']['passed'] and data['compiler']['bodyMovedToSource'];assert data['undo']['move-restored103.cpp']['text']=='#include "move-restored103.h"\r\n'
 name=role+'.move-restored-source.bundle.json';(ROOT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
update(22,'different','미리보기 종료 시 전용 소유 편집기의 포커스를 복원한 재검증에서는 양쪽 모두 헤더 본문을 CPP로 옮겼고 Value()==7 실제 컴파일·실행과 한 번의 Undo 복원에 통과했습니다. 남은 차이는 선언 들여쓰기·구현 줄바꿈 등 서식입니다. 이전 적용 실패는 비활성 검증 환경의 포커스 영향으로 구분해 이력에 보존했습니다.',files,{'bodyActuallyMovedBoth':True,'compileAndRunBoth':True,'oneUndoRestoresBothFiles':True,'formattingEqual':False,'priorFocusFailureSuperseded':True})
