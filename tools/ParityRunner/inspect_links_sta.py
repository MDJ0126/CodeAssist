import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/native-links-runtime-sta.json').read_text());print([(x['plugin'],x['status'],str(x.get('fileTooltip'))[:700],str(x.get('error'))[:170]) for x in r])
