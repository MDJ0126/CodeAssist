import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/rea-native-links-host-members.json').read_text(encoding='utf-8-sig'))['normalized_result']
print([(x['declaring_type'],x['name'],x['signature']['parameter_types']) for x in r['methods'] if any(k in x['name'].lower() for k in ['expand','replace','navigate','openurl','context','url']) and not x['name'].startswith('<')][:55]);print([x['full_name'] for x in r['types'] if 'Value' in x['name'] or 'Behavior' in x['name']])
