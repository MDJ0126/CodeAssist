import sys,json,hashlib
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import ROOT,read,update
fixture=read('pdb-fixture.json');files={}
for role,source in [('VA','VA.pdb-explorer.filtered-tree.json'),('CA','CA.pdb-explorer.output-tree.json')]:
 rows=read(source);texts=[str(x.get('value') or x['name']) for x in rows];hits=[t for t in texts if 'PdbTarget103' in t];assert hits
 excerpt=[]
 for t in hits:
  i=t.index('PdbTarget103');excerpt.append(t[max(0,i-100):i+180])
 data=dict(pdb=fixture['pdb'],pdbSha256=fixture['pdbSha256'],source=source,sourceSha256=hashlib.sha256((ROOT/source).read_bytes()).hexdigest(),targetOutput=excerpt,headers=[x['name'] for x in rows if x['type']=='ControlType.HeaderItem'],targetFilters=[dict(id=x['id'],value=x.get('value')) for x in rows if x['id']=='explore_filter'],presentation='structured symbol rows and name filter' if role=='VA' else 'read-only LLVM dump text')
 name=role+'.pdb-explorer.bundle.json';(ROOT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
update(90,'different','동일 SHA256의 자체 MSVC PDB에서 PdbTarget103을 양쪽 모두 읽었습니다. VA는 주소·이름·길이·형식·컴파일 단위의 구조화된 표와 실제 이름 필터를 제공하지만 CA는 LLVM 덤프를 읽기 전용 텍스트로 표시합니다.',files,{'samePdbBoth':True,'targetSymbolFoundBoth':True,'structuredExplorerAndFilteringEqual':False})
