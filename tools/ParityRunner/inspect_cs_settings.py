import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/options-pages/VA-Enhanced_Listboxes.json').read_text());print([(x['type'],x['name'],x.get('value'),x.get('items')) for x in r['result']['controls'][:24]])
for p in Path('docs/validation/background-103/options-pages').glob('VA-*.json'):
 r=json.loads(p.read_text());v=[(x['name'],x.get('value')) for x in r['result']['controls'] if 'C#' in x['name'] or 'C/C++' in x['name']];
 if v:print(p.name,v)
