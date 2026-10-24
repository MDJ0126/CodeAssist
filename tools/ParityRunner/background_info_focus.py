import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text());cases=[('focus-quick-info','class CorrespondingProbe { public: void Update(); };\r\n',8,'CorrespondingProbe.h','Edit.QuickInfo'),('focus-parameter-info','void ParameterAudit(int first, float second) {}\r\nvoid ParameterCaller() { ParameterAudit(1, 2.0f); }\r\n',None,'parameter-info.cpp','Edit.ParameterInfo')]
for id,text,start,file,command in cases:
 if start is None:start=text.index('1, 2.0f')+3
 for role in ['VA','CA']:
  setup(role,id,text,start,file=file);v,p=module(role,'queue-command',windowTitle='',command=command,focusEditor=True);save(role+'.'+id+'.queued.json',v);time.sleep(.8);q=launch(['ui-tree',h[role]['pid'],OUT/(role+'.'+id+'.tree.json')]);q.wait(timeout=15);r=json.loads((OUT/(role+'.'+id+'.tree.json')).read_text(encoding='utf-8-sig'));print(role,id,[(x['type'],x['name']) for x in r[:9]],flush=True);v,p=send(role,{});save(role+'.'+id+'.render.json',v);v,p=module(role,'info-read',windowTitle='');save(role+'.'+id+'.session.json',v);v,p=module(role,'render-sources',windowTitle='');save(role+'.'+id+'.all-sources.json',v);q=launch(['logical-escape',h[role]['pid'],OUT/(role+'.'+id+'.escape.json')]);q.wait(timeout=15)
