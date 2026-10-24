import sys,json,subprocess,hashlib
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import read,update
ROOT=Path.cwd();OUT=ROOT/'docs/validation/background-103/compiler-check';WORK=ROOT/'.local/parity-runs/background-103/compiled';rows={}
for role in ['VA','CA']:
 folder=WORK/(role+'-create-implementation-void');folder.mkdir(exist_ok=True);archive=OUT/(role+'-create-implementation-void');archive.mkdir(exist_ok=True);header=read(role+'.create-implementation-v3.h.buffer.json')['text'];cpp=read(role+'.create-implementation-v3.cpp.buffer.json')['text'];files={'create-implementation-v3.h':header,'create-implementation-v3.cpp':cpp,'contract.cpp':'#include "create-implementation-v3.h"\nint main() { void (CreateImplementationVoidAudit::*method)() = &CreateImplementationVoidAudit::Compute; return method ? 0 : 1; }\n'}
 for name,text in files.items():(folder/name).write_bytes(text.encode('utf-8'));(archive/name).write_bytes(text.encode('utf-8'))
 dev=Path('C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/Tools/VsDevCmd.bat');script=folder/'build.cmd';script.write_text('@call "'+str(dev)+'" -no_logo -arch=x64\n@cl /nologo /EHsc /std:c++17 /W4 /Fe:program.exe create-implementation-v3.cpp contract.cpp\n',encoding='utf-8');p=subprocess.run(['cmd.exe','/d','/c',str(script)],cwd=folder,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000,timeout=25);(archive/'build.log').write_bytes(p.stdout);row=dict(compileExit=p.returncode,contract='two translation units include the header; member definition must link exactly once',files={n:hashlib.sha256((archive/n).read_bytes()).hexdigest() for n in files})
 if p.returncode==0:run=subprocess.run([str(folder/'program.exe')],cwd=folder,creationflags=0x08000000,timeout=5);row.update(runExit=run.returncode,passed=run.returncode==0)
 rows[role]=row
(OUT/'create-implementation-void-link-result.json').write_text(json.dumps(rows,indent=2));print(rows)
assert rows['VA']['passed'] and rows['CA']['compileExit']!=0
for role,which in [('VA','source'),('CA','header')]:assert read(role+'.create-implementation-v3.undo.json')['after']['text']==read('create-implementation-v3.input.json')[which]
update(38,'different','동일 헤더 멤버 선언에서 구현 생성: VA는 대응 cpp에 정의, CA는 헤더 끝에 non-inline 정의를 추가. 헤더를 두 번역 단위에서 사용하는 링크 검사: VA 통과, CA 중복 정의 LNK2005 실패. 양쪽 Undo 원문 복원.',{'VA':'VA.create-implementation-v3.after.json','CA':'CA.create-implementation-v3.after.json'},dict(implementationExists=True,targetFile=False,twoTranslationUnitLink=False,undoText=True))
