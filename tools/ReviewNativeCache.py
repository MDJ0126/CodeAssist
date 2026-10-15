import pathlib,json,hashlib
r=pathlib.Path('.')
n=json.loads((r/'docs/reverse-engineering/va-2630/native/VA_X64.dll.native.json').read_text(encoding='utf-8-sig'))
p=pathlib.Path('C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936f/Extensions/zgy5sun1.aim/VA_X64.dll')
assert hashlib.sha256(p.read_bytes()).hexdigest().lower()==n['sha256'].lower()
old=json.loads((r/'docs/reverse-engineering/va-2630/native/decompilation-evidence.json').read_text(encoding='utf-8-sig'))
g=next(x for x in old if x['Group']=='core')
selected={0x378360:'FileFinder statistics reader; not cache lookup or persistence algorithm',0x1722b0:'BCL cache warnings; object/context meaning unverified',0x18f220:'Spaghetti.db path passed to callee; database format and semantic role unverified',0x2956b0:'cache and netImport directory strings; initialization and persistence semantics unverified'}
records=[]
for rv,scope in selected.items():
 entry=next(x for x in g['Functions'] if int(x['RVA'],16)==rv)
 raw=(r/'tools/obj/ghidra-core'/entry['Artifact']).read_bytes()
 assert hashlib.sha256(raw).hexdigest().lower()==entry['SHA256'].lower()
 records.append({'rva':hex(rv),'artifact':entry['Artifact'],'sha256':entry['SHA256'],'scope':scope})
xrefs=[x for x in n['string_xrefs'] if x['function_start']==0x378360]
targets=[{'functionRva':hex(x['function_start']),'instructionRva':hex(x['instruction_rva']),'stringRva':hex(x['string_rva']),'reason':'Cache-save trace anchor only; algorithm not decompiled'} for x in n['string_xrefs'] if 'FileFinder cache save' in x['text']]
result={'artifactSHA256':n['sha256'],'imageBase':n['image_base'],'evidenceKind':'Earlier Ghidra decompilation plus LLVM string xrefs; revalidated original and pseudocode hashes; not a new REA native-provider run','reviewedFunctions':records,'fileFinderStatsXrefCount':len(xrefs),'observations':['Statistics function reads cache-hit/miss, filesystem avoidance and include-resolution counters.','Four entry counts are reported for main/local caches with and without relative-directory context.','Shared SRW lock protects traversal when collecting directory/entry statistics.','A registry-reading helper is invoked for NewFileFinderCacheTest in a one-time initialization branch; flag meaning/default runtime unverified.'],'nextNativeTargets':targets,'unknowns':['Cache key and directory-context normalization','Entry creation, invalidation, stale-file detection and lookup algorithm','Disk save/load format, startup lifecycle and thread budget','Relation to semantic symbol/reference index','Whether all references are pre-baked','CPU100 origin in live VS'],'runtimeCompared':False,'fullParityPassed':False}
(r/'docs/validation/native-cache-review.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
p=r/'tools/BuildParitySpec.py';s=p.read_text(encoding='utf-8');needle="(r/'docs/integrated-spec.md').write_text"
insert="cachePath=r/'docs/validation/native-cache-review.json'\nif cachePath.exists():\n cache=json.loads(cachePath.read_text(encoding='utf-8'))\n data['nativeCacheReview']='native-cache-review.json'\n (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')\n lines+=['','## 네이티브 캐시 근거 검토','','[원본·재구성 해시 대조 장부](validation/native-cache-review.json): 기존 Ghidra 결과 4개를 현재 VA_X64.dll 해시와 대조했습니다. REA native provider를 새로 실행한 결과는 아닙니다.','']\n lines+=['- '+x for x in cache['observations']]\n lines+=['','FileFinder 통계 함수는 hit/miss·filesystem 호출 회피·include 해석·main/local 및 relative-directory 구분 카운터를 읽습니다. 캐시 lookup·무효화·save/load 알고리즘을 입증하지 않습니다. 저장 추적 문자열의 함수 RVA를 다음 분석 대상으로 특정했습니다.','', '전체 참조 사전 베이크, 설치 파일 크기와 참조 인덱스의 관계, CPU 100% 원인은 미확인입니다.']\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
p.write_text(s,encoding='utf-8')
print('Original hash and 4 pseudocode hashes matched; stats xrefs:',len(xrefs),'next save targets:',targets)
