import json
from pathlib import Path
p=Path('docs/validation/background-103')
for role in ['VA','CA']:
 for state in ['off','on']:
  f=p/(role+'.spelling.'+state+'.markers.json')
  if f.exists():
   r=json.loads(f.read_text());print(role,state,r.get('result',{}).get('markers'),str(r.get('error'))[:120])
