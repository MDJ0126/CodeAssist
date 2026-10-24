import sys,json,time,base64
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import RUN,module,send
source=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl').read_text(encoding='utf-8-sig',errors='replace');chunk=next(x for x in source.split('\f') if x.lstrip('\r\n').startswith('a:#ifndef ... #endif:#ifn:\n'));body=chunk.lstrip('\r\n').split('\n',1)[1];library=RUN/'CA/.codeassist/snippets.tpl';original=library.read_bytes() if library.exists() else None;library.parent.mkdir(exist_ok=True);library.write_text((original.decode('utf-8-sig')+'\f\n' if original else '')+'a:Parity Ifndef103:#ifn:\n'+body,encoding='utf-8')
try:
 text='// shortcut audit 103\r\n\r\n'
 for role in ['VA','CA']:
  setup(role,'snippet-ifndef-shortcut',text,len(text)-2);v,_=module(role,'posted-text',windowTitle='',text='#ifn');save(role+'.snippet-ifndef-shortcut.typed.json',v);v,_=send(role,dict(kind='input',action='tab'));save(role+'.snippet-ifndef-shortcut.tab.json',v);print(role,v.get('after'),v.get('error'),flush=True)
finally:
 if original is None:library.unlink()
 else:library.write_bytes(original)
 save('snippet-ifndef-shortcut.library-restored.json',dict(restored=not library.exists() if original is None else library.read_bytes()==original,template=body))
