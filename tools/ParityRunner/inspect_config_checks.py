import json
from pathlib import Path
for name in ['VA-Performance.json','CA-Performance.json','VA-C_C___Directories.json','CA-C_C___Directories.json','VA-Projects_and_Files.json','CA-Projects_and_Files.json']:
 p=Path('docs/validation/background-103/options-pages')/name
 if p.exists():
  r=json.loads(p.read_text());print(name,[(x['type'],x['name'],x.get('value'),x.get('items')) for x in r['result']['controls'] if x['type'] in ['TextBox','ComboBox'] or 'CheckBox' in x['type'] or 'RadioButton' in x['type']][:17])
