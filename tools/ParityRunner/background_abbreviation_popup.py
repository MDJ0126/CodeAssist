import sys,json
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());text='// abbreviation103\r\n\r\n';body='#ifndef $end$\n$selected$\n#endif\n';library=RUN/'CA/.codeassist/snippets.tpl';original=library.read_bytes() if library.exists() else None
try:
 library.write_text((original.decode('utf-8-sig')+'\f\n' if original else '')+'a:Parity Ifndef103:#ifn:\n'+body,encoding='utf-8')
 for role in ['VA','CA']:
  setup(role,'snippet-abbreviation-menu',text,len(text)-2);v,_=module(role,'posted-text',windowTitle='',text='#ifn');save(role+'.snippet-abbreviation-menu.typed.json',v)
  if role=='VA':
   q=launch(['native-list-snapshot',h[role]['pid'],OUT/'VA.snippet-abbreviation-menu.snapshot.json']);q.wait(timeout=15);print((OUT/'VA.snippet-abbreviation-menu.snapshot.json').read_text(),flush=True)
  else:
   v,_=module(role,'completion-read',windowTitle='');save(role+'.snippet-abbreviation-menu.session.json',v);print(v,flush=True)
finally:
 if original is None:library.unlink()
 else:library.write_bytes(original)
 save('snippet-abbreviation-menu.library-restore.json',dict(restored=not library.exists() if original is None else library.read_bytes()==original))
