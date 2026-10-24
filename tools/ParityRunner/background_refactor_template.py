import sys,json,hashlib,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module,send
h=json.loads((RUN/'hosts.json').read_text());native=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl');original=native.read_bytes();needle=b'a:Refactor Create From Usage Method Body::\n';assert original.count(needle)==1;patched=original.replace(needle,needle+b'// PARITY_103_CUSTOM_METHOD_BODY\n',1);backup=RUN/'snippet-native-cpp-before.bin';backup.write_bytes(original);library=RUN/'CA/.codeassist/snippets.tpl';caOriginal=library.read_bytes() if library.exists() else None;journal=dict(nativeOriginalSha256=hashlib.sha256(original).hexdigest(),nativePatchedSha256=hashlib.sha256(patched).hexdigest(),scope='single default method body template marker; restore bytes only if unchanged externally')
save('refactor-template.transaction.json',journal)
try:
 native.write_bytes(patched);library.write_text((caOriginal.decode('utf-8-sig')+'\f\n' if caOriginal else '')+'a:Refactor Create From Usage Method Body::\n// PARITY_103_CUSTOM_METHOD_BODY\nthrow std::logic_error("The method or operation is not implemented.");\n',encoding='utf-8')
 text='class TemplateAudit103\r\n{\r\npublic:\r\n    void Run() { MakeTemplate103(7); }\r\n};\r\n'
 for role in ['VA','CA']:
  setup(role,'refactor-custom-template',text,text.index('MakeTemplate103')+3);v,_=module(role,'queue-command',windowTitle='',command='VAssistX.RefactorCreateFromUsage' if role=='VA' else '268',focusEditor=True);save(role+'.refactor-custom-template.queued.json',v)
  if role=='VA':
   spec=OUT/'VA.refactor-custom-template.commit.spec.json';save(spec.name,dict(title='Create From Usage',button='Create',privateFocus=True));q=launch(['drive',h[role]['pid'],OUT/'VA.refactor-custom-template.commit.json',spec]);q.wait(timeout=15)
  time.sleep(.4);v,_=send(role,dict(kind='classification'));save(role+'.refactor-custom-template.observed.json',v);print(role,v.get('after'),flush=True)
finally:
 if native.read_bytes()==patched:native.write_bytes(original);journal['nativeRestored']=native.read_bytes()==original
 else:journal['nativeRestoreConflict']=True
 if caOriginal is None:library.unlink()
 else:library.write_bytes(caOriginal)
 journal['caRestored']=not library.exists() if caOriginal is None else library.read_bytes()==caOriginal;save('refactor-template.transaction.json',journal)
