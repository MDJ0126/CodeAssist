"""Prepare isolated VS command-chain scenarios and compare independently produced evidence.

Never equates source coverage, matching errors, or two no-ops with VA parity.
"""
import argparse
import copy
import datetime as dt
import hashlib
import html
import json
from pathlib import Path


def digest(text):
    return hashlib.sha256(text.encode('utf-8')).hexdigest()


def cases():
    base = [
        dict(id='pair-insert', feature=46, text='', start=0, length=0,
             action='paren', expectedText='()', expectedCaret=1, expectedSelectionLength=0),
        dict(id='pair-delete', feature=46, text='', start=0, length=0, setupParen=True,
             setupText='()', setupCaret=1, action='backspace', expectedText='', expectedCaret=0, expectedSelectionLength=0, verifyUndo=True),
        dict(id='surround-word', feature=55, text='int value;', start=4, length=5,
             action='paren', expectedText='int (value);', expectedSelectionLength=7, verifyUndo=True),
        dict(id='comment-continuation', feature=43, text='// note', start=7, length=0,
             action='return', expectedText='// note\n// ', verifyUndo=True),
        dict(id='reset-zoom', feature=49, text='void Sample() {}\r\n', start=0, length=0,
             action='zoom', vaCommand='VAssistX.ResetEditorZoom', caCommand=0x010f,
             expectedZoom=100, expectedText='void Sample() {}\n'),
        dict(id='smart-select', feature=51, text='void Sample() { int value = 1; }\r\n',
             start=20, length=5, action='command', vaCommand='VAssistX.SmartSelectExtend', caCommand=0x0112),
        dict(id='scope-next', feature=9, text='void First() {}\r\nvoid Second() {}\r\n',
             start=0, length=0, action='command', vaCommand='VAssistX.ScopeNext', caCommand=0x010d),
        dict(id='scope-previous', feature=9, text='void First() {}\r\nvoid Second() {}\r\n',
             start=33, length=0, action='command', vaCommand='VAssistX.ScopePrevious', caCommand=0x010e),
    ]
    # Exercise more than the single happy path; no UI or physical-key claim.
    for label, opening, closing in [('bracket','[',']'),('brace','{','}'),('quote','"','"'),('apostrophe',"'","'")]:
        base.append(dict(id='insert-'+label,feature=46,text='',start=0,length=0,
                         action='typechar',character=opening))
        base.append(dict(id='surround-'+label,feature=55,text='int value;',start=4,length=5,
                         action='typechar',character=opening))
    for label, text in [('indented','    // note'),('multiline','/* note'),('star','/*\r\n * note')]:
        base.append(dict(id='comment-'+label,feature=43,text=text,start=len(text),length=0,action='return'))
    pointer='struct AuditNode { int Value; };\r\nvoid Probe() { AuditNode* pointer = nullptr; pointer }\r\n'
    base.append(dict(id='dot-to-arrow',feature=69,text=pointer,start=pointer.rindex('pointer')+7,
                     length=0,action='typechar',character='.'))
    repair='struct AuditWidget {};\r\nvoid Probe() { auditwidget }\r\n'
    base.append(dict(id='repair-case',feature=73,text=repair,start=repair.index('auditwidget')+11,
                     length=0,action='typechar',character=' '))
    for feature,label,prefix in [(42,'acronym','ALTN'),(50,'shorthand','AuditLong'),(44,'completion','AuditLongTypeN')]:
        text='struct AuditLongTypeName {};\r\nvoid Probe() { '+prefix+' }\r\n'
        base.append(dict(id=label,feature=feature,text=text,start=text.rindex(prefix)+len(prefix),
                         length=0,action='complete',settleMs=1000))
    return base


