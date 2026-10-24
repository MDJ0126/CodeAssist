import sys,json
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import update,ROOT
from compiler_contracts import build_files
r={}
for role in ['VA','CA']:
 files={n:json.loads((ROOT/(role+'.new-file.'+n+'.buffer.json')).read_text(encoding='utf-8-sig'))['text'] for n in ['move-selection-new-file.cpp','move-selection-new-file-output103.h']}
 files['main.cpp']='#include "move-selection-new-file-output103.h"\nint main(){ return MovedFunction103()==7?0:1; }\n'
 # Generated header alone tests moved function; original caller deliberately excluded: default VA include-new-file option was off.
 result=build_files('new-file-selected-function',role,{k:v for k,v in files.items() if k!='move-selection-new-file.cpp'},'MovedFunction103()==7 in actual generated header')
 r[role]=dict(files=files,compiler=result,defaultIncludeOption='off' if role=='VA' else 'no chooser',sourceAndNewFileObserved=True)
 (ROOT/(role+'.new-file.bundle.json')).write_text(json.dumps(r[role],ensure_ascii=False,indent=2),encoding='utf-8')
update(24,'different','선택한 함수가 양쪽에서 실제 새 파일로 이동하고 생성 헤더 단독 컴파일·실행은 성공했습니다. VA는 #pragma once를 추가하고 include 선택 항목을 제공하며, CA의 생성 파일과 형식·대화상자 구성이 다릅니다. VA 기본 include 체크는 꺼진 상태로 비교했으며 전체 호출 원본 빌드 성공으로 확대하지 않습니다.',{x:x+'.new-file.bundle.json' for x in r},dict(actualMoveBoth=True,generatedFunctionRuntimeBoth=all(v['compiler']['passed'] for v in r.values()),generatedTextIdentical=r['VA']['files']['move-selection-new-file-output103.h']==r['CA']['files']['move-selection-new-file-output103.h']))
