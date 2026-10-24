import json
from pathlib import Path
p=Path('docs/validation/background-103/CA.smart-template-designated.session.json');r=json.loads(p.read_text());rows=r.get('result',{}).get('rows',[]);print([(x['text'],x['insertion'],x['description']) for x in rows if 'SmartTemplate103(' in str(x)]);print('rows',len(rows))
