import sys,json
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import ROOT,read,update
files={}
for role in ['VA','CA']:
 data={n:read(role+'.move-formatted-source.'+n+'.buffer.json') for n in ['move-formatted103.h','move-formatted103.cpp']};data['compiler']=read('compiler-check/move-formatted-source-result.json')[role]
 if role=='CA':data['viewStateOnError']=read('CA.move-formatted-source.view-state.json');data['errorWindows']=read('CA.move-formatted-source.failed-windows.json');data['preview']=read('CA.move-formatted-source.preview.json')
 else:data['undo']={n:read(role+'.move-formatted-source.'+n+'.undo.json') for n in ['move-formatted103.h','move-formatted103.cpp']}
 name=role+'.move-formatted-source.bundle.json';(ROOT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
update(22,'different','여러 줄 클래스에서 VA는 헤더 본문을 대응 CPP로 옮기고 한 번의 Undo로 양쪽을 복원했습니다. CA는 올바른 미리보기를 만들지만 비활성 전용 데스크톱의 적용 단계에서 CPP를 열고도 SDK가 헤더 뷰를 반환해 문서 변경 경고로 중단했습니다. 양쪽 코드 실행은 Value()==7을 유지하나 CA 본문 이동은 적용되지 않았습니다. 이 실행 조건의 결과이며 일반 사용자 화면에서의 재현 범위는 별도입니다.',files,{'validFixtureBothCompileAndRun':True,'bodyActuallyMovedBoth':False,'nativeUndoRestoresBothFiles':True,'caSdkViewPathMismatchObserved':True})
