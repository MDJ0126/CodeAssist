import json,hashlib,shutil
from pathlib import Path
from PIL import Image
from collections import Counter
root=Path('docs/validation/background-103');f=json.loads((root/'features.json').read_text());d=json.loads((root/'adjudicated.json').read_text())
def evidence(role,file):
 p=root/file;return dict(file=file,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),data=json.loads(p.read_text(encoding='utf-8-sig')))
def update(n,reason,ev,checks):
 id='VA-F%03d'%n;next(x for x in f if x['id']==id).update(state='different',reason=reason,mainEvidence='background-103/'+ev['VA']['file'],comparisonEvidence='background-103/'+ev['CA']['file'],fullFeatureCertified=False,checkedAssertions=checks)
 d['newJudgments']=[x for x in d['newJudgments'] if x['id']!=id]+[dict(id=id,state='different',reason=reason,evidence=ev,checkedAssertions=checks)]
va=evidence('VA','VA.find-references.final.json');ca=evidence('CA','CA.find-references.navigation.json');assert va['data']['after']['selectionStart']==53 and va['data']['after']['selectedText']=='auditNeedle103';assert ca['data']['result']['caret']==53 and not ca['data']['result']['selectedText'];assert len(ca['data']['result']['entries'])==2
update(2,'코드 참조 2개 검색, 주석·문자열 제외, 두 번째 코드 참조 offset53으로 이동 모두 일치. VA는 이름 14자를 선택하며 CA는 커서만 이동.',dict(VA=va,CA=ca),dict(resultCount=True,excludeCommentAndString=True,navigationDestination=True,selectionState=False))
ev={};spans=[];colors={}
for role in ['VA','CA']:
 ev[role]=evidence(role,role+'.references-enabled.probe.json');r=ev[role]['data'];assert r['status']=='complete' and r['before']['caret']==37;spans.append([(s['start'],s['length'],s['text']) for t in r['markers'] for s in t['spans']]);capture=json.loads((root/(role+'.references-enabled.render.json')).read_text());image=Path(next(x['path'] for x in capture['rows'] if x['label']=='editor'));target=root/'render-evidence'/(role+'-references-enabled.png');shutil.copy2(image,target);colors[role]=Counter(Image.open(image).convert('RGB').crop((245,2,280,12)).getdata()).most_common(1)[0][0]
assert spans[0]==spans[1]==[(35,5,'value'),(58,5,'value')];assert colors['VA']!=colors['CA'];assert json.loads((root/'preference-transaction.json').read_text())['state']=='restored'
update(64,'양쪽 참조 강조를 실제 활성화하고 VA 추가 확인창을 아니요로 처리. 동일 value 두 곳(35,58)을 강조하나 VA 배경 RGB17/61/111, CA82/74/34로 다름. VS 기본 강조가 VA와 함께 켜진 구성 결과이며 VA 고유 마커 인과성은 별도. 양쪽 원래 꺼짐 설정 복원 확인.',ev,dict(twoReferenceSpans=True,backgroundColor=False,settingsRestored=True))
d['counts']={s:sum(x['state']==s for x in f) for s in ['representative-match','different','unverified']};d['determinate']=108-d['counts']['unverified'];(root/'features.json').write_text(json.dumps(f,ensure_ascii=False,indent=2));(root/'adjudicated.json').write_text(json.dumps(d,ensure_ascii=False,indent=2));print(d['counts'])
