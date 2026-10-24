import json
from pathlib import Path
p=Path('docs/validation/background-103')
r=json.loads((p/'native-links-runtime-retry.json').read_text());print([(x['plugin'],x['status'],x.get('regex'),x.get('url'),str(x.get('error'))[:220]) for x in r])
r=json.loads((p/'rea-native-url-members.json').read_text(encoding='utf-8-sig'))['normalized_result'];print('URL strings',r['user_strings']);print('fields',r['fields'])
