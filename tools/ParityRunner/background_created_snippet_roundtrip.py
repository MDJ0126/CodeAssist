import sys,json,hashlib,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module,send
h=json.loads((RUN/'hosts.json').read_text());native=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl');original=native.read_bytes();assert b'Parity103CreatedSelection' not in original;library=RUN/'CA/.codeassist/snippets.tpl';caOriginal=library.read_bytes() if library.exists() else None;journal=dict(nativeOriginalSha256=hashlib.sha256(original).hexdigest());save('created-snippet-roundtrip.transaction.json',journal)
try:
 v,_=module('VA','native-snippet-editor',windowTitle='',newTitle='Parity103CreatedSelection',newShortcut='pcreated103',commit=True,sourceSha256=hashlib.sha256(original).hexdigest());save('VA.created-snippet-roundtrip.saved.json',v);assert v['status']=='complete',v;time.sleep(.3);nativeChanged=native.read_bytes();assert b'Parity103CreatedSelection' in nativeChanged;journal['nativeSavedSha256']=hashlib.sha256(nativeChanged).hexdigest();save('created-snippet-roundtrip.transaction.json',journal);print('native saved',v['result']['stillVisible'],flush=True)
 text='void NewSnippetAudit103() { int example = 103; }\r\n';selected='int example = 103;';setup('CA','created-snippet-roundtrip-source',text,text.index(selected),len(selected));v,_=module('CA','queue-command',windowTitle='',command='374',focusEditor=True);save('CA.created-snippet-roundtrip.queued.json',v)
 for name,value in [('title','Parity103CreatedSelection'),('shortcut','pcreated103')]:
  p=OUT/('CA.created-snippet-roundtrip.'+name+'.spec.json');save(p.name,dict(title='CodeAssist',button='Save',edit=value,privateFocus=True));q=launch(['drive',h['CA']['pid'],OUT/('CA.created-snippet-roundtrip.'+name+'.result.json'),p]);q.wait(timeout=15)
 assert library.exists() and 'Parity103CreatedSelection' in library.read_text();save('CA.created-snippet-roundtrip.saved-library.json',dict(content=library.read_text()));print('CA saved',flush=True)
 scratch='int CreatedSnippetRoundtrip103() {\r\n    \r\n    return example;\r\n}\r\n'
 for role in ['VA','CA']:
  setup(role,'created-snippet-roundtrip-target',scratch,scratch.index('    \r\n')+4);v,_=module(role,'queue-command',windowTitle='',command='VAssistX.VaSnippetInsert' if role=='VA' else '279',focusEditor=True);save(role+'.created-snippet-roundtrip.insert.queued.json',v);time.sleep(.2)
  if role=='VA':
   p=OUT/'VA.created-snippet-roundtrip.insert.spec.json';save(p.name,dict(row='Parity103CreatedSelection'));q=launch(['native-menu-pick',h[role]['pid'],OUT/'VA.created-snippet-roundtrip.insert.result.json',p]);q.wait(timeout=15)
  else:
   v,_=module(role,'snippet-insert',windowTitle='CodeAssist: Snippets',choice='Parity103CreatedSelection');save('CA.created-snippet-roundtrip.insert.result.json',v)
  time.sleep(.3);v,_=send(role,dict(kind='classification'));save(role+'.created-snippet-roundtrip.observed.json',v);print(role,v.get('after'),flush=True)
finally:
 current=native.read_bytes()
 if current==original:journal['nativeRestored']=True
 elif b'Parity103CreatedSelection' in current and ('nativeSavedSha256' not in journal or hashlib.sha256(current).hexdigest()==journal['nativeSavedSha256']):native.write_bytes(original);journal['nativeRestored']=native.read_bytes()==original
 else:journal['nativeRestoreConflict']=True
 if caOriginal is None:
  if library.exists():library.unlink()
 else:library.write_bytes(caOriginal)
 journal['caRestored']=not library.exists() if caOriginal is None else library.read_bytes()==caOriginal;save('created-snippet-roundtrip.transaction.json',journal)
