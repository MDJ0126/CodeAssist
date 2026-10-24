import sys,json
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import ROOT,read,update
files={};results=read('compiler-check/move-all-source-result.json')
for role in ['VA','CA']:
 r=results[role];assert r['passed'] and r['bothMoved'] and r['undoStages'][-1]['restored'];data=dict(compiler=r,input=read('move-all-source.input.json'),after={n:read(role+'.move-all-source.after.'+n+'.json') for n in ['move-all-audit103.h','move-all-audit103.cpp']},settings=read('move-all-source.flags-transaction.json'));name=role+'.move-all-source.bundle.json';(ROOT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
assert len(results['VA']['undoStages'])==2 and len(results['CA']['undoStages'])==1
update(23,'different','같은 클래스의 두 메서드 전체를 양쪽 모두 CPP로 옮겼고 First()+Second()==15 실제 실행에 통과했습니다. 원복에는 VA가 Undo 두 번, CA는 한 번이 필요하며 구현 서식도 다릅니다. Native 선택 창의 두 메서드 체크와 CA의 2개 구현 미리보기를 확인했습니다.',files,{'bothMethodsMovedBoth':True,'compileAndRunBoth':True,'eventualUndoRestoresBoth':True,'undoStepCountEqual':False,'formattingEqual':False})
