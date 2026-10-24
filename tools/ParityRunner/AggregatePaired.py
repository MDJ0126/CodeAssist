"""Compare role-isolated paired runtime snapshots; never count failed commands or no-ops as matches."""
import copy,hashlib,html,json,collections,shutil
from pathlib import Path
from datetime import datetime,timezone,timedelta
ROOT=Path(__file__).resolve().parents[2]
FIELDS=('text','caret','selectionStart','selectionLength','selectedText')
def equal(a,b):return isinstance(a,dict) and isinstance(b,dict) and all(k in a and k in b and a[k]==b[k] for k in FIELDS)
def judge(c,v,a):
 expected=dict(text=c['text'],caret=c['start']+c['length'],selectionStart=c['start'],selectionLength=c['length'],selectedText=c['text'][c['start']:c['start']+c['length']])
 if not v or not a or not v.get('executed') or not a.get('executed'):return 'unverified','명령 비활성·실행 실패'
 if not equal(v.get('before'),expected) or not equal(a.get('before'),expected):return 'unverified','검사 입력 불일치'
 if not v.get('after') or not a.get('after'):return 'unverified','실제 출력 누락'
 # An Undo failure cannot hide an already observed output difference.
 if v.get('error') and not (v.get('after') and 'Edit.Undo' in v['error']):return 'unverified','VA 실행 오류'
 if a.get('error') and not (a.get('after') and 'Edit.Undo' in a['error']):return 'unverified','CA 실행 오류'
 if not equal(v['after'],a['after']):return 'different','동일 입력의 실제 버퍼·커서·선택 결과 차이'
 if equal(v['before'],v['after']):return 'unverified','양쪽 무반응은 통과 아님'
 if c['id']=='repair-case' and 'auditwidget' in v['after']['text']:return 'unverified','대소문자 교정이 실제 발동하지 않음'
 if c['id']=='comment-continuation' and '// \n' not in v['after']['text'].replace('\r',''):return 'unverified','주석 계속 입력 기능 발동 미확인'
 if c.get('undo'):
  if not v.get('undo') or not a.get('undo'):return 'unverified','Undo 증거 누락'
  if not equal(v['undo'],a['undo']):return 'different','Undo 복원 차이'
 return 'representative-match','실제 대표 출력 일치'
def self_test():
 c=dict(id='x',text='',start=0,length=0,undo=False);b=dict(text='',caret=0,selectionStart=0,selectionLength=0,selectedText='');end=dict(text='()',caret=1,selectionStart=1,selectionLength=0,selectedText='');v=dict(executed=True,before=b,after=end,error=None);a=copy.deepcopy(v);checks=0
 def check(w,c=c,v=v,a=a):
  nonlocal checks
  assert judge(c,v,a)[0]==w;checks+=1
 check('representative-match');check('unverified',a=dict(a,executed=False));check('unverified',a=dict(a,before=end));check('unverified',a=dict(a,after=None));check('unverified',v=dict(v,after=b),a=dict(a,after=b));check('different',a=dict(a,after=b,error='Edit.Undo unavailable'));check('unverified',c=dict(c,undo=True));check('different',c=dict(c,undo=True),v=dict(v,undo=b),a=dict(a,undo=end));print('Paired comparator tests',checks,'passed');return checks

