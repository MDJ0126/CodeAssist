import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module
cases=[('brace-interface-match','void BraceInterface103() {\r\n    return;\r\n}\r\n','{'),('brace-interface-mismatch','void BraceMismatch103() {\r\n    ]\r\n}\r\n',']')]
for id,text,char in cases:
 for role in ['VA','CA']:
  setup(role,id,text,text.index(char)+1);v,p=module(role,'marker-state',windowTitle='');save(role+'.'+id+'.markers.json',v);print(role,id,v.get('result',{}).get('markers'),str(v.get('error'))[:100],flush=True)
