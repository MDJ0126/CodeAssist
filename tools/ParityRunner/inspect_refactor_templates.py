from pathlib import Path
p=Path('C:/Users/ehdwn/AppData/Roaming/VisualAssist/Autotext/cpp.tpl');s=p.read_text(encoding='utf-8-sig',errors='replace')
print([(x.lstrip().split('\n',1)[0],x.lstrip().split('\n',1)[1][:380]) for x in s.split('\f') if 'Document Method' in x.split('\n',2)[0] or x.lstrip().split('\n',1)[0].startswith('a:Refactor')][:12])
