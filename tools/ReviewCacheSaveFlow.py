import pathlib,json,hashlib,re
r=pathlib.Path('.')
n=json.loads((r/'docs/reverse-engineering/va-2630/native/VA_X64.dll.native.json').read_text(encoding='utf-8-sig'))
b=pathlib.Path('C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936f/Extensions/zgy5sun1.aim/VA_X64.dll').read_bytes()
assert hashlib.sha256(b).hexdigest().lower()==n['sha256'].lower()
records=[]
for name,rv,end in [('cache-save-current.asm.txt',0x378a70,0x378cc2),('cache-serialize-current.asm.txt',0x3795a0,0x379800)]:
 p=r/'tools/obj/rea'/name
 text=p.read_text(encoding='utf-8-sig');assert hex(0x1ed000000+rv)[2:]+':' in text.lower()
 records.append({'functionRva':hex(rv),'unwindEndRva':hex(end),'assemblySHA256':hashlib.sha256(p.read_bytes()).hexdigest(),'artifact':str(p),'provider':'LLVM objdump from VS2022; not REA native provider','executedTarget':False})
strings=[]
for rv in [0xe1a8a8,0xe0c85c,0xe1a944,0xe1a930]:
 sec=next(s for s in n['sections'] if s['rva']<=rv<s['rva']+s['raw_size']);off=sec['raw_offset']+rv-sec['rva']
 value=b[off:off+200].decode('utf-16le',errors='replace').split(chr(0))[0]
 strings.append({'rva':hex(rv),'fileOffset':hex(off),'encoding':'utf-16le','value':value})
assert strings[-1]['value']=='%s§%s§%x\n'
a={'artifactSHA256':n['sha256'],'functions':records,'literalFormats':strings,'observations':[{'instructionRva':'0x378a85','fact':'Early return if dword at pointer from object+0x58 minus0x10 is zero; exact string/object type unverified'},{'instructionRva':'0x378a96','fact':'Early return if global pointer 0x127c780 null or byte at pointed object+0x1ec9 zero; option identity unverified'},{'instructionRva':'0x378b6b','fact':'Pass container at object+0x00 to function RVA0x3795a0'},{'instructionRva':'0x378c3c','fact':'Pass container at object+0x10 to same function RVA0x3795a0'},{'instructionRva':'0x379676','fact':'Formatting helper receives UTF16 record template %s§%s§%x followed by newline'},{'instructionRva':'0x37968d','fact':'Indirect call at vtable+0x78 receives character length doubled; concrete stream class and write implementation unverified'}],'unknowns':['Meaning of hex header and record fields','Concrete writer type, paths, atomicity and errors','Filtering status at node+0x28','Load parser and invalidation lifecycle','Whether cache concerns files/includes only or semantic symbols','Runtime serialized bytes not captured'],'fullParityPassed':False}
(r/'docs/validation/native-cache-save-flow.json').write_text(json.dumps(a,ensure_ascii=False,indent=2),encoding='utf-8')
p=r/'tools/BuildParitySpec.py';s=p.read_text(encoding='utf-8');needle="(r/'docs/integrated-spec.md').write_text"
insert="savePath=r/'docs/validation/native-cache-save-flow.json'\nif savePath.exists():\n data['nativeCacheSaveFlow']='native-cache-save-flow.json'\n (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')\n lines+=['','## 캐시 저장 호출 경로','','[원본 범위 디스어셈블 근거](validation/native-cache-save-flow.json): RVA 0x378A70 및 0x3795A0을 설치 DLL에서 LLVM으로 다시 읽었습니다. 원본 실행이나 REA native provider 실행은 하지 않았습니다.','', '호출부는 object+0x00과 object+0x10의 두 컨테이너를 같은 함수에 전달합니다. callee는 UTF-16 형식 문자열 `%s§%s§%x` 및 newline을 formatting helper에 전달하고, 길이를 두 배로 하여 vtable+0x78의 간접 호출에 넘깁니다. 실제 필드 의미·writer·파일 경로·전체 형식은 미확인입니다.','', '조건부 early return에는 object+0x58에서 얻은 pointer의 길이 후보 및 global 객체의 +0x1EC9 byte가 사용됩니다. 해당 byte를 특정 옵션 이름으로 단정하지 않습니다. load·무효화·오류 처리·실제 출력 bytes 비교는 남아 있습니다.']\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
p.write_text(s,encoding='utf-8')
print('Validated original hash, two function address anchors, four UTF16 format literals')
