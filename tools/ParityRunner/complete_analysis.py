from pathlib import Path
import json,collections,xml.etree.ElementTree as ET,re
P=Path('C:/Projects/CodeAssist');D=P/'docs/validation/layout-audit-0.0.90'
p=D/'behavior-comparison.json';r=json.loads(p.read_text(encoding='utf-8'))
for row in r['rows']:
 if row['id'] in ['dot-to-arrow','repair-case','comment-continuation','acronym']:
  row['comparison']='inconclusive';row['reason']='Feature disabled in CA baseline or no observable completion; equal plain input does not verify feature'
 elif row['id']=='surround-word':
  row['comparison']='sample-different';row['reason']='Valid same-input command and Undo evidence; VA int(value); vs CA int (value); and selection differ'
r['counts']=dict(collections.Counter(x['comparison'] for x in r['rows']));r['determinate']=sum(x['comparison']!='inconclusive' for x in r['rows']);r['matchPercent']=round(100*r['counts']['sample-match']/r['determinate'],2);r['coveragePercent']=round(100*r['determinate']/len(r['rows']),2);p.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print('Behavior',r['counts'],r['matchPercent'])
# Published VA default/recommended key combinations, intentionally excluding native VS F12 aliases.
expected={0x11a:'ALT+K',0x116:'CTRL+SHIFT+V',0x110:'ALT+SHIFT+H',0x112:'ALT+SHIFT+]',0x113:'ALT+SHIFT+[',0x114:'ALT+]',0x115:'ALT+[',0x10f:'CTRL+0',0x100:'ALT+G',0x102:'ALT+SHIFT+F',0x103:'ALT+SHIFT+O',0x104:'ALT+SHIFT+S',0x105:'ALT+O',0x106:'ALT+M',0x107:'ALT+SHIFT+G',0x108:'ALT+LEFT',0x109:'ALT+RIGHT',0x10a:'ALT+SHIFT+R',0x10b:'ALT+SHIFT+Q',0x10c:'ALT+SHIFT+C',0x10d:'ALT+DOWN',0x10e:'ALT+UP'}
def norm(v):return '+'.join(sorted(v.replace('CONTROL','CTRL').split('+')))
tree=ET.parse(P/'CodeAssist.Extension/CodeAssistCommands.vsct');sym={x.get('name'):int(x.get('value'),0) for x in tree.getroot().iter() if x.tag.endswith('IDSymbol') and x.get('value')};actual=collections.defaultdict(list)
keymap={'VK_OEM_6':']','VK_OEM_4':'[','VK_LEFT':'LEFT','VK_RIGHT':'RIGHT','VK_UP':'UP','VK_DOWN':'DOWN','VK_F12':'F12'}
for x in tree.getroot().iter():
 if x.tag.endswith('KeyBinding'):
  key=keymap.get(x.get('key1'),x.get('key1'));mods=x.get('mod1','').replace('CONTROL','CTRL').split();actual[sym[x.get('id')]].append(norm('+'.join(mods+[key])))
rows=[{'caCommand':hex(i),'expectedKeys':keys,'actualKeys':actual[i],'combinationMatches':norm(keys) in actual[i]} for i,keys in expected.items()]
keys={'source':'https://www.wholetomato.com/documentation/configuration/keyboard-shortcuts','retrievedAt':'2026-10-18 KST','scope':'Published VA default/recommended key combinations vs current CA VSCT; not current physical input or scope equivalence','passed':sum(x['combinationMatches'] for x in rows),'total':len(rows),'physicalExecutionChecked':0,'rows':rows}
(D/'keyboard-analysis.json').write_text(json.dumps(keys,ensure_ascii=False,indent=2),encoding='utf-8');print('Keys',keys['passed'],keys['total'])
spec=json.loads((P/'docs/validation/integrated-spec.json').read_text(encoding='utf-8-sig'));plan=json.loads((P/'docs/validation/debug-0.0.90/CA.plan.json').read_text(encoding='utf-8-sig'));res=json.loads((P/'docs/validation/debug-0.0.90/CA.results.json').read_text(encoding='utf-8-sig'));byfeature={x['feature']:x for x in res['cases'][:108]};audit=[]
for f in spec['features']:
 n=int(f['id'][-3:]);row=byfeature.get(n,{})
 audit.append({'id':f['id'],'name':f['name'],'implementationScope':f.get('implementationScope'),'implementationState':f.get('implementationState'),'documentation':f.get('documentationSpec'),'requiredGates':f.get('requiredGates'),'currentRunScope':row.get('verificationScope'),'currentProbe':(row.get('probe')or{}).get('status'),'currentError':row.get('error'),'fullFeatureParityCertified':False})
assert len(audit)==108
(D/'all-108-analysis.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8');print('Audited',len(audit))
