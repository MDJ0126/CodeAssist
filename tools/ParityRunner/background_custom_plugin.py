import sys,json,hashlib
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import RUN,module;from adjudicate_background import update
file=RUN/'custom-plugin/ParitySourceLink103.slp';sha=hashlib.sha256(file.read_bytes()).hexdigest();r={}
for role in ['VA','CA']:
 v,_=module(role,'plugin-contract-query',windowTitle='',file=str(file),fileSha256=sha);save(role+'.custom-plugin-contract.json',v);print(role,v,flush=True);assert v['status']=='complete';r[role]=v
assert r['VA']['result']['url']=='https://example.invalid/custom/103'
update(100,'different','소유한 무해한 Source Links 플러그인을 원본 IPluginFactory/IPlugin 계약으로 실제 빌드하고 VA 호스트에서 정규식·툴팁·URL 메서드를 실행했습니다. 같은 플러그인 타입은 CA의 ICodeAssistPlugin 계약과 호환되지 않으며 CA 계약은 텍스트 편집 액션용입니다. 원본의 설치 검색·분리 AppDomain 검증기까지 인증한 결과는 아니고 외부 실행은 호출하지 않았습니다.',{x:x+'.custom-plugin-contract.json' for x in r},dict(nativeFactoryMethodsExecuted=True,nativeUrlExpected=True,caSourceLinksContractCompatible=r['CA']['result']['caContractCompatible'],externalExecutionExcluded=True))
