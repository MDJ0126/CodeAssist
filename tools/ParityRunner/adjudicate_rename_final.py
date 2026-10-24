import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import save,OUT;from background_client import RUN
from adjudicate_background import update
results={}
for role in ['VA','CA']:
 folder=RUN/role;r={name:dict(exists=(folder/name).exists(),text=(folder/name).read_text() if (folder/name).exists() else None) for name in ['RenameFileAudit103.h','RenameFileAudit103.cpp','RenamedFileAudit103.h','RenamedFileAudit103.cpp']};results[role]=r;save(role+'.rename-related-file.final.bundle.json',r);print(role,r)
assert results['VA']['RenamedFileAudit103.h']['exists'] and results['VA']['RenamedFileAudit103.cpp']['exists']
# Include changes may remain unsaved in open buffer; inspect through owned document separately.
update(26,'different','소유한 프로젝트의 h/cpp 한 쌍에서 실제 이름 변경: VA는 관련 헤더와 cpp 둘 다 새 이름으로 바꿨습니다. CA는 헤더만 바꾸고 cpp 이름을 유지했습니다. include의 미저장 버퍼 및 소스 제어 반영은 이 판정에 포함하지 않습니다.',{x:x+'.rename-related-file.final.bundle.json' for x in results},dict(headerRenamedBoth=all(v['RenamedFileAudit103.h']['exists'] for v in results.values()),nativeRelatedCppRenamed=results['VA']['RenamedFileAudit103.cpp']['exists'],caRelatedCppRenamed=results['CA']['RenamedFileAudit103.cpp']['exists']))
