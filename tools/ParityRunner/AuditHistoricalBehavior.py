"""Reassess archived evidence without claiming any new product execution."""
import argparse, hashlib, html, json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from Aggregate108 import assess, read, sha
ROOT=Path(__file__).resolve().parents[2]
def run(output):
    archive=ROOT/'docs/validation/fixed-108-097-final'
    provenance=read(archive/'provenance.json')
    receipts={x['file'].replace('\\','/'):x['sha256'] for x in provenance}
    def verified(path):
        rel=path.relative_to(ROOT).as_posix()
        if rel not in receipts or sha(path)!=receipts[rel]:
            raise ValueError('Archived evidence changed or receipt missing: '+rel)
        return read(path)
    plan_path=ROOT/'tools/ShortcutProbe/behavior-plan.json'
    plan=verified(plan_path)
    actual=verified(archive/'live/behavior.json')
    cases={x['id']:x for x in actual['cases']}
    rows=[]
    for c in plan['cases']:
        oracle_path=ROOT/c['oracleFile']
        oracle=verified(oracle_path)
        if sha(oracle_path)!=c['oracleSha256']:
            raise ValueError('VA oracle receipt mismatch')
        va=next((x for x in oracle['cases'] if x['id']==c['id']),None)
        state,reason=assess(c,cases.get(c['id']),va)
        observed=state
        if state=='representative-match' and c.get('acceptanceEligible') is False:
            state,reason='unverified',c['acceptanceReason']
        rows.append(dict(id=c['id'],state=state,observedOutputState=observed,reason=reason,VA=str(oracle_path.relative_to(ROOT)),CA=str((archive/'live/behavior.json').relative_to(ROOT))))
    counts=Counter(x['state'] for x in rows)
    result=dict(generatedAt=datetime.now(timezone.utc).isoformat(),scope='Archived 0.0.94 evidence reassessment; not current 0.0.116 product verification',computerUsePerformed=False,newRuntimeExecutions=0,total=len(rows),counts=dict(counts),cases=rows)
    output.mkdir(parents=True,exist_ok=True)
    (output/'results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    table=''.join('<tr>'+''.join('<td>'+html.escape(x[k])+'</td>' for k in ['id','state','reason'])+'</tr>' for x in rows)
    page='<html lang="ko"><meta charset="utf-8"><title>기존 실행 근거 재판정</title><style>body{font:15px/1.6 system-ui;background:#20232b;color:#eee;padding:32px}td,th{border:1px solid #666;padding:10px}table{border-collapse:collapse}a{color:#ffd44d}</style><h1>기존 실행 근거 재판정</h1><p>0.0.94 보관 자료의 해시를 확인하고 강화된 판정기를 적용했습니다. 현재 0.0.116 실행·화면 검증이 아니며 신규 통과는 0건입니다.</p><p>'+html.escape(str(dict(counts)))+' / '+str(len(rows))+'개</p><table><tr><th>시나리오</th><th>재판정</th><th>근거</th></tr>'+table+'</table><p><a href="results.json">원본 결과</a></p></html>'
    (output/'index.html').write_text(page,encoding='utf-8')
    print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,required=True);args=ap.parse_args();run(args.output)