def catalog():
    spec_path=Path(__file__).resolve().parents[2]/'docs/validation/integrated-spec.json'
    spec=json.loads(spec_path.read_text(encoding='utf-8-sig'))
    ready=full_cases()
    rows=[]
    for feature in spec['features']:
        number=int(feature['id'][-3:])
        attached=[c['id'] for c in ready if c['feature']==number]
        doc=feature.get('documentationSpec',{})
        rows.append(dict(id=feature['id'],name=feature['name'],source=doc.get('source'),
                         requiredAssertions=doc.get('observedSpec',[]),requiredGates=feature.get('requiredGates',[]),
                         scenarios=attached,adapterState='command-chain-or-ui-probe' if attached else 'adapter-required',
                         executionStatus='not-executed',fullFeatureParityPassed=False,
                         nextAction='Execute and compare actual role-isolated reports' if attached else
                         'Implement deterministic owned fixtures and an adapter for these documented assertions; opening a menu is not sufficient'))
    assert len(rows)==108 and len({r['id'] for r in rows})==108
    return dict(schema=1,denominator=108,scenarioCount=len(ready),runnableFeatureCount=sum(bool(r['scenarios']) for r in rows),
                scope='108 feature probes; config/cancellation probes are not full functional tests',features=rows)


def full_cases():
    """One explicit scoped test per feature; no full-parity claim for UI probes."""
    existing={}
    for c in cases():existing.setdefault(c['feature'],c)
    commands={
      3:('VAssistX.FindSymbolDialog',0x104),5:('VAssistX.GotoMember',0x171),
      6:('VAssistX.GotoRelated',0x107),12:('VAssistX.OpenFileInSolutionDialog',0x103),
      16:('VAssistX.RefactorChangeSignature',0x1c0),17:('VAssistX.RefactorConvertBetweenPointerAndInstance',0x18b),
      18:('VAssistX.RefactorEncapsulateField',0x181),19:('VAssistX.RefactorExtractMethod',0x18c),
      20:('VAssistX.RefactorIntroduceVariable',0x10b),21:('VAssistX.RefactorMoveImplementationToHeaderFile',0x10b),
      22:('VAssistX.RefactorMoveImplementationToSourceFile',0x10b),23:('VAssistX.RefactorMoveMethodImplementationsToSourceFile',0x1c1),
      24:('VAssistX.RefactorMoveSelectionToNewFile',0x174),25:('VAssistX.RefactorRename',0x10a),
      26:('VAssistX.RefactorRenameFiles',0x173),27:('VAssistX.RefactorSimplifyInstanceDeclaration',0x180),
      28:('VAssistX.RefactorAddForwardDeclaration',0x17a),29:('VAssistX.RefactorAddMember',0x182),
      30:('VAssistX.RefactorAddMissingCaseStatements',0x18a),31:('VAssistX.RefactorAddSimilarMember',0x183),
      32:('VAssistX.RefactorAddUsingDirective',0x184),33:('VAssistX.RefactorAddIncludeDirective',0x179),
      34:('VAssistX.BracesToggle',0x185),35:('VAssistX.RefactorCreateDeclaration',0x186),
      36:('VAssistX.RefactorCreateFile',0x172),37:('VAssistX.RefactorCreateFromUsage',0x10c),
      38:('VAssistX.RefactorCreateImplementation',0x187),39:('VAssistX.RefactorCreateMethodImplementations',0x188),
      40:('VAssistX.RefactorDocumentMethod',0x178),41:('VAssistX.RefactorImplementInterface',0x189),
      48:('VAssistX.Paste',0x116),53:('VAssistX.SortSelectedLines',0x11e),
      56:('VAssistX.VaSnippetInsert',0x175),66:('File.Print',0x194),80:('VAssistX.VaSnippetInsert',0x117),
      83:('VAssistX.VaSnippetInsert',0x175),85:('VAssistX.CreateVASnippetFromSelection',0x176),
      89:('VAssistX.AddressResolver',0x191),90:('VAssistX.PDBExplorer',0x190),91:('VAssistX.VAMemoryView',0x192),
      98:('VAssistX.OpenFileInSolutionDialog',0x193),
    }
    pages={1:'Goto',2:'Highlighting',4:'Goto',7:'Highlighting',8:'Display',10:'Goto',11:'Goto',13:'VA Hashtags',
      14:'Display',15:'Display',45:'Editor',47:'Editor',52:'Enhanced Listboxes',54:'Enhanced Listboxes',57:'Display',
      58:'Display',59:'Display',60:'Coloring',61:'Coloring',62:'Highlighting',63:'Highlighting',64:'Highlighting',
      65:'Display',67:'Display',68:'Coloring',70:'Coloring',71:'Highlighting',72:'Highlighting',74:'Underlining',75:'Underlining',
      76:'Project-Wide',77:'Per-File',78:'Per-File',79:'Project-Wide',81:'Code Generation',82:'Enhanced Listboxes',
      84:'Editor',86:'Editor',87:'Editor',88:'Editor',92:'Debug Assistance',93:'Display',94:'Display',
      95:'Source Links',96:'Source Links',97:'Source Links',99:'Source Links',100:'Source Links',
      101:'Display',102:'Projects and Files',103:'Goto',104:'Mouse',105:'Projects and Files',
      106:'C/C++ Directories',107:'Performance',108:'Editor'}
    fixtures={5:('struct Audit { int Health; void Update(); };\r\nvoid Probe() { Audit item; item.Health = 1; }\r\n',7,5),18:('class Audit { public: int Health; };\r\n',26,6),
      16:('int Sum(int left, int right) { return left + right; }\r\n',4,3),
      19:('void Probe() { int value = 1; value += 2; }\r\n',15,25),
      25:('int AuditValue = 1;\r\nint Probe(){ return AuditValue; }\r\n',4,10),
      34:('void Probe() { if (true) Call(); }\r\n',24,7),
      35:('int Sum(int left, int right) { return left + right; }\r\n',0,51),
      38:('class Audit { public: void Update(); };\r\n',22,14),
      39:('void First();\r\nvoid Second();\r\n',0,29),
      40:('int Sum(int left, int right);\r\n',0,28),
      53:('charlie\r\nalpha\r\nbravo\r\n',0,23)}
    result=[]
    for number in range(1,109):
        if number in existing:
            row=copy.deepcopy(existing[number]);row['verificationScope']='editor-command-sample'
        else:
            text,start,length=fixtures.get(number,('struct Audit { int Health; void Update(); };\r\nvoid Probe() { Audit item; item.Health = 1; }\r\n',19,6))
            # Keep malformed fixture coordinates from ever selecting another document.
            length=min(length,len(text)-start)
            command,ca=commands.get(number,('VAssistX.Options',0x141))
            row=dict(id='feature-'+str(number).zfill(3),feature=number,text=text,start=start,length=length,
                     action='dialog-probe',vaCommand=command,caCommand=ca,page=pages.get(number,''),
                     filePicker=number in (24,36,66,89,90,98),
                     verificationScope='dialog-cancel-or-direct-command' if number in commands else 'options-page-only')
        result.append(row)
    assert len(result)==108 and len({r['feature'] for r in result})==108
    return result


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')


