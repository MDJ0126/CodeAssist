import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,launch,OUT;from background_client import RUN
from adjudicate_background import update
h=json.loads((RUN/'hosts.json').read_text());results={}
for role in ['VA','CA']:
 folder=RUN/role;new='RenamedFileAudit103';assert not (folder/(new+'.h')).exists() and not (folder/(new+'.cpp')).exists()
 spec=OUT/(role+'.rename-related-file.commit.spec.json');save(spec.name,dict(title='Rename' if role=='VA' else 'CodeAssist',edit=new if role=='VA' else new+'.h',editMatch='RenameFileAudit103' if role=='VA' else 'RenameFileAudit103.h',button='Rename' if role=='VA' else 'Save',privateFocus=True))
 q=launch(['drive',h[role]['pid'],OUT/(role+'.rename-related-file.commit.json'),spec]);q.wait(timeout=15);time.sleep(.4)
 r={name:dict(exists=(folder/name).exists(),text=(folder/name).read_text() if (folder/name).exists() else None) for name in ['RenameFileAudit103.h','RenameFileAudit103.cpp',new+'.h',new+'.cpp']};results[role]=r;save(role+'.rename-related-file.bundle.json',r);print(role,r,flush=True)
update(26,'different','소유한 프로젝트의 h/cpp 한 쌍을 실제 이름 변경했습니다. VA는 선택된 관련 헤더·cpp 둘 다 새 이름으로 바꾸고 cpp의 include도 갱신했습니다. CA는 헤더만 변경하고 cpp 이름·include를 유지했습니다. 소스 제어 반영은 별도 미검증입니다.',{x:x+'.rename-related-file.bundle.json' for x in results},dict(headerRenamedBoth=all(v['RenamedFileAudit103.h']['exists'] for v in results.values()),nativeRelatedCppRenamed=results['VA']['RenamedFileAudit103.cpp']['exists'],caRelatedCppRenamed=results['CA']['RenamedFileAudit103.cpp']['exists']))
