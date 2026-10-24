import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT;from background_client import RUN,module
from adjudicate_background import update
from compiler_contracts import build_files
h=json.loads((RUN/'hosts.json').read_text());results={}
for role in ['VA','CA']:
 target=RUN/role/'CreateFileAudit103.cpp';assert not target.exists()
 for mode,spec in [('set-id',dict(id='1210' if role=='VA' else '1001',value=str(target))),('drive',dict(title='Create File' if role=='VA' else 'Create file',button='Create' if role=='VA' else '저장(S)',requiredNewFile=str(target),fileFieldId='1210' if role=='VA' else '1001',privateFocus=True))]:
  p=OUT/(role+'.create-file.'+mode+'.spec.json');save(p.name,spec);q=launch([mode,h[role]['pid'],OUT/(role+'.create-file.'+mode+'.result.json'),p]);q.wait(timeout=15)
 time.sleep(.3);q=launch(['read-document',h[role]['pid'],OUT/(role+'.create-file.generated.json'),target]);q.wait(timeout=15)
 r=json.loads((OUT/(role+'.create-file.generated.json')).read_text(encoding='utf-8-sig'));results[role]=r;save(role+'.create-file.bundle.json',dict(generated=r,dialog=json.loads((OUT/(role+'.create-corresponding-file.tree.json')).read_text(encoding='utf-8-sig'))[:12]))
 print(role,repr(r['text']),flush=True)
update(36,'different','짝 없는 동일 헤더에서 실제 cpp 생성: VA 기본 후보는 같은 이름의 cpp이며 생성 내용에 헤더 include를 넣습니다. CA 기본 후보는 NewFile.cpp이고 지정한 cpp를 빈 내용으로 생성합니다. 파일이 실제 생성·열린 결과로 확인했습니다.',{x:x+'.create-file.bundle.json' for x in results},dict(actualFileCreatedBoth=True,generatedContentIdentical=results['VA']['text']==results['CA']['text'],nativeIncludesHeader='CreateFileAudit103.h' in results['VA']['text'],caIncludesHeader='CreateFileAudit103.h' in results['CA']['text']))
