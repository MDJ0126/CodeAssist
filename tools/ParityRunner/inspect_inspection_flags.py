import json
from pathlib import Path
for name in ['VA-Per-File.json','VA-Project-Wide.json','CA-Per-File.json','CA-Project-Wide.json','VA-Checker_Sets.json','CA-Checker_Sets.json']:
 p=Path('docs/validation/background-103/options-pages')/name
 if p.exists():
  r=json.loads(p.read_text());print(name,[(x['type'],x['name'],x.get('value')) for x in r['result']['controls'] if 'CheckBox' in x['type'] or 'RadioButton' in x['type'] or x['type'] in ['TextBox','ComboBox']][:13])
