import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/rea-native-links-host-members.json').read_text(encoding='utf-8-sig'))['normalized_result'];print([(x.get('namespace'),x.get('name'),x.get('full_name')) for x in r['types'] if any(v in x.get('name','').lower() for v in ['config','link','simple','pattern','regex'])][:45]);print(r['types'][:1]);print(r['methods'][:1])
