import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/rea-native-links-host-members.json').read_text(encoding='utf-8-sig'))['normalized_result'];refs={x['token']:x for x in r['member_refs']};methods={x['token']:x for x in r['methods']};strings={x['token']:x['value'] for x in r['user_strings']['literals']}
for name in ['CreateRegex','SetRegex']:
 m=next(x for x in r['methods'] if x['declaring_type'].endswith('.HwPrefs') and x['name']==name);print(name,[(a['il_offset'],a['opcode'],strings.get(a.get('operand'),refs.get(a.get('operand'),methods.get(a.get('operand'),{})).get('name',a.get('operand')))) for a in m['body']['anchors']][:45])
print(Path('tools/ManagedDecompiler/Program.cs').read_text()[:1800])
