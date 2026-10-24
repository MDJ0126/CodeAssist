import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from adjudicate_background import update
r={}
for role in ['VA','CA']:
 f=OUT/(role+'.snippet-input-year.json');v=json.loads(f.read_text(encoding='utf-8-sig'));r[role]=v;save(role+'.snippet-input-year.bundle.json',dict(observed=v,requestedYear='2037',template=(OUT/'snippet-shared-template.txt').read_text(),libraryRestored=json.loads((OUT/'snippet-input-year.library-restore.json').read_text())))
assert '2037' in r['VA']['after']['text'] and '2037' not in r['CA']['after']['text']
update(87,'different','동일 Doxygen 본문의 YEAR에 VA는 실제 사용자 입력 대화상자를 표시하고 지정한 2037을 삽입했습니다. CA는 입력 없이 현재 연도 2026으로 치환했습니다. YEAR 토큰의 실제 입력 처리 차이를 검증한 대표 사례이며 모든 입력 이름·8필드 한도는 별도입니다. CA 스니펫 라이브러리는 복원했습니다.',{x:x+'.snippet-input-year.bundle.json' for x in r},dict(nativeRequestedInputInserted=True,caRequestedInputInserted=False,libraryRestored=True))
