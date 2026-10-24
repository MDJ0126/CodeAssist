import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from adjudicate_background import update
r={}
for role in ['VA','CA']:
 v=json.loads((OUT/(role+'.refactor-custom-template.observed.json')).read_text());r[role]=v;save(role+'.refactor-custom-template.bundle.json',dict(observation=v,transaction=json.loads((OUT/'refactor-template.transaction.json').read_text())))
assert '// PARITY_103_CUSTOM_METHOD_BODY' in r['VA']['after']['text']
update(81,'different','실제 Refactor Create From Usage Method Body 스니펫에 고유 주석을 임시 추가하자 VA 생성 메서드에 해당 주석이 반영됐습니다. CA 동일 제목·본문 라이브러리는 대응 생성 명령에서 반영되지 않았습니다. 원본 cpp.tpl과 CA 라이브러리 모두 원래 바이트로 복원했습니다. 필수 제목 사용자화 대표 사례이며 삭제 재생성·모든 reserved 문맥은 별도입니다.',{x:x+'.refactor-custom-template.bundle.json' for x in r},dict(nativeCustomizedTemplateConsumed=True,caCustomizedTemplateConsumed='PARITY_103_CUSTOM_METHOD_BODY' in r['CA']['after']['text'],nativeBytesRestored=True,caBytesRestored=True))
