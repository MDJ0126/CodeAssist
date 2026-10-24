from pathlib import Path
s=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl').read_text(encoding='utf-8-sig',errors='replace')
for t in s.split('\f'):
 if any(x in t.split('\n',2)[0:2] for x in []):pass
 if any(x.lower() in t.lower() for x in ['HRESULT','std::string','Smart Suggestion','break;','typedef']):print(repr(t[:260]))
print('head',repr(s[:160]))
