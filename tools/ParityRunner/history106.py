from foreground106 import *
text='// ClipboardAlpha106\r\n// ClipboardBeta106\r\n\r\n'
for role in ['VA','CA']:
 setup_raw(role,'history-physical106',text,len(text));v=physical(role,'history-physical106','history',native=role=='VA');print(role,v['after'],flush=True)
