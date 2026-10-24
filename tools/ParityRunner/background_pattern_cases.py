import sys
sys.path.insert(0,'tools/ParityRunner');from background_steps import save;from background_client import module
for name,kw in [('simple',dict(keywords='AUDIT',suffix='-',valueType='Number',input='AUDIT-103 AUDIT-xyz audit-7 AUDIT-42.')),('complex',dict(keywords=r'\b(?<Keyword>AUDIT)-(?<Value>[0-9]+)\b',suffix='',valueType='Number',input='AUDIT-103 AUDIT-xyz audit-7 AUDIT-42.')),('string',dict(keywords='AUDIT',suffix=':',valueType='String',input='AUDIT:alpha AUDIT:"two words" AUDIT:103'))]:
 v,_=module('VA','native-pattern-query',windowTitle='',**kw);save('VA.source-link-pattern.'+name+'.json',v);print(name,v,flush=True)
