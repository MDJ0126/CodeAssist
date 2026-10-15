import pathlib,json,collections,datetime,hashlib
r=pathlib.Path('.')
p=pathlib.Path('C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936f/Extensions/zgy5sun1.aim/Data/vs17_1')
g=collections.defaultdict(lambda:{'files':0,'bytes':0});samples=[];ext=collections.Counter();files=[]
for f in p.rglob('*'):
 if not f.is_file():continue
 try:
  st=f.stat();rel=f.relative_to(p);z=st.st_size
 except OSError:continue
 files.append((f,rel,z));g[rel.parts[0]]['files']+=1;g[rel.parts[0]]['bytes']+=z;ext[f.suffix.lower()]+=1
for f,rel,z in files:
 if str(rel).replace('\\','/') in ['fileids.va','ProjectInfo/7bd8209f.va','cpp/Db0Ds.db','cpp/Db0Idx.db']:
  before=f.stat()
  with f.open('rb') as stream:raw=stream.read(4096)
  after=f.stat()
  samples.append({'relativePath':str(rel).replace('\\','/'),'length':z,'prefixLength':len(raw),'prefixSHA256':hashlib.sha256(raw).hexdigest(),'stableMetadataDuringRead':before.st_size==after.st_size and before.st_mtime_ns==after.st_mtime_ns,'bom':'UTF16LE' if raw.startswith(bytes([255,254])) else 'UTF8' if raw.startswith(bytes([239,187,191])) else 'none','utf16SectionDelimiterCount':raw.count(bytes([167,0])),'asciiNewlineCount':raw.count(bytes([10])),'scope':'4096-byte prefix only; no source content or private paths copied'})
a={'observedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'root':str(p),'scope':'Passive existing VA data directory; target not launched or modified','fileCount':len(files),'totalBytes':sum(z for _,_,z in files),'groupStats':dict(g),'extensionCounts':dict(ext),'prefixSamples':samples,'fileFinderSerializationMatched':False,'limitations':['Files may be stale, partially written or from earlier sessions.','Timestamp and file naming do not establish producer function or semantic schema.','Nonempty data does not prove all references pre-baked or packaged.','No cache creation, invalidation, startup or UI comparison executed.','Only this vs17_1 data root surveyed; absence here is not global absence.']}
(r/'docs/validation/passive-va-data-snapshot.json').write_text(json.dumps(a,ensure_ascii=False,indent=2),encoding='utf-8')
p=r/'tools/BuildParitySpec.py';s=p.read_text(encoding='utf-8');needle="(r/'docs/integrated-spec.md').write_text"
insert="snapshotPath=r/'docs/validation/passive-va-data-snapshot.json'\nif snapshotPath.exists():\n snap=json.loads(snapshotPath.read_text(encoding='utf-8'))\n data['passiveVADataSnapshot']='passive-va-data-snapshot.json'\n (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')\n lines+=['','## 실제 VA 데이터 경로 읽기 전용 관찰','','[수동 관찰 장부](validation/passive-va-data-snapshot.json): 설치 확장 Data/vs17_1에서 '+str(snap['fileCount'])+'개 파일, '+str(snap['totalBytes'])+' bytes를 관찰했습니다. VA 실행·UI 조작·파일 변경 없이 파일 메타데이터와 지정한 4개 파일의 최대4096byte prefix 해시/형식 지표만 기록했습니다. 내용·개인 경로를 복사하지 않았습니다.','', '| 그룹 | 파일 수 | bytes |','|---|---|---|']\n for group,stats in snap['groupStats'].items():\n  lines.append('| '+group+' | '+str(stats['files'])+' | '+str(stats['bytes'])+' |')\n lines+=['','cpp 데이터는 존재하지만 전체 참조 베이크 여부·생성 함수·정확한 스키마·FileFinder 저장과의 연결은 미확인입니다. 기존 파일의 존재는 현재 알고리즘·성능·동등성 검증을 대신하지 않습니다.']\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
p.write_text(s,encoding='utf-8')
print('Passive snapshot',len(files),'files',a['totalBytes'],'bytes; stable prefixes',sum(x['stableMetadataDuringRead'] for x in samples),'/',len(samples))
