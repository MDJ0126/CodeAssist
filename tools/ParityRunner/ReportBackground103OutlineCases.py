import json,hashlib
from pathlib import Path
root=Path('docs/validation/background-103');f=json.loads((root/'features.json').read_text());d=json.loads((root/'adjudicated.json').read_text())
def ev(file):
 p=root/file;return dict(file=file,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),data=json.loads(p.read_text(encoding='utf-8-sig')))
va=ev('VA.missing-cases.valid.json');ca=ev('CA.missing-cases.buffer-after.json');assert va['data']['status']==ca['data']['status']=='complete';assert va['data']['before']['text']==ca['data']['text'];assert 'State::Running:' in va['data']['after']['text'] and 'State::Done:' in va['data']['after']['text'];assert va['data']['undo']['text']==va['data']['before']['text'];assert any(x['name']=='The selected syntax is unsupported. Select the complete declaration or expression.' for x in json.loads((root/'CA.missing-cases.valid.failure-tree.json').read_text(encoding='utf-8-sig')))
rows=[(30,'同一 switch カーソル条件で VA は Running/Done/default の case を生成し Undo で原文復元。CA は enum 入力後に unsupported syntax を表示し、実バッファは未変更。','VA.missing-cases.valid.json','CA.missing-cases.buffer-after.json')]
va2=ev('VA.outline.navigation.json');ca2=ev('CA.outline.module-navigation.json');assert va2['data']['after']['caret']==64;assert ca2['data']['result']['line']==4 and ca2['data']['result']['column']==10
rows.append((93,'동일 클래스·두 메서드 구조 표시 후 두 번째 메서드로 양쪽 실제 이동. VA는 4행 시작(offset64), CA는 4행 10열(이름 위치)로 이동. 초기 CA 검사기 실패는 모든 WPF 루트 탐색으로 해소.','VA.outline.navigation.json','CA.outline.module-navigation.json'))
rows[0]=(30,'동일 switch 커서 조건에서 VA는 Running/Done/default case를 생성하고 Undo로 원문 복원. CA는 enum 입력 뒤 unsupported syntax 경고를 표시하며 실제 버퍼는 변경되지 않음. 완전한 switch 선택 조건의 추가 검증은 별도.',rows[0][2],rows[0][3])
for n,reason,a,b in rows:
 id='VA-F%03d'%n;next(x for x in f if x['id']==id).update(state='different',reason=reason,mainEvidence='background-103/'+a,comparisonEvidence='background-103/'+b,fullFeatureCertified=False);d['newJudgments']=[x for x in d['newJudgments'] if x['id']!=id]+[dict(id=id,state='different',reason=reason,evidence={'VA':ev(a),'CA':ev(b)})]
d['counts']={s:sum(x['state']==s for x in f) for s in ['representative-match','different','unverified']};d['determinate']=108-d['counts']['unverified'];(root/'features.json').write_text(json.dumps(f,ensure_ascii=False,indent=2));(root/'adjudicated.json').write_text(json.dumps(d,ensure_ascii=False,indent=2));print(d['counts'])
