import json
from pathlib import Path
r=json.loads(Path('.local/parity-runs/background-103/native-debug-tools-members.json').read_text())['normalized_result']
for t in ['WholeTomatoSoftware.VaDebuggerTools.Controls.VaStepFilterControl','WholeTomatoSoftware.VaDebuggerTools.VaStepFilterToolWindow']:
 print(t,[(x['name'],x['signature']['parameter_types']) for x in r['methods'] if x['declaring_type']==t][:40]);print('fields',[x['name'] for x in r['fields'] if x['declaring_type']==t])
print([x['value'] for x in r['user_strings']['literals'] if 'StepFilter' in x['value']][:12])
