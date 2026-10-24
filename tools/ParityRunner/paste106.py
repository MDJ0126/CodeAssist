from foreground106 import *
text='void Paste106() {\r\n    \r\n}\r\n';payload='if(true){\r\nint value=1;\r\nvalue++;\r\n}'
for role in ['VA','CA']:
 setup_raw(role,'paste-physical106',text,text.index('    \r\n')+4)
 v=physical(role,'paste-physical106','paste',text=payload);print(role,'afterPaste',v['after'],flush=True)
 v=physical(role,'paste-undo106','keys',keys='CTRL+Z');print(role,'afterUndo',v['after'],flush=True)
