import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from adjudicate_background import update
geometry=json.loads((OUT/'matching-comment.geometry.json').read_text())
for role in ['VA','CA']:
 save(role+'.matching-comment.bundle.json',dict(geometry=geometry[role],activeMarkers=json.loads((OUT/(role+'.matching-comment.active.markers.json')).read_text()),restoredTransaction=json.loads((OUT/'matching-comment.transaction.json').read_text()),scope='automatic comment word highlight at caret; not manual Find Selected'))
assert geometry['VA']['restoredChangedPixels']==0 and geometry['CA']['restoredChangedPixels']==0
update(63,'different','VA의 주석·문자열 matching words 옵션을 실제 켜고 주석 단어에 커서를 두면 다른 동일 단어가 노란색으로 강조되고 커서를 떠나면 원복됩니다. CA는 같은 자동 강조가 없으며 현재 줄 배경 변화만 보였습니다. 수동 Find Selected 명령과 구분했고 VA 설정은 원래 값으로 복원했습니다.',{x:x+'.matching-comment.bundle.json' for x in geometry},dict(nativeAutomaticHighlightObserved=True,caAutomaticHighlightObserved=False,reversibleBoth=True,settingsRestored=True))
