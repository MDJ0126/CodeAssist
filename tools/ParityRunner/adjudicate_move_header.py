import sys,json
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import ROOT,read,update
id='cross-move-to-header';stem=id+'103';inputs=read(id+'.input.json');files={}
for role in ['VA','CA']:
 data={name:read(role+'.'+id+'.'+name+'.buffer.json') for name in [stem+'.h',stem+'.cpp']};data['undo']={name:read(role+'.'+id+'.'+name+'.undo.json') for name in [stem+'.h',stem+'.cpp']};assert data['undo'][stem+'.h']['text']==inputs['header'] and data['undo'][stem+'.cpp']['text']==inputs['source'];data['compiler']=read('compiler-check/'+id+'-result.json')[role];assert data['compiler']['passed'] and data['compiler']['requestedMoveApplied'];data['inputs']=inputs
 name=role+'.'+id+'.bundle.json';(ROOT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
update(21,'different','양쪽 모두 CPP 구현을 헤더의 클래스 선언으로 이동하고 원래 CPP 정의를 제거했습니다. 실제 실행 Value()==7과 한 번의 Undo로 두 파일 복원에 통과했습니다. VA는 여러 줄·탭, CA는 한 줄·공백으로 구현을 배치해 서식이 다릅니다. 전용 미리보기 종료의 소유 창 포커스 복원을 적용했습니다.',files,{'bodyMovedToHeaderBoth':True,'sourceDefinitionRemovedBoth':True,'compileAndRunBoth':True,'undoBothFilesBoth':True,'formattingEqual':False})
