import sys,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module,send
for id,text,target in [('brace-key-pair','void BraceKey103() {\r\n    return;\r\n}\r\n','pair'),('brace-key-mismatch','void BraceKeyBad103() {\r\n    return;\r\n}\r\n    } // mismatch\r\n','mismatch')]:
 position=text.index('{') if target=='pair' else text.rindex('}');offsets=[text.index('}')] if target=='pair' else [position]
 for role in ['VA','CA']:
  setup(role,id,text,position-1)
  for state,key,expected in [('baseline',None,position-1),('at',39,position),('after',39,position+1),('restored1',37,position),('restored',37,position-1)]:
   if key:
    v,p=module(role,'logical-key',windowTitle='',key=key);save(role+'.'+id+'.'+state+'.key.json',v);assert v['status']=='complete',v;time.sleep(.15)
   v,p=module(role,'character-bounds',windowTitle='',offsets=offsets);save(role+'.'+id+'.'+state+'.bounds.json',v);assert v['status']=='complete',v
   v,p=send(role,dict(kind='classification'));save(role+'.'+id+'.'+state+'.caret.json',v);assert v['after']['text']==text and v['after']['caret']==expected,(role,state,v)
   v,p=send(role,{});save(role+'.'+id+'.'+state+'.render.json',v)
  print(role,id,'posted-arrow states verified',flush=True)
