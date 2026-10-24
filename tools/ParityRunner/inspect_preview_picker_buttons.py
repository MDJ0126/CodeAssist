import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/CA.file-preview-utility.picker-tree.json').read_text(encoding='utf-8-sig'));print([(x['name'],x['id']) for x in r if x['type']=='ControlType.Button'][:14])