def prepare(root):
    root = root.resolve()
    root.mkdir(parents=True, exist_ok=False)
    (root / '.codeassist-parity-owned').write_text('Generated isolated integration fixtures only.\n', encoding='utf-8')
    run_id = root.name
    for role in ('VA', 'CA'):
        fixture_root = root / role
        fixture_root.mkdir()
        (fixture_root / 'Bootstrap.cpp').write_text('void ParityBootstrap() {}\n', encoding='utf-8')
        # A unique solution per role prevents /Edit DDE from reusing the other profile.
        guid='{A6CA370E-97D9-46F7-9D13-03B31AF6F550}'
        (fixture_root / 'Parity.vcxproj').write_text('<?xml version="1.0"?><Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"><ItemGroup Label="ProjectConfigurations"><ProjectConfiguration Include="Debug|x64"><Configuration>Debug</Configuration><Platform>x64</Platform></ProjectConfiguration></ItemGroup><PropertyGroup Label="Globals"><ProjectGuid>'+guid+'</ProjectGuid><Keyword>Win32Proj</Keyword></PropertyGroup><Import Project="$(VCTargetsPath)\\Microsoft.Cpp.Default.props"/><PropertyGroup Label="Configuration"><ConfigurationType>Application</ConfigurationType><PlatformToolset>v143</PlatformToolset></PropertyGroup><Import Project="$(VCTargetsPath)\\Microsoft.Cpp.props"/><ItemGroup><ClCompile Include="Bootstrap.cpp"/></ItemGroup><Import Project="$(VCTargetsPath)\\Microsoft.Cpp.targets"/></Project>', encoding='utf-8')
        (fixture_root / 'Parity.sln').write_text('Microsoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio Version 17\nProject("{BC8A1FFA-BEE3-4634-8014-F334798102B3}") = "Parity", "Parity.vcxproj", "'+guid+'"\nEndProject\nGlobal\nGlobalSection(SolutionConfigurationPlatforms) = preSolution\nDebug|x64 = Debug|x64\nEndGlobalSection\nGlobalSection(ProjectConfigurationPlatforms) = postSolution\n'+guid+'.Debug|x64.ActiveCfg = Debug|x64\n'+guid+'.Debug|x64.Build.0 = Debug|x64\nEndGlobalSection\nEndGlobal\n', encoding='utf-8')
        write_json(root / (role + '.plan.json'), dict(runId=run_id, role=role, fixtureRoot=str(fixture_root), cases=full_cases()))
    write_json(root/'all-108.scenarios.json',catalog())
    print(root)


