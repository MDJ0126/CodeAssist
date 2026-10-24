from pathlib import Path
s=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl').read_text(encoding='utf-8-sig',errors='replace')
for t in s.split('\f'):
 if '$selected$' in t and t.lstrip().startswith('a:'):print(repr(t[:320]))
