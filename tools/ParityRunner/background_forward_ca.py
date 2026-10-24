import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module,send
from compiler_contracts import build_files
from adjudicate_background import update
h=json.loads((RUN/'hosts.json').read_text());text='#pragma once\r\nclass ForwardContainer103\r\n{\r\n    ForwardTarget103* value;\r\n};\r\n';header='#pragma once\r\nclass ForwardTarget103 {};\r\n';(RUN/'CA/ForwardTarget103.h').write_bytes(header.encode());setup('CA','forward-menu',text,text.index('ForwardTarget103')+3,file='ForwardContainer103.h',extras=['ForwardTarget103.h']);v,_=module('CA','queue-command',windowTitle='',command='378',focusEditor=True);save('CA.forward-menu.queued.json',v);time.sleep(.3)
p=OUT/'CA.forward-menu.commit.spec.json';save(p.name,dict(title='CodeAssist',button='Save',privateFocus=True));q=launch(['drive',h['CA']['pid'],OUT/'CA.forward-menu.commit.json',p]);q.wait(timeout=15);v,_=send('CA',dict(kind='classification'));save('CA.forward-menu.observed.json',v);print(v['after'],flush=True)
r={}
for role in ['VA','CA']:
 v=json.loads((OUT/(role+'.forward-menu.observed.json')).read_text());r[role]=v;save(role+'.forward-menu.bundle.json',dict(observation=v,expected='global forward declaration before containing class; preserve caret logical symbol'))
update(28,'different','공식 문서의 실제 VA Quick Action 메뉴에서 Add Forward Declaration을 실행하면 포함 클래스 바깥 파일 상단에 선언을 삽입합니다. CA 대응 명령은 커서 줄 앞에 삽입해 포함 클래스 안에 중첩 선언을 만듭니다. 이전 직접 명령 E_FAIL은 기능 부재로 판정하지 않고 실제 메뉴 실행으로 대체했습니다.',{x:x+'.forward-menu.bundle.json' for x in r},dict(nativeGlobalDeclaration=r['VA']['after']['text'].index('class ForwardTarget103;')<r['VA']['after']['text'].index('class ForwardContainer103'),caGlobalDeclaration=r['CA']['after']['text'].index('class ForwardTarget103;')<r['CA']['after']['text'].index('class ForwardContainer103')))