def canonical(snapshot):
    return {k: (v.replace('\r\n', '\n') if k == 'text' else v)
            for k, v in snapshot.items() if k in ('text', 'caret', 'selectionStart', 'selectionLength', 'selectedText', 'zoom')}


def validate_side(plan, result, case, row):
    if not result or not row:
        return 'missing-evidence'
    if result.get('schema') != 1 or result.get('runId') != plan['runId'] or result.get('role') != plan['role']:
        return 'wrong-run-or-role'
    if result.get('fatal') or row.get('error'):
        return 'execution-error'
    if row.get('inputHash') != digest(case['text']) or row.get('feature') != case['feature'] or row.get('action') != case['action']:
        return 'input-mismatch'
    if row.get('hresult') is not None and row['hresult'] < 0:
        return 'execution-error'
    before, after = row.get('before'), row.get('after')
    if not isinstance(before, dict) or not isinstance(after, dict):
        return 'missing-snapshot'
    required = {'text', 'caret', 'selectionStart', 'selectionLength', 'selectedText', 'zoom', 'activeFile'}
    if not required <= before.keys() or not required <= after.keys():
        return 'missing-snapshot-field'
    if before['text'] != case.get('setupText',case['text']) or before['selectionStart'] != case.get('setupCaret',case['start']) or before['selectionLength'] != case['length']:
        return 'setup-mismatch'
    owned_prefix=str(Path(plan['fixtureRoot'])).lower().replace('\\','/').rstrip('/')+'/'
    if any(not str(s['activeFile']).lower().replace('\\','/').startswith(owned_prefix) or
           not str(s['activeFile']).lower().replace('\\','/').endswith('/'+case['id'].lower()+'.cpp') for s in (before,after)):
        return 'wrong-active-document'
    if case['action']=='dialog-probe':
        probe=row.get('probe') or {}
        status=probe.get('status')
        if status=='product-error-dialog':return 'product-error-dialog'
        if status not in ('dialog-observed-and-cancelled','dialog-closed-without-cancel-control','popup-menu-observed-and-cancelled'):
            return 'direct-command-effect' if canonical(before)!=canonical(after) else 'no-observable-dialog-or-effect'
        if case.get('page') and not probe.get('pageSelected'):return 'requested-page-not-observed'
        if before['text']!=after['text']:return 'unexpected-edit-on-cancel'
        return 'ui-cancel-scope-only'
    if canonical(before) == canonical(after):
        return 'no-op'
    if case.get('verifyUndo'):
        undo = row.get('undo')
        if not isinstance(undo, dict) or not required <= undo.keys():
            return 'missing-undo-snapshot'
        if canonical(undo) != canonical(before):
            return 'undo-restoration-failed'
    for field, expected_key in [('text', 'expectedText'), ('caret', 'expectedCaret'),
                                ('selectionLength', 'expectedSelectionLength'), ('zoom', 'expectedZoom')]:
        if expected_key in case:
            actual, expected = canonical(after)[field], case[expected_key]
            if field == 'text':
                expected = expected.replace('\r\n', '\n')
            if actual != expected:
                return 'expectation-failed'
    return 'observed'


