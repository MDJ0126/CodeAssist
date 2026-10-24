import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module
for id,text,start,file,op in [('direct-quick-info','class CorrespondingProbe { public: void Update(); };\r\n',8,'CorrespondingProbe.h','quickinfo-direct'),('direct-parameter-info','void ParameterAudit(int first, float second) {}\r\nvoid ParameterCaller() { ParameterAudit(1, 2.0f); }\r\n',86,'direct-parameter-info.cpp','parameter-direct')]:
 if op=='parameter-direct':start=text.index('1, 2.0f')+3
 setup('CA',id,text,start,file=file);v,_=module('CA',op,windowTitle='');save('CA.'+id+'.session.json',v);print(id,v,flush=True)
 module('CA','logical-key',windowTitle='',key=27)
