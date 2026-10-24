import sys,json,shutil
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import ROOT,read,update
for feature,id,label in [(67,'clean-focus-quick-info','Quick Info'),(65,'clean-focus-parameter-info','Parameter Info')]:
 files={}
 for role in ['VA','CA']:
  session=read(role+'.'+id+'.session.json');view=read(role+'.'+id+'.view-state.json');assert view['result']['views'][0]['hasFocus'];queued=read(role+'.'+id+'.queued.json');log=Path(queued['result']['completion']);outcome=json.loads(log.read_text(encoding='utf-8-sig'));sources=read(role+'.'+id+'.all-sources.json');popups=[x for x in sources['result']['sources'] if 'PopupRoot' in x['type']];data=dict(session=session,commandOutcome=outcome,preparedView=view,popupSources=popups,scope='standard editor command with owned focused view; hover and direct provider invocation separate')
  if role=='VA':
   assert popups;image=popups[0]['image']
  else:
   render=read(role+'.'+id+'.render.json');image=next(x['path'] for x in render['rows'] if x['label']=='editor' and x['status']=='captured')
  target=ROOT/'render-evidence'/(role+'-'+('quick-info-popup' if feature==67 else 'parameter-info-popup')+'.png' if role=='VA' else role+'-'+('quick-info-editor' if feature==67 else 'parameter-info-editor')+'.png');shutil.copyfile(image,target)
  name=role+'.'+id+'.bundle.json';(ROOT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
 va=read(files['VA'])['session']['result'];ca=read(files['CA'])['session']['result']
 if feature==67:assert va['quickState']=='Visible' and ca['quickState'] is None
 else:assert va['signatures'] and not ca['signatures']
 update(feature,'different',('자체 경고가 없고 실제 편집기 파일·포커스가 맞는 새 실행에서 표준 '+label+' 명령을 재검증했습니다. VA는 '+('클래스 정보 팝업을 표시하지만 CA는 같은 명령 후 세션·팝업이 없습니다. 마우스 hover 및 공급자 직접 호출과는 구분한 표준 명령 경로의 차이입니다.' if feature==67 else '두 번째 인수를 선택한 함수 서명 도움말을 표시하지만 CA는 E_ABORT를 반환하고 서명 세션이 없습니다. 다른 트리거·언어 전체 인증은 아닙니다.')),files,{'actualFileAndFocusValidatedBoth':True,'knownOldWarningDialogsAbsent':True,'sameStandardCommandBoth':True,'infoSessionPresentBoth':False})
