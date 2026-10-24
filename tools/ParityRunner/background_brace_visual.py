import sys,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,focus_existing;from background_client import module,send
cases=[('brace-visual-pair','void BraceVisual103() {\r\n    return;\r\n}\r\n',6,None,'}'),('brace-visual-mismatch','void BraceBadVisual103() {\r\n    ] // mismatch\r\n}\r\n',None,']',']')]
for id,text,base,char,target in cases:
 if base is None:base=text.index('mismatch')+3
 on=text.index('{')+1 if char is None else text.index(char)+1
 offsets=[text.index(target)] if target==']' else [text.index('{'),text.index('}')]
 for role in ['VA','CA']:
  setup(role,id,text,base)
  for state,caret in [('baseline',base),('active',on),('restored',base)]:
   module(role,'caret-set',windowTitle='',offset=caret);focus_existing(role,id+'-'+state);time.sleep(.35);v,p=module(role,'character-bounds',windowTitle='',offsets=offsets);save(role+'.'+id+'.'+state+'.bounds.json',v);assert v['status']=='complete',v;v,p=send(role,{});save(role+'.'+id+'.'+state+'.render.json',v)
  print(role,id,'A-B-A captures complete',flush=True)
