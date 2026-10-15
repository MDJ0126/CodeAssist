import pathlib,json,hashlib
r=pathlib.Path('.')
# Analyst reconstruction of a leaf calculation, not original execution.
def leaf(value):
 raw=value.encode('utf-16le',errors='surrogatepass');h=0
 for i in range(0,len(raw),2):
  c=int.from_bytes(raw[i:i+2],'little')
  if c==0:break
  product=(h*129)&0xffffffff
  if 65<=c<=90:
   packed=(((h+c)&0xffffffff)<<24)&0x7f000000
   h=(((packed|c)+32+product)&0xffffffff)|0x10000000
  else:h=(product+c)&0xffffffff
 return h
samples=[{'inputKind':label,'reconstructionValue':hex(leaf(value))} for label,value in [('empty',''),('lowercase-a','a'),('uppercase-A','A'),('lowercase-ab','ab'),('uppercase-Ab','Ab'),('semicolon-separated','a;b'),('nul-terminated','a\x00b')]]
assert leaf('a\x00b')==leaf('a') and leaf('')==0 and leaf('A')!=leaf('a')
m=json.loads((r/'tools/obj/rea/va-import-map.json').read_text())
assert m['0xdb6fa0']['symbol']=='EnterCriticalSection'
a={'scope':'Bounded original LLVM disassembly; analyst reconstruction not original execution','artifactSHA256':json.loads((r/'docs/validation/native-cache-save-flow.json').read_text())['artifactSHA256'],'functions':[{'rva':'0x32b250','endRetRva':'0x32b29d','artifact':'cache-header-key.asm.txt','sha256':hashlib.sha256((r/'tools/obj/rea/cache-header-key.asm.txt').read_bytes()).hexdigest()},{'rva':'0x3780e0','unwindEndRva':'0x378246','artifact':'cache-reset.asm.txt','sha256':hashlib.sha256((r/'tools/obj/rea/cache-reset.asm.txt').read_bytes()).hexdigest()}],'findings':['Header calculation is a 32-bit rolling operation over NUL-terminated UTF16 units; ordinary branch h=129*h+unit modulo2^32.','ASCII uppercase A-Z takes special packed-bit branch; uppercase and lowercase do not simply normalize to the same hash.','Reset enters critical section at object+0x30 and clears two container sentinels plus size fields at object+0x08 and +0x18.','Reset calls context-producing helpers and combines outputs with semicolon literal into object+0x60. Exact context meaning unverified.','Previously observed loader compares loaded dword to this header calculation and resets on mismatch; comparison is not sufficient to prove mtime/content fingerprint.'],'reconstructionSamples':samples,'sampleExecutionScope':'Independent Python leaf reconstruction only; not native oracle or parity pass','unknowns':['Meaning and provenance of context-producing helper outputs','All loader parse fields and corrupt input rules','Header collisions and invalidation coverage','Thread model and lock release on every exception path','Cache relation to semantic reference index'],'fullParityPassed':False}
(r/'docs/validation/native-cache-header-reset.json').write_text(json.dumps(a,ensure_ascii=False,indent=2),encoding='utf-8')
p=r/'tools/BuildParitySpec.py';s=p.read_text(encoding='utf-8');needle="(r/'docs/integrated-spec.md').write_text"
insert="headerPath=r/'docs/validation/native-cache-header-reset.json'\nif headerPath.exists():\n h=json.loads(headerPath.read_text(encoding='utf-8'))\n data['nativeCacheHeaderReset']='native-cache-header-reset.json'\n (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')\n lines+=['','## キャ시 헤더 계산·초기화','','[함수별 근거](validation/native-cache-header-reset.json): RVA0x32B250은 NUL 종료 UTF-16 단위를 처리하는 32bit rolling 연산입니다. 일반 경로는 129배 누적이며 ASCII 대문자는 별도 packed-bit 처리를 합니다. 대소문자를 단순 정규화하는 hash라고 부르지 않습니다.','', 'RVA0x3780E0은 EnterCriticalSection을 호출하고 두 container의 sentinel/size를 초기화합니다. context helper 출력을 세미콜론으로 결합해 object+0x60을 갱신합니다. helper 출력의 의미와 전체 취소·예외 경로는 미확인입니다.','', 'Python 재구성의 7개 입력 실행은 원본 코드 실행·동등성 검증이 아닙니다. 이 자료로 파일 timestamp/content 변경을 모두 감지한다고 단정하지 않습니다.']\n"
insert=insert.replace('キャ시','캐시')
if insert not in s:s=s.replace(needle,insert+needle,1)
p.write_text(s,encoding='utf-8')
print('Header reconstruction samples',len(samples),'critical-section import verified; no original execution')
