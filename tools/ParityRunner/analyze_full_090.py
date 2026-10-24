from pathlib import Path
import json,importlib.util,collections,html,shutil,xml.etree.ElementTree as ET,hashlib,zipfile
P=Path('C:/Projects/CodeAssist');R=P/'.local/parity-runs/20261018-113400-full-090';D=P/'docs/validation/debug-0.0.90';D.mkdir(exist_ok=True,parents=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
sp=importlib.util.spec_from_file_location('parity',P/'tools/ParityRunner/parity.py');m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m)
r=read(R/'CA.results.json');plan=read(R/'CA.plan.json');assert r['completed'] and not r.get('fatal')
rows={x['id']:x for x in r['cases']};valid=[]
for c in plan['cases']:
 x=rows.get(c['id']);state=m.validate_side(plan,r,c,x);valid.append({'id':c['id'],'scope':c.get('verificationScope'),'action':c['action'],'status':state,'hasExplicitExpected':any(k.startswith('expected') for k in c),'feature':c['feature']})
(D/'functional-checks.json').write_text(json.dumps(valid,ensure_ascii=False,indent=2),encoding='utf-8')
O=P/'docs/validation/automated-runner-20261018/20261018-021854-OptionsVerified';vp=read(O/'VA.plan.json');vr={x['id']:x for x in read(O/'VA.results.json')['cases']};reverse={}
for e in ET.parse(P/'CodeAssist.Core/Localization/ui.xml').getroot():
 english=next((t.text for t in e if t.get('lang')=='en'),e.get('source',''))
 for t in e:
  if t.text:reverse[t.text]=english
reverse.update({'Display CodeAssist Navigation Bar':'Display VA Navigation Bar','CodeAssist Hashtags':'VA Hashtags'})
def controls(c):
 out=set()
 for item in ((c.get('probe') or {}).get('controls') or []):
  if not isinstance(item,dict):continue
  if item.get('type','').split('.')[-1] not in ['CheckBox','RadioButton','ComboBox','TextBox','Edit']:continue
  name=item.get('name') or ''
  if name:out.add(reverse.get(name,name).replace('_','').replace('CodeAssist','VA'))
 return out
matrix=[]
for v in vp['cases']:
 choices=[x for x in plan['cases'][:108] if x.get('page')==v['page']];c=next((rows[x['id']] for x in choices if (rows[x['id']].get('probe') or {}).get('pageSelected')),None)
 a=controls(vr[v['id']]);b=controls(c or {});matrix.append({'page':v['page'],'vaNamedControls':sorted(a),'caNamedControls':sorted(b),'labelOnlyMatches':sorted(a&b),'unmatchedVaLabels':sorted(a-b),'caPageObserved':bool(c),'note':'Label inventory only; unmatched wording is not proof of missing implementation'})
(D/'options-matrix.json').write_text(json.dumps(matrix,ensure_ascii=False,indent=2),encoding='utf-8')
for n in ['CA.results.json','CA.plan.json']:shutil.copy2(R/n,D/n)
for f in (R/'screenshots').iterdir():shutil.copy2(f,D/f.name)
counts=collections.Counter(x['status'] for x in valid)
summary={'version':'0.0.90','completed':r['completed'],'scenarioCount':len(valid),'catalogFeatureProbes':108,'additionalCases':len(valid)-108,'functionalStates':dict(counts),'explicitExpectedCases':sum(x['hasExplicitExpected'] for x in valid),'explicitExpectedObserved':sum(x['hasExplicitExpected'] and x['status']=='observed' for x in valid),'optionsPagesCompared':len(matrix),'optionsPagesObserved':sum(x['caPageObserved'] for x in matrix),'unitTestsPassed':272,'wholeProductSimilarity':None,'caRun':str(R),'vaReferenceRun':str(O),'vaFreshRunBlockedByApproval':True}
with zipfile.ZipFile(P/'installers/0.0.90/CodeAssist.vsix') as z:summary['dllSHA256']=hashlib.sha256(z.read('CodeAssist.Extension.dll')).hexdigest()
(D/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
