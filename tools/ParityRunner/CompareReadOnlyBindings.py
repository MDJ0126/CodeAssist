"""Compare current registrations only. Never certifies physical key dispatch."""
import json,html
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/validation/bindings-live-121'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def normalized(binding):
    scope,chord=binding.split('::',1)
    scope={'텍스트 편집기':'editor','Text Editor':'editor','전역':'global','Global':'global'}.get(scope,scope)
    chord=chord.upper()
    for source,target in [('아래쪽 화살표','DOWN'),('위쪽 화살표','UP'),('왼쪽 화살표','LEFT'),('오른쪽 화살표','RIGHT')]:chord=chord.replace(source,target)
    return scope,'+'.join(sorted(chord.split('+')))
def selftest():
    assert normalized('텍스트 편집기::Shift+Alt+]')==normalized('Text Editor::Alt+Shift+]')
    assert normalized('전역::Alt+G')!=normalized('텍스트 편집기::Alt+G')
    assert normalized('텍스트 편집기::Alt+위쪽 화살표')==normalized('Text Editor::Alt+Up')
    assert normalized('텍스트 편집기::Alt+]')!=normalized('텍스트 편집기::Shift+Alt+]')
    return 4
def run():
    tests=selftest();ca=read(OUT/'CA.json');va=read(OUT/'VA.json')
    plan=read(ROOT/'docs/validation/shortcuts-095/CA094.json')['cases']
    names=['FindSelected','Paste','VAHashtags','SmartSelectExtend','SmartSelectShrink','SmartSelectExtendBlock','SmartSelectShrinkBlock','ResetEditorZoom','GotoImplementation','FindReferences','OpenFileInSolutionDialog','FindSymbolDialog','OpenCorrespondingFile','ListMethodsInCurrentFile','GotoRelated','NavigateBack','NavigateForward','RefactorRename','RefactorContextMenu','RefactorCreateFromUsage','ScopeNext','ScopePrevious']
    assert len(plan)==len(names)==22
    rows=[]
    for case,suffix in zip(plan,names):
        c=next(x for x in ca['commands'] if x['id']==case['expectedCACommand'])
        v=next(x for x in va['commands'] if x['name'].split('.',1)[-1]==suffix)
        cb=set(map(normalized,c['bindings']));vb=set(map(normalized,v['bindings']))
        wanted=normalized('Text Editor::'+case['chord'])
        rows.append(dict(chord=case['chord'],caCommand=c['name'],vaCommand=v['name'],caBindings=c['bindings'],vaBindings=v['bindings'],caEditorChordRegistered=wanted in cb,vaEditorChordRegistered=wanted in vb,editorChordMatch=wanted in cb and wanted in vb,fullBindingSetMatch=bool(cb) and cb==vb,physicalInputTested=False))
    n=len(rows);m=sum(x['editorChordMatch'] for x in rows)
    result=dict(scope='Current user-profile command registrations, not factory defaults or physical dispatch',CApid=ca['pid'],VApid=va['pid'],CAVersion='0.0.121',computerUsePerformed=False,total=n,caEditorChordRegistered=sum(x['caEditorChordRegistered'] for x in rows),vaEditorChordRegistered=sum(x['vaEditorChordRegistered'] for x in rows),editorChordMatches=m,editorChordMatchPercent=round(m*100/n,2),fullBindingSetMatches=sum(x['fullBindingSetMatch'] for x in rows),normalizerTests=tests,newPhysicalKeyTests=0,newFullFeatureCertifications=0,cases=rows)
    (OUT/'results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    table=''.join('<tr>'+''.join('<td>'+html.escape(str(x[k]))+'</td>' for k in ['chord','vaCommand','vaBindings','caCommand','caBindings','editorChordMatch'])+'</tr>' for x in rows)
    (OUT/'index.html').write_text('<html lang="ko"><meta charset="utf-8"><title>현재 단축키 등록 비교 0.0.121</title><style>body{background:#20232b;color:#eee;font:14px/1.7 system-ui;margin:28px}td,th{border:1px solid #666;padding:10px;vertical-align:top}table{border-collapse:collapse}a{color:#ffd44d}</style><h1>현재 단축키 등록 비교 — CA 0.0.121</h1><p>편집기 범위의 지정 조합 양쪽 등록 '+str(m)+'/22 ('+str(result['editorChordMatchPercent'])+'%). CA '+str(result['caEditorChordRegistered'])+'/22, VA '+str(result['vaEditorChordRegistered'])+'/22. 전체 범위·추가 키까지 같은 항목 '+str(result['fullBindingSetMatches'])+'/22.</p><p>현재 사용자 프로필의 등록 상태입니다. VA에서 키가 비어 있어도 명령이 없는 것으로 보지 않습니다. 공장 기본값이나 실제 키 실행 동등성을 뜻하지 않습니다. 이번 실제 키 입력 0건, 기능 전체 인증 0건. 화면·입력·명령 호출 없이 DTE에서 등록값만 읽었습니다.</p><table><tr><th>지정 조합</th><th>VA 명령</th><th>VA 등록</th><th>CA 명령</th><th>CA 등록</th><th>편집기 조합 일치</th></tr>'+table+'</table><p><a href="VA.json">VA 현재 원본</a> · <a href="CA.json">CA 현재 원본</a> · <a href="results.json">계산 JSON</a></p></html>',encoding='utf-8')
    print(json.dumps({k:result[k] for k in ['total','caEditorChordRegistered','vaEditorChordRegistered','editorChordMatches','editorChordMatchPercent','fullBindingSetMatches','normalizerTests']}))
if __name__=='__main__':run()
