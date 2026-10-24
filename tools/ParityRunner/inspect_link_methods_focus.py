import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/rea-native-links-host-members.json').read_text(encoding='utf-8-sig'))['normalized_result']
for t in ['WholeTomatoSoftware.SourceLinks.LinkSettings','WholeTomatoSoftware.SourceLinks.HwLinkOptions','WholeTomatoSoftware.SourceLinks.SourceLinksAST','WholeTomatoSoftware.SourceLinks.SourceLinksQuickInfoSource','WholeTomatoSoftware.SourceLinks.LinkModelExtensions']:
 print(t,[(x['name'],x['signature'].get('return_type'),x['signature'].get('parameter_types')) for x in r['methods'] if x['declaring_type']==t][:24]);print('fields',[x['name'] for x in r['fields'] if x['declaring_type']==t])
