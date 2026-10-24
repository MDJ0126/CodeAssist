import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/VA.suggest-auto-native.windows.json').read_text(encoding='utf-8-sig'));print([x for x in r if x['visible'] and x['cls']=='SysListView32'])