def compare(root):
    plans = {role: json.loads((root / (role + '.plan.json')).read_text(encoding='utf-8-sig')) for role in ('VA', 'CA')}
    results = {}
    raw = {}
    for role in plans:
        path = root / (role + '.results.json')
        result = json.loads(path.read_text(encoding='utf-8-sig')) if path.exists() else None
        # The runner hashes the exact bytes decoded as text, not reserialized JSON.
        if result and result.get('planHash') != digest((root / (role + '.plan.json')).read_bytes().decode('utf-8-sig')):
            result = dict(result, fatal='Plan changed or evidence belongs to another plan')
        raw[role] = result
        records = (result or {}).get('cases', [])
        results[role] = {r['id']: r for r in records}
        for row in records:
            sensor=root/(role+'-'+row['id']+'.dialog.json')
            if sensor.exists():
                data=json.loads(sensor.read_text(encoding='utf-8-sig'))
                if (data.get('evidenceRunId')==plans[role]['runId'] and data.get('evidenceRole')==role and
                    data.get('evidenceCaseId')==row['id'] and data.get('evidenceProcessId')==row.get('processId',result['processId'])):
                    row['probe']=data;row['probeSource']='case-bound-independent-client'
        if len(results[role]) != len(records):
            raw[role] = dict(result, fatal='Duplicate case IDs')
    if plans['VA']['runId'] != plans['CA']['runId'] or plans['VA']['cases'] != plans['CA']['cases']:
        raise ValueError('VA and CA plans must contain identical inputs, assertions and run ID')
    names={int(f['id'][-3:]):f['name'] for f in catalog()['features']}
    rows = []
    for case in plans['VA']['cases']:
        state = {r: validate_side(plans[r], raw[r], case, results[r].get(case['id'])) for r in plans}
        status = 'needs-review'
        if case['action']=='dialog-probe' and all(s=='ui-cancel-scope-only' for s in state.values()):status='ui-probe-only-not-functional-pass'
        if all(s == 'observed' for s in state.values()):
            equal = canonical(results['VA'][case['id']]['after']) == canonical(results['CA'][case['id']]['after'])
            asserted = any(k.startswith('expected') for k in case)
            status = ('scenario-match' if asserted else 'unasserted-agreement') if equal else 'different'
        rows.append(dict(id=case['id'], feature=case['feature'], featureName=names.get(case['feature'],''), verificationScope=case.get('verificationScope','editor-command-sample'), status=status, sides=state,
                         errors={r:results[r].get(case['id'],{}).get('error') for r in plans},
                         probeStatuses={r:(results[r].get(case['id'],{}).get('probe') or {}).get('status') for r in plans},
                         elapsedMs={r: results[r].get(case['id'], {}).get('elapsedMs') for r in plans}))
    report = dict(schema=1, generatedAt=dt.datetime.now(dt.timezone.utc).isoformat(), cases=rows,
                  completeReports=all(raw[role] and raw[role].get('completed') and not raw[role].get('fatal') and set(results[role])=={c['id'] for c in plans[role]['cases']} for role in plans),
                  recordedCaseCounts={role:len(results[role]) for role in plans},
                  scopeCounts={scope:sum(c.get('verificationScope','editor-command-sample')==scope for c in plans['VA']['cases']) for scope in ('editor-command-sample','dialog-cancel-or-direct-command','options-page-only')},
                  attemptedPairedFeatureIds=sorted({c['feature'] for c in plans['VA']['cases']
                      if all(results[r].get(c['id'],{}).get('before') is not None and
                             results[r].get(c['id'],{}).get('after') is not None and
                             not results[r].get(c['id'],{}).get('error') for r in plans)}),
                  executedPairedFeatures=sorted({r['feature'] for r in rows if all(s == 'observed' for s in r['sides'].values())}),
                  fullFeatureParityPassed=0, wholeProductSimilarity=None,
                  pendingFeatureIds=[n for n in range(1,109) if n not in {c['feature'] for c in plans['VA']['cases']}],
                  warning='108개 항목의 제한된 자동 프로브입니다. 옵션 페이지 확인·대화상자 취소는 기능 검증 통과가 아닙니다. 명령 후보 실패는 원본 기능 부재를 뜻하지 않습니다. 전체 기능·단축키·테마·성능 유사도는 미확인입니다.')
    write_json(root / 'comparison.json', report)
    body = ''.join('<tr>' + ''.join('<td>' + html.escape(str(r[k])) + '</td>' for k in ('id','feature','featureName','verificationScope','status','sides','probeStatuses','errors','elapsedMs')) + '</tr>' for r in rows)
    (root / 'comparison.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><title>VA·CA 자동 실행 비교</title><style>body{background:#191c22;color:#eee;font:16px/1.6 system-ui;margin:32px}td,th{border:1px solid #596170;padding:10px}table{border-collapse:collapse}h1{color:#ffd955}</style><h1>VA·CA 자동 실행 비교</h1><p>' + html.escape(report['warning']) + '</p><p>전체 유사도 미확인 · 완전 기능 인증 0/108</p><table><tr><th>시나리오</th><th>기능 ID</th><th>기능명</th><th>검증 범위</th><th>판정</th><th>양쪽 실행</th><th>UI 관찰</th><th>호출 오류</th><th>소요 ms</th></tr>' + body + '</table></html>', encoding='utf-8')
    return report


