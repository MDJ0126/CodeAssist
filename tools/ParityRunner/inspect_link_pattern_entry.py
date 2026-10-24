import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/rea-native-links-host-members.json').read_text(encoding='utf-8-sig'))['normalized_result']
print([(x['declaring_type'],x['name'],x['signature']['parameter_types']) for x in r['methods'] if any(k in x['name'].lower() for k in ['regex','match','geturl','getmarkers','parse','buildurl']) and not x['name'].startswith('<')][:65])
print('HW',[x['name'] for x in r['methods'] if x['declaring_type']=='WholeTomatoSoftware.SourceLinks.HwLinkOptions'][23:])
