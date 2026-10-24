import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from adjudicate_background import update
r={}
for role in ['VA','CA']:
 queued=json.loads((OUT/(role+'.create-from-usage.queued.json')).read_text());completion=json.loads(__import__('pathlib').Path(queued['result']['completion']).read_text(encoding='utf-8-sig'));buffer=json.loads((OUT/(role+'.create-from-usage.buffer.json')).read_text(encoding='utf-8-sig'));r[role]=dict(buffer=buffer,commandCompletion=completion,entry=queued);save(role+'.create-from-usage.bundle.json',r[role]);print(role,completion)
update(37,'different','동일한 미정의 멤버 호출 MissingAction103(7)에서 VA는 int 인수를 추론하고 private 메서드 본문을 실제 삽입했습니다. CA의 등록된 Create From Usage 명령은 VS Quick Actions로 위임하지만 이 조건에서 코드 생성 없이 원본을 유지했습니다. 본문 생성 동작의 대표 시나리오이며 class·enum 등 전체 생성 종류를 검증한 것은 아닙니다.',{x:x+'.create-from-usage.bundle.json' for x in r},dict(nativeMethodGenerated='void MissingAction103(int param1)' in r['VA']['buffer']['text'],caMethodGenerated='void MissingAction103' in r['CA']['buffer']['text']))
