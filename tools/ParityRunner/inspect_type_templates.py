from pathlib import Path
s=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl').read_text(encoding='utf-8-sig',errors='replace')
print([repr(t[:170]) for t in s.split('\f') if t.lstrip().startswith(('a:bool:','a:BOOL:','a:Refactor Document Method:'))])
