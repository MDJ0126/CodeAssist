import ctypes as c,json
from pathlib import Path
u=c.WinDLL('user32',use_last_error=True);u.GetWindowLongW.argtypes=[c.c_void_p,c.c_int];u.GetWindowLongW.restype=c.c_long;rows=[]
for h in [1577264,17434992]:
 c.set_last_error(0);style=u.GetWindowLongW(h,-16);rows.append(dict(hwnd=h,style=hex(style&0xffffffff),readOnly=bool(style&0x800),error=c.get_last_error()))
Path('docs/validation/background-103/VA.workspace-field-styles.json').write_text(json.dumps(rows,indent=2))
