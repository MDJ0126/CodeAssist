import sys,json,hashlib,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());native=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl');original=native.read_bytes();entry=b'\f\na:SmartTemplate103::\nSmartTemplate103(103)\nSmartTemplate103(203)\n';assert b'a:SmartTemplate103:' not in original;patched=original+entry;library=RUN/'CA/.codeassist/snippets.tpl';caOriginal=library.read_bytes() if library.exists() else None;journal=dict(nativeOriginalSha256=hashlib.sha256(original).hexdigest(),nativePatchedSha256=hashlib.sha256(patched).hexdigest());save('smart-template.transaction.json',journal)
try:
 native.write_bytes(patched);library.write_bytes((caOriginal+b'\f\n' if caOriginal else b'')+entry.lstrip(b'\f\n'))
 text='struct SmartTemplate103\r\n{\r\n    explicit SmartTemplate103(int input) : value(input) {}\r\n    int value;\r\n};\r\nint SmartTemplateCaller103()\r\n{\r\n    SmartTemplate103 result =;\r\n    return result.value;\r\n}\r\n'
 for role in ['VA','CA']:
  setup(role,'smart-template',text,text.index('=;')+1);v,_=module(role,'posted-text',windowTitle='',text=' ');save(role+'.smart-template.typed.json',v)
  if role=='VA':
   q=launch(['native-list-snapshot',h[role]['pid'],OUT/'VA.smart-template.snapshot.json']);q.wait(timeout=15);print((OUT/'VA.smart-template.snapshot.json').read_text(),flush=True)
  else:
   v,_=module(role,'completion-read',windowTitle='');save(role+'.smart-template.session.json',v);print(v,flush=True)
finally:
 if native.read_bytes()==patched:native.write_bytes(original);journal['nativeRestored']=native.read_bytes()==original
 else:journal['nativeRestoreConflict']=True
 if caOriginal is None:library.unlink()
 else:library.write_bytes(caOriginal)
 journal['caRestored']=not library.exists() if caOriginal is None else library.read_bytes()==caOriginal;save('smart-template.transaction.json',journal)
