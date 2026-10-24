import sys,time,json
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from verify_native_parse_pool105 import setting,restart,count;from background_client import module,send;from background_steps import setup
P=Path('.local/parity-runs/remaining-105');text='class SingleParser105 { public: int Compute() { return 105; } };\r\nint main(){ SingleParser105 value; return value.Compute(); }\r\n';start=text.rindex('SingleParser105')+3
try:
 setting(1);restart('single-functional');setup('VA','single-parser105',text,start);module('VA','queue-command',windowTitle='',command='VAssistX.ReparseCurrentFile',focusEditor=True);time.sleep(.6);module('VA','queue-command',windowTitle='',command='VAssistX.GotoImplementation',focusEditor=True);time.sleep(.6);v,_=send('VA',dict(kind='classification'));(P/'VA.single-parser-navigation.actual.json').write_text(json.dumps(v,ensure_ascii=False,indent=2));assert v['after']['selectedText']=='SingleParser105',v;count('single-functional-active');print('Native fresh symbol resolved under one-worker limit',flush=True)
finally:
 setting(8);restart('single-functional-restored');count('single-functional-restored');(P/'single-functional.transaction.json').write_text(json.dumps(dict(original=8,restored=True),indent=2))
