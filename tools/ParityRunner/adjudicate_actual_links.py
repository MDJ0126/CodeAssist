import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from adjudicate_background import update
for number,name in [(95,'simple'),(96,'complex')]:
 r={}
 for role in ['VA','CA']:
  v=json.loads((OUT/(role+'.actual-link-pattern.'+name+'.json')).read_text());assert v['status']=='complete';r[role]=v['result'];save(role+'.actual-link-pattern.'+name+'.bundle.json',dict(observation=v,input='AUDIT-103 AUDIT-xyz audit-7 AUDIT 42 AUDIT-84.',scope='actual host pattern compilation/matching and URL variable expansion; no browser or hover execution',nativeRuntimeCacheRestored=r[role].get('runtimeRegexCacheRestored',True)))
 reason=('단순 숫자 링크 규칙의 실제 호스트 실행: VA는 대소문자 무시와 구분자 공백을 허용해 4개 링크를 만들고 CA는 같은 입력에서 2개만 만듭니다. 공통 AUDIT-103/84의 URL 치환은 같습니다.' if number==95 else '같은 명명 그룹 정규식을 실제 호스트에서 실행: VA는 IgnoreCase/Singleline으로 3개 링크를 만들고 CA는 대소문자를 구분해 2개만 만듭니다. 공통 링크의 URL 치환은 같습니다.')+' 외부 브라우저/마우스 hover는 이 판정 범위가 아니며 사용자 설정은 저장하지 않고 원본 런타임 캐시는 복원했습니다.'
 update(number,'different',reason,{x:x+'.actual-link-pattern.'+name+'.bundle.json' for x in r},dict(actualMatchingBothExecuted=True,commonPositiveUrlEqual=r['VA']['matches'][0]['url']==r['CA']['matches'][0]['url'],allMatchesEqual=[(x['text'],x['url']) for x in r['VA']['matches']]==[(x['text'],x['url']) for x in r['CA']['matches']],nativeCacheRestored=r['VA']['runtimeRegexCacheRestored']))
