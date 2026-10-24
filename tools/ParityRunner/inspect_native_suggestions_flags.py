import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/options-pages/VA-Suggestions.json').read_text());print([(x['type'],x['name'],x.get('value')) for x in r['result']['controls'] if 'CheckBox' in x['type'] or 'RadioButton' in x['type']])
