import json
from pathlib import Path
r=json.loads(Path('.local/parity-runs/background-103/native-debug-tools-members.json').read_text(encoding='utf-8-sig'))['normalized_result'];print([x['full_name'] for x in r['types'] if any(s in x['name'].lower() for s in ['filter','step'])][:45])
