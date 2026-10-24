import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from background_client import module;from compiler_contracts import build_run;from adjudicate_background import update
expected=json.loads((OUT/'VA.suggest-recent-multiline.typed.json').read_text())['result']['after']['text'];r={}
for role in ['VA','CA']:
 if role=='VA':v,_=module(role,'native-list-accept',windowTitle='',hwnd=3283458,choice='nearbyValue103',expectedText=expected)
 else:v,_=module(role,'completion-commit',windowTitle='',choice='nearbyValue103')
 save(role+'.suggest-recent-multiline.accept.json',v);assert v['status']=='complete',v;r[role]=v['result']['after'];c=build_run('recent-suggestion',role,r[role]['text']+'\nint main(){return RecentSuggestion103()==7?0:1;}\n','same local variable accepted, runtime7');assert c['passed'];save(role+'.suggest-recent-multiline.bundle.json',dict(observation=v,compiler=c,nativeImage='VA.suggest-recent-multiline.snapshot.native-list-0.png' if role=='VA' else None));print(role,r[role],flush=True)
update(54,'representative-match','동일 함수 안 최근 지역 변수 nearbyValue103을 원본의 실제 추천 이미지와 CA CodeAssistPreview 세션에서 확인하고 수락했습니다. 완성 본문·커서·선택이 일치하고 실행 결과 7도 같습니다. 최근 지역 변수 대표 사례이며 C# 병합·모든 선택 규칙 전체 인증은 아닙니다.',{x:x+'.suggest-recent-multiline.bundle.json' for x in r},dict(actualSuggestionAcceptedBoth=True,runtimeBoth=True,textIdentical=r['VA']['text']==r['CA']['text'],caretIdentical=r['VA']['caret']==r['CA']['caret'],selectionIdentical=r['VA']['selectionLength']==r['CA']['selectionLength']))
