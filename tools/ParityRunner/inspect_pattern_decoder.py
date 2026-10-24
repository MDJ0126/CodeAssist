import json
from pathlib import Path
r=json.loads(Path('docs/validation/background-103/rea-native-links-host-members.json').read_text(encoding='utf-8-sig'))['normalized_result'];m=next(x for x in r['methods'] if x['declaring_type'].endswith('.HwPrefs') and x['name']=='GetRegexPattern');refs={x['token']:x for x in r['member_refs']};methods={x['token']:x for x in r['methods']};strings={x['token']:x['value'] for x in r['user_strings']['literals']};print([(a,strings.get(a.get('operand'),refs.get(a.get('operand'),methods.get(a.get('operand'),{})).get('name'))) for a in m['body']['anchors']])
