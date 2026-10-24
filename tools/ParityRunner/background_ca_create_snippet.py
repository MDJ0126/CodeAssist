import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());text='void CreateSelectionAudit103()\r\n{\r\n    int value = 103;\r\n}\r\n';selected='int value = 103;';library=RUN/'CA/.codeassist/snippets.tpl';original=library.read_bytes() if library.exists() else None
try:
 setup('CA','create-snippet-selection',text,text.index(selected),len(selected));v,_=module('CA','queue-command',windowTitle='',command='374',focusEditor=True);save('CA.create-snippet-selection.queued.json',v)
 for id,value in [('title','Parity103CreatedSelection'),('shortcut','pcreated103')]:
  spec=OUT/('CA.create-snippet-selection.'+id+'.spec.json');save(spec.name,dict(title='CodeAssist',button='Save',edit=value,privateFocus=True));q=launch(['drive',h['CA']['pid'],OUT/('CA.create-snippet-selection.'+id+'.result.json'),spec]);q.wait(timeout=15)
 time.sleep(.3);save('CA.create-snippet-selection.created-library.json',dict(exists=library.exists(),content=library.read_text() if library.exists() else None));print('created',library.read_text() if library.exists() else None,flush=True)
finally:
 if original is None:
  if library.exists():library.unlink()
 else:library.write_bytes(original)
 save('CA.create-snippet-selection.library-restored.json',dict(restored=not library.exists() if original is None else library.read_bytes()==original))
