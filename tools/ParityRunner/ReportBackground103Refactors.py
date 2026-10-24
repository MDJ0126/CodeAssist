from pathlib import Path
import json,hashlib
root=Path('docs/validation/background-103');f=json.loads((root/'features.json').read_text());d=json.loads((root/'adjudicated.json').read_text())
def ev(file):
 p=root/file;return dict(file=file,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),data=json.loads(p.read_text(encoding='utf-8-sig')))
for id in ['encapsulate','extract']:
 for role in ['VA','CA']:
  b=json.loads((root/(role+'.'+id+'.prepared.json')).read_text(encoding='utf-8-sig'));a=json.loads((root/(role+'.'+id+'.after.json')).read_text());u=json.loads((root/(role+'.'+id+'.undo.json')).read_text());assert a['status']==u['status']=='complete' and b['before']['text']==u['after']['text']
enc=json.loads((root/'compiler-check/encapsulate-runtime-result.json').read_text());assert enc['VA']['passed'] and not enc['CA']['passed'];ext=json.loads((root/'compiler-check/extract-runtime-result.json').read_text());assert ext['VA']['passed'] and ext['CA']['passed']
rows=[(18,'동일 필드 커서에서 양쪽 캡슐화 적용·Undo 복원. VA setter 실행 후 getter=7 통과, CA는 매개변수 자기 대입(value=value)으로 getter=7 계약 실패. 양쪽 컴파일 성공. 기존 내부 참조의 getter 치환 여부도 다름.','VA.encapsulate.after.json','CA.encapsulate.after.json'),(19,'동일 두 문장 선택·같은 MyMethod 시그니처로 추출 적용. 양쪽 컴파일·실행에서 StepOne→StepTwo 1회씩 유지, Undo 복원. 줄바꿈·들여쓰기·호출부 서식은 다름.','VA.extract.after.json','CA.extract.after.json'),(20,'동일 상수 표현식 선택에서 VA는 subtotal 선언·표현식 교체 및 Undo 수행. CA 현재 런타임 74개 명령에 변수 도입 전용 명령이 없고, 대응 빠른 작업 실행 메뉴는 함수 추출만 제공. 이 fixture의 진입점/제공 작업 차이이며 모든 컴파일러 컨텍스트의 불가능 판정은 아님.','VA.introduce-variable.after.json','CA.introduce-variable.quick-actions.json')]
for n,reason,a,b in rows:
 id='VA-F%03d'%n;next(x for x in f if x['id']==id).update(state='different',reason=reason,mainEvidence='background-103/'+a,comparisonEvidence='background-103/'+b,fullFeatureCertified=False);d['newJudgments']=[x for x in d['newJudgments'] if x['id']!=id]+[dict(id=id,state='different',reason=reason,evidence={'VA':ev(a),'CA':ev(b)})]
d['counts']={s:sum(x['state']==s for x in f) for s in ['representative-match','different','unverified']};d['determinate']=108-d['counts']['unverified'];(root/'features.json').write_text(json.dumps(f,ensure_ascii=False,indent=2));(root/'adjudicated.json').write_text(json.dumps(d,ensure_ascii=False,indent=2));print(d['counts'])
