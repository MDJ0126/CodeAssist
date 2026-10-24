from pathlib import Path
s=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl').read_text(encoding='utf-8-sig',errors='replace')
print([repr(t[:190]) for t in s.split('\f') if any(t.lstrip().startswith(prefix+name+':') for prefix in ['a:','s:','b:','c:','t:'] for name in ['HRESULT','bool','class','loop','BOOL'])])
print(sorted(set(t.lstrip().split(':',1)[0] for t in s.split('\f') if ':' in t)))
