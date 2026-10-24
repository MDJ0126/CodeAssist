from foreground106 import *
for role in ['VA','CA']:
 for case,text in [('matched','void BracePhysical106() {\r\n    return;\r\n}\r\n'),('mismatch','void BracePhysical106() {\r\n    return;\r\n}\r\n}\r\n')]:
  pos=text.index('{') if case=='matched' else text.rindex('}')
  setup_raw(role,'physical-'+case+'106',text,pos-1);physical(role,case+'-off','focus');physical(role,case+'-at','keys',keys='RIGHT');v=physical(role,case+'-after','keys',keys='RIGHT');print(role,case,v['after'],flush=True)
