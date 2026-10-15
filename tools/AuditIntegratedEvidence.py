import pathlib,json,hashlib,datetime
r=pathlib.Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().lower()
s=read(r/'docs/validation/integrated-spec.json');checks=[];failures=[]
def check(label,passed):
 checks.append({'check':label,'passed':bool(passed)})
 if not passed:failures.append(label)
check('108 unique feature IDs',len(s['features'])==len({x['id'] for x in s['features']})==108)
check('Documented partial count matches actual rows',sum(bool(x['documentationSpec']) for x in s['features'])==s['documentedPartialCount'])
check('No unsupported full parity claim',s['fullParityCount']==0 and not any(x['fullParityPassed'] for x in s['features']))
links={k:v for k,v in s.items() if isinstance(v,str) and v.endswith('.json')}
for k,v in links.items():check('Evidence link '+k,(r/'docs/validation'/v).is_file())
installed=pathlib.Path(read(r/'docs/validation/va-installed-help-catalog.json')['installedRoot'])
for item in read(r/'docs/validation/installed-help-defaults.json')['defaults']:
 check('Installed help hash '+item['resource'],sha(installed/item['resource'])==item['sha256'].lower())
key=read(r/'docs/validation/rea/keybinding-reconstruction-summary.json')
check('Keybinding exact original DLL hash',sha(pathlib.Path(key['subject']['local_path']))==key['subject']['digest']['sha256'].lower())
decomp=read(r/'tools/obj/rea/keybinding-decompiler.json')
for method in decomp['Methods']:check('Key reconstruction '+method['Token'],sha(pathlib.Path(method['SourcePath']))==method['SourceSHA256'].lower())
for name in ['native-cache-save-flow.json','native-cache-open-load-flow.json','native-cache-header-reset.json','native-cache-parser-rules.json']:
 doc=read(r/'docs/validation'/name)
 check(name+' exact original DLL hash',sha(installed/'VA_X64.dll')==doc['artifactSHA256'].lower())
 entries=doc.get('functions',doc.get('artifacts',[]))
 for item in entries:
  raw=item.get('artifact',item.get('file'))
  if raw is None:continue
  p=pathlib.Path(raw)
  if not p.is_absolute():p=r/p if raw.startswith('tools') else r/'tools/obj/rea'/p
  expected=item.get('sha256',item.get('assemblySHA256'))
  check(name+' analysis artifact '+p.name,expected is not None and sha(p)==expected.lower())
html=(r/'docs/integrated-spec.html').read_text(encoding='utf-8')
for name in ['native-cache-parser-rules.json','rea/keybinding-reconstruction-summary.json','installed-help-defaults.json']:
 check('HTML evidence link '+name,name in html)
report={'observedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'Evidence provenance and integrated-ledger consistency audit only; no VA runtime parity tests','checks':checks,'passedCount':sum(x['passed'] for x in checks),'failedCount':len(failures),'featureCount':108,'documentedPartialCount':s['documentedPartialCount'],'runtimeEvidenceRows':sum(bool(x['runtimeEvidence']) for x in s['features']),'fullParityCount':s['fullParityCount'],'limitations':['Passing provenance does not prove interpretation correctness or functional equivalence.','Raw reconstruction remains analyst inference.','No native provider or UI/runtime comparison executed by this audit.']}
(r/'docs/validation/evidence-audit-summary.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Evidence checks',report['passedCount'],'passed,',report['failedCount'],'failed; runtime rows',report['runtimeEvidenceRows'])
if failures:raise SystemExit('; '.join(failures))
