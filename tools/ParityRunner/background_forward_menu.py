import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());header='#pragma once\r\nclass ForwardTarget103 {};\r\n';(RUN/'VA/ForwardTarget103.h').write_bytes(header.encode());text='#pragma once\r\nclass ForwardContainer103\r\n{\r\n    ForwardTarget103* value;\r\n};\r\n'
setup('VA','forward-menu',text,text.index('ForwardTarget103')+3,file='ForwardContainer103.h',extras=['ForwardTarget103.h']);v,_=module('VA','queue-command',windowTitle='',command='VAssistX.RefactorContextMenu',focusEditor=True);save('VA.forward-menu.queued.json',v);time.sleep(.3)
q=launch(['native-menus',h['VA']['pid'],OUT/'VA.forward-menu.native.json']);q.wait(timeout=15)
q=launch(['ui-tree',h['VA']['pid'],OUT/'VA.forward-menu.tree.json']);q.wait(timeout=15)
print((OUT/'VA.forward-menu.native.json').read_text(encoding='utf-8-sig')[:2200])
