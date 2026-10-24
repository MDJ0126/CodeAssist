import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from compiler_contracts import build_run;from adjudicate_background import update
r={}
for role in ['VA','CA']:
 v=json.loads((OUT/(role+'.snippet-surround.json')).read_text(encoding='utf-8-sig'));result=build_run('snippet-surround',role,v['after']['text']+'\nint main(){return SurroundAudit103()==5?0:1;}\n','selection disabled by #if 0, runtime result5');assert result['passed'],result;r[role]=v;save(role+'.snippet-surround.bundle.json',dict(observed=v,compiler=result,template=(OUT/'snippet-surround.template.txt').read_text(),libraryRestored=json.loads((OUT/'snippet-surround.library-restore.json').read_text())))
update(83,'different','설치된 VA #if 0 스니펫 본문을 양쪽에 동일하게 사용해 선택한 value += 2 문장을 실제 감쌌습니다. 양쪽 컴파일·실행 결과가 5로 같아 선택 코드가 비활성화된 것을 확인했습니다. CA는 추가 빈 줄을 남겨 출력 형식이 다릅니다. surround 문자·모든 템플릿의 전체 검증은 아닙니다.',{x:x+'.snippet-surround.bundle.json' for x in r},dict(selectedCodePreservedBoth=True,conditionalBlockCreatedBoth=True,outputTextIdentical=r['VA']['after']['text']==r['CA']['after']['text']))
