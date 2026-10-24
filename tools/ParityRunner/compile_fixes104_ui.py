import sys,json
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from compiler_contracts import build_run,build_files
p=Path('.local/parity-runs/fixes-104');d=json.loads((p/'actual-ui-results.json').read_text());r={}
checks={'member':'int main(){ AddMemberAudit a; a.addedField=7; return a.addedField==7 ? 0:1; }','setter':'int main(){ EncapAudit a; a.SetValue(7); return a.GetValue()==7 ? 0:1; }','declaration':'int main(){ DeclAudit a; return a.Value()==7 ? 0:1; }','override':'int main(){ Derived d; Base& b=d; try { b.Value(); } catch(...) { return 0; } return 1; }'}
for name,main in checks.items():r[name]=build_run('fix104-actual-ui-'+name,'CA',d[name]['text']+'\n'+main+'\n','actual VS command output compiles and runtime postcondition passes; Undo restored original')
r['header']=build_files('fix104-actual-ui-header','CA',{'HeaderAudit.h':d['header']['text'],'a.cpp':'#include "HeaderAudit.h"\nint Other(){HeaderAudit a;try {a.Value();}catch(...){return 7;}return 1;}','main.cpp':'#include "HeaderAudit.h"\nint Other(); int main(){HeaderAudit a;try{a.Value();}catch(...){return Other()==7?0:1;}return 1;}'},'actual VS generated header linked into two translation units; both method calls and exceptions behave as generated')
(p/'actual-ui-compiler-results.json').write_text(json.dumps(r,indent=2));print({k:v['passed'] for k,v in r.items()});assert all(v['passed'] for v in r.values())
