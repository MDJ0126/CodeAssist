import sys
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import module
input='AUDIT-103 AUDIT-xyz audit-7 AUDIT 42 AUDIT-84.'
for name,kw,ca in [('simple',dict(keywords='AUDIT',suffix='-',valueType='Number'),dict(pattern='AUDIT-{id}',regex=False,target='https://example.invalid/{id}')),('complex',dict(keywords=r'RE=\b(?<Keyword>AUDIT)-(?<Value>[0-9]+)\b',suffix='',valueType='Number'),dict(pattern=r'\b(?<Keyword>AUDIT)-(?<Value>[0-9]+)\b',regex=True,target='https://example.invalid/{Value}'))]:
 for role in ['VA','CA']:
  v,_=module(role,'native-pattern-query' if role=='VA' else 'ca-pattern-query',windowTitle='',input=input,**(kw if role=='VA' else ca));save(role+'.actual-link-pattern.'+name+'.json',v);print(role,name,v,flush=True)
