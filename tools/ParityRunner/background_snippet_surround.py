import json,sys,subprocess,time,shutil
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module,RUN
ROOT=Path.cwd();OUT=ROOT/'docs/validation/background-103';PY=sys.executable;hosts=json.loads((RUN/'hosts.json').read_text());worker=ROOT/'tools/ShortcutProbe/BackgroundOptionsV28.exe'
def launch(args):return subprocess.Popen([PY,str(ROOT/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(worker),*map(str,args)],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=0x08000000)
source=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl').read_text(encoding='utf-8-sig',errors='replace');chunk=next(x for x in source.split('\f') if x.lstrip('\r\n').startswith('a:#if 0 ... #endif:#if:\n') or x.lstrip('\r\n').startswith('a:#if 0 ... #endif:#if:\r\n'));body=chunk.lstrip('\r\n').split('\n',1)[1]
library=RUN/'CA/.codeassist/snippets.tpl';existed=library.exists();original=library.read_bytes() if existed else None;library.parent.mkdir(exist_ok=True);library.write_text((original.decode('utf-8-sig')+'\f\n' if existed else '')+'a:Parity Surround103::\n'+body,encoding='utf-8');(OUT/'snippet-surround.template.txt').write_text(body,encoding='utf-8')
try:
 for role in ['VA','CA']:
  out=OUT/(role+'.snippet-surround.json');p=launch(['case',hosts[role]['pid'],out,OUT/'snippet-surround.case.json',role]);time.sleep(2.7)
  if role=='VA':
   q=launch(['pick',hosts[role]['pid'],OUT/'VA.snippet-surround-pick.json',OUT/'snippet-surround.pick.json']);q.wait(timeout=13)
  else:
   v,path=module(role,'snippet-insert',windowTitle='CodeAssist: Snippets',choice='Parity Surround103');(OUT/'CA.snippet-surround-dialog.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
  p.wait(timeout=15);result=json.loads(out.read_text(encoding='utf-8-sig'));assert result['status']=='complete';print(role,repr(result['after']['text']),flush=True)
finally:
 if existed:library.write_bytes(original)
 else:library.unlink()
 (OUT/'snippet-surround.library-restore.json').write_text(json.dumps(dict(existed=existed,restored=(library.read_bytes()==original) if existed else not library.exists())))