def self_test():
    import tempfile
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp) / 'run'
        prepare(root)
        plans = {r: json.loads((root / (r + '.plan.json')).read_text()) for r in ('VA','CA')}
        case_index=next(i for i,c in enumerate(plans['VA']['cases']) if c['id']=='pair-insert')
        c = plans['VA']['cases'][case_index]
        def row(role):
            file = str(Path(plans[role]['fixtureRoot']) / (c['id'] + '.cpp'))
            before = dict(text='', caret=0, selectionStart=0, selectionLength=0, selectedText='', zoom=100, activeFile=file)
            return dict(id=c['id'],feature=c['feature'],action=c['action'],inputHash=digest(c['text']),error=None,
                        before=before,after=dict(before,text='()',caret=1,selectionStart=1),elapsedMs=2)
        def result(role):
            return dict(schema=1,runId=root.name,role=role,planHash=digest((root/(role+'.plan.json')).read_bytes().decode('utf-8-sig')),completed=True,fatal=None,cases=[row(role)])
        for role in plans:
            write_json(root/(role+'.results.json'),result(role))
        assert compare(root)['cases'][case_index]['status']=='scenario-match'
        assert not compare(root)['completeReports'], 'One recorded case must not certify a full suite'
        count=1
        for expected, mutation in [
            ('no-op', lambda r:r.update(after=copy.deepcopy(r['before']))),
            ('execution-error',lambda r:r.update(error='same error on both sides')),
            ('input-mismatch',lambda r:r.update(inputHash='bad')),
            ('expectation-failed',lambda r:r['after'].update(text='(wrong)')),
            ('wrong-active-document',lambda r:r['after'].update(activeFile='user.cpp')),
            ('setup-mismatch',lambda r:r['before'].update(selectionStart=1)),
            ('missing-snapshot',lambda r:r.update(after=None)),
            ('missing-snapshot-field',lambda r:r['after'].pop('text')),
        ]:
            for role in plans:
                payload=result(role);mutation(payload['cases'][0]);write_json(root/(role+'.results.json'),payload)
            report=compare(root)
            assert report['cases'][case_index]['status']!='scenario-match'
            assert report['cases'][case_index]['sides']['VA']==expected, report
            count+=1
        payload=result('VA');payload['planHash']='stale';write_json(root/'VA.results.json',payload)
        assert compare(root)['cases'][case_index]['sides']['VA']=='execution-error';count+=1
        (root/'VA.results.json').unlink()
        assert compare(root)['cases'][case_index]['sides']['VA']=='missing-evidence';count+=1
        assert compare(root)['wholeProductSimilarity'] is None;count+=1
        print('PASS',count,'evidence-validation checks (synthetic data; NOT VA execution)')


if __name__ == '__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('mode',choices=['prepare','compare','self-test'])
    cli.add_argument('root',nargs='?',type=Path)
    args=cli.parse_args()
    if args.mode=='self-test':self_test()
    elif args.mode=='prepare':prepare(args.root)
    else:print(json.dumps(compare(args.root),ensure_ascii=False,indent=2))
