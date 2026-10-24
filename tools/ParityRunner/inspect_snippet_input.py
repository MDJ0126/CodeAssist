import json
from pathlib import Path
p=Path('docs/validation/background-103');r=json.loads((p/'CA.snippet-header.final-dialog.json').read_text());print([(x['type'],x['name'],x.get('value')) for x in r['result']['controls'] if x['type'] in ['TextBox','TextBlock','System.Windows.Controls.TextBox']][:14]);a=json.loads((p/'VA.snippet-header.final.json').read_text(encoding='utf-8-sig'));b=json.loads((p/'CA.snippet-header.final.json').read_text(encoding='utf-8-sig'));print('native',a['after']);print('CA',b['after'])
