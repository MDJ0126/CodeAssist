import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/rea-native-links-host-artifact.json').read_text(encoding='utf-8-sig'));n=r['normalized_result'];print(n.keys());print([(x.get('namespace'),x.get('name')) for x in n.get('types',[]) if any(v in x.get('name','').lower() for v in ['link','config','regex','rule','simple','complex'])][:60])
