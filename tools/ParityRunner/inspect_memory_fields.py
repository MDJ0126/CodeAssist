import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/VA.memory103.tree.json').read_text(encoding='utf-8-sig'));i=next(i for i,x in enumerate(r) if x['name']=='VA Memory View');print([(x['type'],x['name'],x['id'],x.get('value')) for x in r[i:i+35]])
