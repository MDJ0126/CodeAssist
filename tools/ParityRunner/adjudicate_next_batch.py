import sys,json
sys.path.insert(0,'tools/ParityRunner')
from adjudicate_background import update,read
for n,reason in [(58,'동일 멤버·지역 변수에서 VA는 네임스페이스·클래스·함수까지 문맥 경로를 표시하지만 CA는 클래스명만 표시합니다. 미해결 심볼에서 VA 문맥은 비고 CA는 클래스명을 유지합니다.'),(59,'지역 변수에서 VA 정의 필드는 초기화식까지 표시하지만 CA는 형식과 이름만 표시합니다. 미해결 심볼에서 VA는 빈 필드이고 CA는 입력 이름을 표시합니다.')]:
 a={p:read('VA.context-dte.'+p+'.values.json') for p in ['field','local','unknown']};b={p:read('CA.context-dte.'+p+'.values.json') for p in a}
 evidence={r:r+'.context-dte.bundle.json' for r in ['VA','CA']}
 for r in evidence:
  from adjudicate_background import ROOT
  (ROOT/evidence[r]).write_text(json.dumps({p:read(r+'.context-dte.'+p+'.values.json') for p in a},ensure_ascii=False,indent=2),encoding='utf-8')
 update(n,'different',reason,evidence,{'sameMemberAndLocalInput':True,'nativeQualifiedLocalScope':any('BarAudit103.Owner103.Compute.localCounter' in v for t,v in a['local']),'unknownDefinitionEqual':False})
files={}
for role in ['VA','CA']:
 vals={s:read(role+'.history-warm.'+s+'.observed.json') for s in ['goto','back','forward']}
 assert vals['back']['after']['caret']==126
 assert vals['goto']['after']['caret']==vals['forward']['after']['caret']
 from adjudicate_background import ROOT
 name=role+'.history-warm.bundle.json';(ROOT/name).write_text(json.dumps(vals,ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
update(10,'different','같은 파일에서 정의 이동→뒤로→앞으로를 실제 명령으로 수행했습니다. 양쪽 모두 호출 위치(126)로 돌아오고 정의로 재이동하지만 정의 도착 커서는 VA 21, CA 5로 다릅니다.',files,{'backReturnsCallsiteBoth':True,'forwardReturnsDefinitionBoth':True,'caretEqual':False})
