import sys,json,time,subprocess,hashlib,base64
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN;from background_steps import OUT,save;from adjudicate_background import update
# Actual external command-line build: the header stays outside each source/project directory.
work=RUN/'compiled/external-build103';work.mkdir(exist_ok=True);header=RUN/'shared-custom103/PlatformHeader103.h';buildResults={}
for role in ['VA','CA']:
 source=RUN/role/'external-build-lookup.cpp';exe=work/(role+'.exe');obj=work/(role+'.obj');script=work/(role+'-build.cmd');script.write_text('@call "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\Tools\\VsDevCmd.bat" -no_logo -arch=x64\n@cl /nologo /EHsc /std:c++17 /I"'+str(header.parent)+'" /Fe:"'+str(exe)+'" /Fo:"'+str(obj)+'" "'+str(source)+'"\n');p=subprocess.run(['cmd.exe','/d','/c',str(script)],cwd=work,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000,timeout=25);(OUT/('compiler-check/'+role+'-external-build.log')).write_bytes(p.stdout);assert p.returncode==0,p.stdout;r=subprocess.run([str(exe)],creationflags=0x08000000,timeout=5);buildResults[role]=dict(compileExit=p.returncode,runExit=r.returncode,sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),headerSha256=hashlib.sha256(header.read_bytes()).hexdigest(),includeDirectory=str(header.parent));assert r.returncode==0
save('compiler-check/external-build-result.json',buildResults)
def opt(op,**kw):
 for n in range(30):
  v,_=module('VA',op,windowTitle='Visual Assist Options',**kw)
  if v.get('error'):raise RuntimeError(v['error'])
  if (v.get('result') or {}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
for kind in [1,0]:
 module('VA','open-options',windowTitle='');opt('options-read',page='C/C++ Directories');opt('options-edit',setting='DirectoryPlatform',newText='Custom');opt('options-edit',setting='DirectoryKind',index=kind);v=opt('options-read',inspectDirectoryRows=True);paths=[x['path'] for x in v['result']['lists'][0]['items']];assert paths==[str(header.parent)],paths;opt('options-edit',setting='CustomDirectory',action='remove',path=str(header.parent));opt('options-button',button='Save');module('VA','open-options',windowTitle='');opt('options-read',page='C/C++ Directories');opt('options-edit',setting='DirectoryKind',index=kind);v=opt('options-read',inspectDirectoryRows=True);save('VA.external-build.restored-group'+str(kind)+'.json',v);assert v['result']['lists'][0]['items']==[];opt('options-button',button='Cancel')
module('VA','open-options',windowTitle='');opt('options-read',page='C/C++ Directories');opt('options-edit',setting='DirectoryKind',index=0);opt('options-edit',setting='DirectoryPlatform',newText='Project defined');opt('options-button',button='Save');module('VA','open-options',windowTitle='');v=opt('options-read',page='C/C++ Directories',inspectDirectoryRows=True);save('VA.external-build.final-platform.json',v);assert v['result']['combos'][1]['selected']==1;opt('options-button',button='Cancel')
j=json.loads((OUT/'external-build.flags-transaction.json').read_text());path=Path(j['path']);assert path==RUN/'CA/compile_flags.txt';expected=('-std=c++17\n-I'+str(header.parent).replace('\\','/')+'\n').encode();assert path.read_bytes().replace(b'\r\n',b'\n')==expected
if j['original'] is None:path.unlink()
else:path.write_bytes(base64.b64decode(j['original']))
j.update(caFlagsRestored=True,nativeSystemAndSourcePathsRestored=True,nativeProjectDefinedRestored=True);save('external-build.flags-transaction.json',j)
for role in ['VA','CA']:
 after=json.loads((OUT/('VA.external-build.source-dir-after.json' if role=='VA' else 'CA.external-build-lookup.after.json')).read_text());assert after['result']['dteFile']==str(header);save(role+'.external-build.bundle.json',dict(after=after,configuration=json.loads((OUT/('VA.external-build.after-restart-options.json' if role=='VA' else 'CA.external-build-lookup.queued.json')).read_text()),nativeSourceDirectoryAdded=json.loads((OUT/'VA.external-build.add-source-dir.json').read_text()),buildResults=buildResults,restored=j,scope='actual configured external header navigation and external command-line compilation; caret positions differ'))
update(106,'different','VA는 Custom의 System include·Source files 경로 설정과 재파싱 후, CA는 compile_flags.txt의 외부 -I 설정으로 같은 외부 헤더에 실제 도착했습니다. 외부 경로를 사용한 실제 명령줄 빌드·실행도 양쪽 통과했습니다. 설정 방식과 정의 커서 위치(VA 39, CA 20)가 달라 완전 일치로 세지 않았습니다. 테스트 경로 두 그룹과 CA 플래그는 원상복구했습니다.',{x:x+'.external-build.bundle.json' for x in ['VA','CA']},dict(actualExternalHeaderReachedBoth=True,actualExternalCommandLineBuildPassedBoth=True,actualExecutablePassedBoth=True,configurationWorkflowEquivalent=False,definitionCaretEquivalent=False,settingsRestored=True))
