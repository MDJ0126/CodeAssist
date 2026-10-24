import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,setup,save,OUT;from background_client import module,send,RUN
h=json.loads((RUN/'hosts.json').read_text())['CA']['pid'];base=Path('.local/parity-runs/fixes-104')
for mode in ['show-owned','load-capture-package']:
 p=base/(mode+'.json');q=launch([mode,h,p]);q.wait(timeout=20);print(mode,p.read_text(encoding='utf-8-sig')[:300],flush=True)
text='class AddMemberAudit {\r\npublic:\r\n};\r\n';decl='class DeclAudit {};\r\nint DeclAudit::Value() { return 7; }\r\n';virt='class Base { public: virtual int Value() = 0; virtual ~Base() = default; };\r\nclass Derived : public Base {};\r\n';encap='class EncapAudit {\r\npublic:\r\n    int value;\r\n};\r\n';header='#pragma once\r\nclass HeaderAudit {\r\npublic:\r\n    int Value();\r\n};\r\n'
cases=[('member',text,8,0,386,'int addedField;','Add Member to Class'),('setter',encap,encap.index('value')+2,0,385,'Value','Encapsulate Field'),('declaration',decl,decl.index('int DeclAudit'),len('int DeclAudit::Value() { return 7; }'),390,None,'Create Declaration'),('override',virt,virt.index('Derived')+2,0,393,'virtual int Value() = 0;','Implement Virtual Methods in Class'),('header',header,header.index('Value')+2,0,391,'HeaderAudit','Generate Method Implementation')]
results={}
def drive(name,title,edit=None):
 p=base/(name+'.driver.json');spec=dict(title=title,button='Save' if edit is not None else 'Apply changes');
 if edit is not None:spec['edit']=edit
 p.write_text(json.dumps(spec));out=base/(name+'.driver-result.json');q=launch(['drive',h,out,p]);q.wait(timeout=15);r=json.loads(out.read_text(encoding='utf-8-sig'));assert r.get('acted'),r
for name,body,start,length,cmd,prompt,title in cases:
 setup('CA','fix104-'+name,body,start,length,file='fix104-'+name+('.h' if name=='header' else '.cpp'));v,_=module('CA','queue-command',windowTitle='',command=str(cmd),focusEditor=True);assert v['status']=='complete',v;time.sleep(.3)
 if prompt is not None:drive(name+'-prompt','CodeAssist',prompt)
 drive(name+'-apply',title);time.sleep(.2);v,_=send('CA',dict(kind='classification'));(base/(name+'.after.json')).write_text(json.dumps(v,ensure_ascii=False,indent=2));after=v['after']['text'];assert after!=body,(name,v)
 module('CA','queue-command',windowTitle='',command='Edit.Undo',focusEditor=True);time.sleep(.25);undo,_=send('CA',dict(kind='classification'));assert undo['after']['text']==body,(name,'undo failure');results[name]=dict(text=after,undoRestored=True,source='actual VS command and Apply changes dialog');(base/'actual-ui-results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2));print(name,'actual Apply + Undo passed',flush=True)
