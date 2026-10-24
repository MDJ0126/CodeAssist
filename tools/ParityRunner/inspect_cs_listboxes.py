import json
from pathlib import Path
for p in Path('docs/validation/background-103/options-pages').glob('VA-*Enhanced*.json'):
 r=json.loads(p.read_text());print(p.name,[(x['name'],x.get('value')) for x in r['result']['controls'] if 'CheckBox' in x['type'] or x['type']=='ComboBox'])