def run():
 out=ROOT/'docs/validation/paired-098';planfile=ROOT/'tools/ShortcutProbe/paired-plan.json';plan=json.loads(planfile.read_text());hash=hashlib.sha256(planfile.read_bytes()).hexdigest();vp=out/'VA-project2/VA.json';ap=out/'CA-project/CA.json';v=json.loads(vp.read_text());a=json.loads(ap.read_text());assert v['role']=='VA' and a['role']=='CA' and v['planHash']==a['planHash']==hash
 assert len(v['cases'])==len(a['cases'])==len(plan['cases'])==13
 # Independently observed extension isolation, not inferred from profile labels.
 home=Path.home()/'AppData/Local/Microsoft/VisualStudio'
 import xml.etree.ElementTree as E
 def identity(folder):
  result=[]
  for f in folder.rglob('extension.vsixmanifest'):
   root=E.parse(f).getroot();ns={'v':'http://schemas.microsoft.com/developer/vsx-schema/2011'};i=root.find('v:Metadata/v:Identity',ns);result.append(dict(id=i.get('Id'),version=i.get('Version'),file=str(f)))
  return result
 vi=identity(home/'17.0_ca39936fCodeAssistVAAudit/Extensions');ai=identity(home/'17.0_ca39936fCodeAssistAudit/Extensions');assert len(vi)==1 and vi[0]['id']=='44630d46-96b5-488c-8df926e21db8c1a3';assert len(ai)==1 and ai[0]['version']=='0.0.94';log=ROOT/'.local/VA-only-098-ready.activity.xml';s=log.read_text(encoding='utf-16');assert 'End package load [Visual Assist]' in s and 'End package load [CodeAssist' not in s
 (out/'isolation.json').write_text(json.dumps(dict(VA=vi,CA=ai,VAProcessId=v['pid'],CAProcessId=a['pid'],VAActivityLog=str(log),VAActivityLogSha256=hashlib.sha256(log.read_bytes()).hexdigest()),ensure_ascii=False,indent=2),encoding='utf-8')
 vr={x['id']:x for x in v['cases']};ar={x['id']:x for x in a['cases']};rows=[]
 for c in plan['cases']:
  state,reason=judge(c,vr.get(c['id']),ar.get(c['id']));rows.append(dict(id=c['id'],feature=c['feature'],state=state,reason=reason,VA=vr[c['id']],CA=ar[c['id']]))
 prior=ROOT/'docs/validation/fixed-108-097-final';features=json.loads((prior/'features.json').read_text());rank={'representative-match':1,'different':2};updates={}
 for r in rows:
  if r['state']=='unverified':continue
  k='VA-F'+str(r['feature']).zfill(3)
  if k not in updates or rank[r['state']]>rank[updates[k]['state']]:updates[k]=r
 for f in features:
  if f['id'] in updates:
   r=updates[f['id']];f.update(state=r['state'],reason=r['reason'],newPairedScenario=r['id'])
 counts=collections.Counter(f['state'] for f in features);assert len(features)==108 and len({f['id'] for f in features})==108
 images=[]
 from PIL import Image
 for role,folder in [('VA',vp.parent),('CA',ap.parent)]:
  for p in folder.glob('*-*.png'):
   with Image.open(p) as im:im.verify()
   images.append(dict(role=role,file=p.relative_to(out).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
 summary=dict(total=108,matches=counts['representative-match'],differences=counts['different'],unverified=counts['unverified'],confirmedMatchPercent=round(counts['representative-match']/108*100,2),determinatePercent=round((108-counts['unverified'])/108*100,2),newScenarioCounts=dict(collections.Counter(r['state'] for r in rows)),newScenarios=13,imageCount=len(images),productSimilarity=None,startedAt='2026-10-18T21:16:09+09:00',generatedAt=datetime.now(timezone(timedelta(hours=9))).isoformat(),selfTests=self_test())
 for name,obj in [('summary.json',summary),('features.json',features),('paired-results.json',rows),('screenshots.json',images)]:
  (out/name).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
 h='<!doctype html><html lang="ko"><meta charset="utf-8"><title>VA 단독 / CA 단독 실제 추가 검증</title><style>body{background:#1b1d24;color:#eee;font:15px/1.6 system-ui;margin:30px}a{color:#ffd166}pre{white-space:pre-wrap}img{max-width:47%}details{border:1px solid #555;margin:14px 0;padding:12px}.different{color:#ff9999}.representative-match{color:#8fe0aa}</style><h1>VA·CA 실제 추가 검증</h1>'
 h+=f'<p>108개 고정 목록: 대표 결과 일치 {summary["matches"]}/108 ({summary["confirmedMatchPercent"]}%) · 차이 {summary["differences"]}/108 · 미검증 {summary["unverified"]}/108. 판정 근거 확보 {108-summary["unverified"]}/108 ({summary["determinatePercent"]}%). 제품 전체 유사도 값이 아닙니다.</p>'
 h+='<p>이번 신규 실제 입력 시나리오 13개. VA 단독 새 프로필과 CA 0.0.94 단독 프로필을 실제 실행했습니다. 이전 기록의 다음 이동·선택 확장만 일치했던 기능도 반대 방향에서 차이가 나오면 차이로 재분류했습니다. 무반응·사용할 수 없는 명령·단순 팝업 열기는 통과하지 않습니다. 101개 전체 검증은 미완료입니다.</p><p><a href="isolation.json">확장 분리 근거</a> · <a href="paired-results.json">원본 실제 출력</a> · <a href="features.json">108개 상태</a> · <a href="summary.json">집계</a></p>'
 for r in rows:
  h+=f'<details><summary class="{r["state"]}">{r["id"]} · 기능 {r["feature"]} · {r["state"]}</summary><p>{r["reason"]}</p><pre>'+html.escape(json.dumps(r,ensure_ascii=False,indent=2))+'</pre>'
  for phase in ['before','after','undo']:
   h+='<div>'+''.join('<a href="'+i['file']+'"><img loading="lazy" src="'+i['file']+'"></a>' for i in images if Path(i['file']).name==r['id']+'-'+phase+'.png')+'</div>'
  h+='</details>'
 h+='<h2>전체 108개</h2>'+''.join('<p>'+html.escape(f['id']+' '+f['name']+' — '+f['state'])+'</p>' for f in features)+'</html>'
 (out/'index.html').write_text(h,encoding='utf-8');(ROOT/'docs/comparison-098.html').write_text('<!doctype html><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=validation/paired-098/index.html"><a href="validation/paired-098/index.html">실제 추가 검증</a>',encoding='utf-8');print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':run()
