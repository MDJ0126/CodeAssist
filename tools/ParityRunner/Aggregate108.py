"""Fixed-denominator evidence aggregation. UI probes never become functional passes."""
import argparse,collections,copy,csv,hashlib,html,json,shutil
from pathlib import Path
from datetime import datetime,timezone
ROOT=Path(__file__).resolve().parents[2]
FIELDS=('text','caret','selectionStart','selectionLength','selectedText')
def read(path):return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def equal(a,b,fields=FIELDS):return isinstance(a,dict) and isinstance(b,dict) and all(k in a and k in b and a[k]==b[k] for k in fields)
def assess(c,actual,va):
    fields=tuple(c.get('compareFields',FIELDS))
    if fields not in (FIELDS,FIELDS+('zoom',)):raise ValueError('Comparison fields cannot omit core output assertions')
    if not actual or actual.get('error') or not actual.get('physicalInput'):return 'unverified','실행 누락·입력 실패·검사 환경 차단'
    before={'text':c['text'],'caret':c['start']+c['length'],'selectionStart':c['start'],'selectionLength':c['length'],'selectedText':c['text'][c['start']:c['start']+c['length']]}
    if 'preZoom' in c:before['zoom']=c['preZoom']
    if not equal(actual.get('before'),before,fields):return 'unverified','입력 문서·선택 설정 불일치'
    if not va or not equal(va.get('before'),before,fields):return 'unverified','VA 원본 입력 문서·선택 증거 누락 또는 불일치'
    if va.get('error') or not equal(c.get('expected'),va.get('after'),fields):return 'unverified','VA 원본 출력과 기대값 불일치'
    if equal(before,va['after'],fields):return 'unverified','양쪽 무반응은 통과가 아님'
    if not actual.get('after'):return 'unverified','실행 결과 누락'
    if not equal(actual['after'],va['after'],fields):return 'different','실제 버퍼·커서·선택 결과 차이'
    if c.get('verifyUndo'):
        if not va.get('undo') or not actual.get('undo'):return 'unverified','필수 Undo 증거 누락'
        if va['undo'].get('text')!=c['text']:return 'unverified','VA Undo가 입력 문서를 복원하지 않음'
        if not equal(actual['undo'],va['undo']):return 'different','Undo 복원 결과 차이'
    return 'representative-match','대표 시나리오의 실제 결과 일치; 기능 전체 인증 아님'
def tests():
    before=dict(text='',caret=0,selectionStart=0,selectionLength=0,selectedText='')
    after=dict(text='()',caret=1,selectionStart=1,selectionLength=0,selectedText='')
    c=dict(text='',start=0,length=0,expected=after,verifyUndo=False)
    va=dict(error=None,before=before,after=after)
    a=dict(error=None,physicalInput=True,before=before,after=after)
    checks=[]
    def check(name,wanted,cc=c,aa=a,vv=va):
        result=assess(cc,aa,vv)[0];assert result==wanted,(name,result,wanted);checks.append(name)
    check('exact actual result','representative-match')
    check('missing VA before','unverified',vv=dict(va,before=None))
    check('wrong VA before','unverified',vv=dict(va,before=after))
    check('missing VA record','unverified',vv=None)
    check('matching broken Undo','unverified',cc=dict(c,verifyUndo=True),aa=dict(a,undo=after),vv=dict(va,undo=after))
    for k,v in [('text','wrong'),('caret',2),('selectionStart',0),('selectionLength',1),('selectedText','wrong')]:
        x=copy.deepcopy(a);x['after'][k]=v;check(k+' mismatch','different',aa=x)
    for name,x in [('missing result',None),('blocked',dict(a,error='modal')),('no input',dict(a,physicalInput=False)),('wrong fixture',dict(a,before=after)),('no after',dict(a,after=None))]:check(name,'unverified',aa=x)
    check('untrusted expected','unverified',cc=dict(c,expected=before))
    check('matching no-op','unverified',cc=dict(c,expected=before),aa=dict(a,after=before),vv=dict(va,after=before))
    check('missing undo','unverified',cc=dict(c,verifyUndo=True),vv=dict(va,undo=before))
    check('wrong undo','different',cc=dict(c,verifyUndo=True),aa=dict(a,undo=after),vv=dict(va,undo=before))
    check('correct undo','representative-match',cc=dict(c,verifyUndo=True),aa=dict(a,undo=before),vv=dict(va,undo=before))
    zoomBefore=dict(before,zoom=140);zoomAfter=dict(before,zoom=100)
    zc=dict(c,expected=zoomAfter,preZoom=140,compareFields=list(FIELDS)+['zoom']);za=dict(a,before=zoomBefore,after=zoomAfter);zv=dict(va,before=zoomBefore,after=zoomAfter)
    check('zoom action is not a no-op','representative-match',cc=zc,aa=za,vv=zv)
    check('wrong zoom fails','different',cc=zc,aa=dict(za,after=dict(zoomAfter,zoom=140)),vv=zv)
    for fields in ([],['text']):
        try:assess(dict(c,compareFields=fields),a,va)
        except ValueError:checks.append('reject weakened field set')
        else:raise AssertionError('Weak assertion set accepted')
    print('Evidence aggregation adversarial tests:',len(checks),'passed');return checks

