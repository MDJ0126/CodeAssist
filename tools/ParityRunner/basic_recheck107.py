from foreground106 import *
from adjudicate_background import update
catalog=json.loads(Path('tools/ShortcutProbe/behavior-plan.json').read_text())['cases'];paired=json.loads(Path('tools/ShortcutProbe/paired-plan.json').read_text())['cases'];ids={'scope-next':9,'pair-insert':46,'smart-select':51,'surround-word':55,'completion':44,'reset-zoom':49}
cases=[c for c in catalog if c['id']in ids]+[c for c in paired if c['id']=='dot-to-arrow']
for c in cases:
 number=ids.get(c['id'],69);records={}
 for role in ['VA','CA']:
  setup(role,'full-recheck107-'+c['id'],c['text'],c['start'],c['length']);physical(role,c['id']+'107-focus','focus');time.sleep(.6)
  if 'preZoom'in c:module(role,'owned-view-zoom107',windowTitle='',value=c['preZoom'])
  v=physical(role,c['id']+'107-action','keys',keys=c['keys'][0]);records[role]=dict(input=c,actual=v)
  if 'preZoom'in c:
   z,_=module(role,'owned-view-zoom107',windowTitle='');records[role]['zoom']=z;module(role,'owned-view-zoom107',windowTitle='',value=100)
  if c.get('verifyUndo') or c.get('undo'):records[role]['undo']=physical(role,c['id']+'107-undo','keys',keys='CTRL+Z')
  save(role+'.basic-'+c['id']+'107.bundle.json',records[role])
 a,b=[records[r]['actual']['after'] for r in ['VA','CA']];fields=['text','caret','selectionStart','selectionLength','selectedText'];same=all(a[k]==b[k]for k in fields)
 if 'preZoom'in c:same=same and records['VA']['zoom']['result']['zoom']==records['CA']['zoom']['result']['zoom']==100
 positive=any(records[r]['actual']['before']!=records[r]['actual']['after']for r in ['VA','CA']) or 'preZoom'in c
 assert positive,('Matching no-op excluded',c['id'],records)
 files={}
 for role in ['VA','CA']:
  name=role+'.basic-'+c['id']+'107.bundle.json';target=Path('docs/validation/background-103')/name;target.write_bytes((P/name).read_bytes());files[role]=name
 update(number,'representative-match' if same else 'different','이번 실제 OS 키 입력으로 대표 편집 시나리오 '+c['id']+'를 재실행했습니다. 전체 텍스트·커서·선택 범위'+(' 및 140→100 줌' if 'preZoom'in c else '')+'를 양쪽 비교했습니다. 기능 전체·모든 경계의 인증은 아닙니다.',files,dict(actualOsInputBoth=True,positiveChangeObserved=positive,representativeOutputEqual=same))
 print(c['id'],'same',same,flush=True)