def run(output,live_path):
    output.mkdir(parents=True,exist_ok=True)
    spec_path=ROOT/'docs/validation/integrated-spec.json';spec=read(spec_path)
    assert len(spec['features'])==len({x['id'] for x in spec['features']})==108
    fixed={'VA-F'+str(i).zfill(3) for i in range(1,109)}
    assert {x['id'] for x in spec['features']}==fixed
    manifest_path=ROOT/'tools/ParityRunner/acceptance-108.json'
    if manifest_path.exists():
        assert set(read(manifest_path)['baselineIds'])==fixed, 'Fixed baseline IDs changed'
    plan_path=ROOT/'tools/ShortcutProbe/behavior-plan.json';plan=read(plan_path);live=read(live_path)
    assert len(live['cases'])==len({x['id'] for x in live['cases']})
    actual={x['id']:x for x in live['cases']};sources={spec_path,plan_path,live_path}
    old_plan_path=ROOT/'.local/parity-runs/20261018-015759-745/VA.plan.json';old_plan=read(old_plan_path)
    original_cases={x['id']:x for x in old_plan['cases']};sources.add(old_plan_path)
    scenarios={};images=[]
    from PIL import Image
    for c in plan['cases']:
        oracle_path=ROOT/c['oracleFile'];sources.add(oracle_path)
        if sha(oracle_path)!=c['oracleSha256']:raise ValueError('Changed VA oracle '+str(oracle_path))
        oracle=read(oracle_path)
        if oracle.get('role')!='VA' or oracle.get('planHash')!=hashlib.sha256(old_plan_path.read_bytes().decode('utf-8-sig').encode()).hexdigest():raise ValueError('VA role or plan hash mismatch')
        v={x['id']:x for x in oracle['cases']}.get(c['id'])
        if not v:raise ValueError('Missing VA oracle case')
        if v.get('inputHash')!=hashlib.sha256(c['text'].encode('utf-8')).hexdigest():raise ValueError('VA input hash mismatch')
        original=original_cases[c['id']]
        if any(c[k]!=original[k] for k in ('text','start','length')):raise ValueError('Input differs from actual VA run')
        state,reason=assess(c,actual.get(c['id']),v)
        observed_state=state
        if state=='representative-match' and c.get('acceptanceEligible') is False:state,reason='unverified',c['acceptanceReason']
        scenarios[original['feature']]={'state':state,'reason':reason,'observedOutputState':observed_state,'scenarioId':c['id'],'CA':str(live_path.relative_to(ROOT)),'VA':str(oracle_path.relative_to(ROOT)),'before':(actual.get(c['id']) or {}).get('before'),'expected':v.get('after'),'after':(actual.get(c['id']) or {}).get('after'),'undo':(actual.get(c['id']) or {}).get('undo'),'undoRequired':c.get('verifyUndo',False)}
        for role,folder in [('VA',oracle_path.parent/'screenshots'),('CA',live_path.parent)]:
            for phase in ('before','after','undo'):
                candidates=[folder/(role+'-'+c['id']+'-'+phase+'.png'),folder/(c['id']+'-'+phase+'.png')]
                source=next((x for x in candidates if x.exists()),None)
                if source:
                    with Image.open(source) as im:im.verify()
                    dest=output/'screenshots'/(role+'-'+c['id']+'-'+phase+'.png');dest.parent.mkdir(exist_ok=True);shutil.copy2(source,dest)
                    images.append({'case':c['id'],'role':role,'phase':phase,'file':str(dest.relative_to(output)),'sha256':sha(dest)})
    history_path=ROOT/'docs/validation/layout-audit-0.0.90/all-108-analysis.json';history={x['id']:x for x in read(history_path)};sources.add(history_path)
    key_path=ROOT/'docs/validation/shortcuts-095/CA094.json';key=read(key_path);sources.add(key_path)
    rename=next(x for x in key['cases'] if x['expectedCACommand']==266)
    rows=[]
    for f in spec['features']:
        number=int(f['id'][-3:]);old=history.get(f['id'],{});main=scenarios.get(number)
        state=main['state'] if main else 'unverified';reason=main['reason'] if main else '대표 기능 결과를 비교하는 실행 어댑터·기대 결과가 없음'
        access=None
        if number==25:
            access={'chord':rename['chord'],'received':rename['caCommandObserved'],'source':str(key_path.relative_to(ROOT)),'scope':'기본 단축키 접근 동작'}
            if rename['inputSent'] and rename.get('error') is None and rename['availability'] and not rename['caCommandObserved']:
                state='different';reason='이름 바꾸기 기본 키 Alt+Shift+R 명령 미수신; 기능 결과 비교 완료 아님'
        doc=f.get('documentationSpec') or {}
        rows.append({'id':f['id'],'name':f['name'],'state':state,'reason':reason,'mainEvidence':main,'accessEvidence':access,'historicalProbe':{'scope':old.get('currentRunScope'),'observation':old.get('currentProbe'),'error':old.get('currentError'),'countsAsFunctionalPass':False},'documentedAssertions':doc.get('observedSpec',[]),'nextScenarios':doc.get('comparisonScenarios',[]),'requiredGates':f.get('requiredGates',[]),'fullFeatureCertified':False,'nextAction':'추가 경계·설정·지원 범위 검증' if state=='representative-match' else ('기본 키 수신 경로 조사·수정·재검사' if number==25 else '동일 입력/문맥의 대표 동작 실행 어댑터와 원본 기대 결과 확보')})
    counts=collections.Counter(x['state'] for x in rows);assert sum(counts.values())==108
    summary={'schema':1,'denominator':108,'representativeMatches':counts['representative-match'],'confirmedDifferences':counts['different'],'unverified':counts['unverified'],'confirmedRepresentativeMatchPercent':round(counts['representative-match']/108*100,2),'determinateChecksPercent':round((counts['representative-match']+counts['different'])/108*100,2),'catalogAccountingPercent':100,'fullFeatureCertificates':0,'fullProductSimilarity':None,'scope':'108개 고정 목록의 대표 동작/접근 결과 확인 현황. 대표 일치는 기능 전체 인증이 아니며, 미검증을 불일치로 추정하지 않음. 전체 제품 실제 유사도 추정값이 아님.','freshBehaviorPassed':sum(x.get('observedOutputState',x['state'])=='representative-match' for x in scenarios.values()),'freshBehaviorTotal':len(scenarios),'shortcutReceived':sum(x['caCommandObserved'] for x in key['cases']),'shortcutTotal':len(key['cases']),'generatedAt':datetime.now(timezone.utc).isoformat()}
    (output/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    (output/'features.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    (output/'provenance.json').write_text(json.dumps([{'file':str(x.relative_to(ROOT)),'sha256':sha(x)} for x in sorted(sources)],ensure_ascii=False,indent=2),encoding='utf-8')
    (output/'screenshots.json').write_text(json.dumps(images,ensure_ascii=False,indent=2),encoding='utf-8')
    with (output/'features.csv').open('w',encoding='utf-8-sig',newline='') as stream:
        writer=csv.DictWriter(stream,fieldnames=['id','name','state','reason','nextAction']);writer.writeheader();writer.writerows({k:r[k] for k in writer.fieldnames} for r in rows)
    labels={'representative-match':'대표 결과 일치','different':'차이 확인','unverified':'미검증'}
    h='<!doctype html><html lang="ko"><meta charset="utf-8"><title>108개 고정 분모 검증</title><style>body{background:#1d2026;color:#eee;font:15px/1.65 system-ui;margin:32px}a{color:#ffd453}table{border-collapse:collapse;width:100%}td,th{border:1px solid #555;padding:9px;vertical-align:top}.representative-match{color:#80de9b}.different{color:#ff9898}.unverified{color:#ddd}pre{white-space:pre-wrap}.pair{display:flex;gap:12px}.pair img{max-width:47%;height:auto;object-fit:contain}details{margin:12px 0;padding:12px;border:1px solid #555}input{padding:10px;width:75%;background:#30343c;color:white}</style>'
    h+=f'<h1>108개 고정 분모 검증</h1><p><strong>확인된 대표 결과 일치 {counts["representative-match"]}/108 ({summary["confirmedRepresentativeMatchPercent"]}%) · 차이 {counts["different"]}/108 · 미검증 {counts["unverified"]}/108</strong></p><p>검증 판정 진행률 {summary["determinateChecksPercent"]}% ({counts["representative-match"]+counts["different"]}/108). 목록 집계 108/108은 실행 검증률이 아닙니다. 확인된 부분의 하한 현황이며, 실제 제품이 {summary["confirmedRepresentativeMatchPercent"]}%만 구현됐다는 뜻이 아닙니다. 포괄 기능 인증은 0/108이며 전체 실제 유사도를 추정하지 않습니다.</p><p>새 실제 키·버퍼·커서·선택·요청된 Undo 검사: {summary["freshBehaviorPassed"]}/{summary["freshBehaviorTotal"]}. 기본 단축키 수신: {summary["shortcutReceived"]}/{summary["shortcutTotal"]}. VA는 기존 실제 실행 증거, CA는 0.0.94 검사 프로필의 이번 실행입니다. 버전·환경 범위를 혼합해 일치로 승격하지 않았습니다.</p>'
    h+='<p><a href="summary.json">집계 JSON</a> · <a href="features.csv">108개 CSV</a> · <a href="features.json">상세 JSON</a> · <a href="provenance.json">증거 해시</a></p><input placeholder="기능·번호·판정 검색" oninput="document.querySelectorAll(\'.feature\').forEach(x=>x.hidden=!x.textContent.toLowerCase().includes(this.value.toLowerCase()))">'
    for r in rows:
        h+='<details class="feature"><summary class="'+r['state']+'">'+html.escape(r['id']+' '+r['name']+' — '+labels[r['state']])+'</summary><p>'+html.escape(r['reason'])+'</p><p>다음 작업: '+html.escape(r['nextAction'])+'</p><p>기존 프로브: '+html.escape(str(r['historicalProbe']))+'</p><ul>'+''.join('<li>'+html.escape(t)+'</li>' for t in r['nextScenarios'])+'</ul>'
        if r['mainEvidence']:
            m=r['mainEvidence'];h+='<pre>'+html.escape(json.dumps(m,ensure_ascii=False,indent=2))+'</pre>'
            for phase in ('before','after','undo'):
                photos=[i for i in images if i['case']==m['scenarioId'] and i['phase']==phase]
                if photos:h+='<p>'+phase+'</p><div class="pair">'+''.join('<a href="'+i['file']+'"><img loading="lazy" src="'+i['file']+'" alt="'+i['role']+' '+phase+'"></a>' for i in photos)+'</div>'
        h+='</details>'
    h+='</html>';(output/'index.html').write_text(h,encoding='utf-8');print(json.dumps(summary,ensure_ascii=False));return summary
if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--self-test',action='store_true');ap.add_argument('--live-report',type=Path);ap.add_argument('--output',type=Path,default=ROOT/'docs/validation/fixed-108-096');args=ap.parse_args()
    if args.self_test:tests()
    if args.live_report:run(args.output.resolve(),args.live_report.resolve())
