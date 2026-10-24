import sys,json,time
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT;from background_client import RUN,module
h=json.loads((RUN/'hosts.json').read_text());text='int RecentSuggestion103()\r\n{\r\n    int nearbyValue103 = 7;\r\n    return ;\r\n}\r\n'
for role in ['VA','CA']:
 setup(role,'suggest-recent-multiline',text,text.index(';\r\n}'));v,_=module(role,'posted-text',windowTitle='',text='nearbyVal');save(role+'.suggest-recent-multiline.typed.json',v)
 if role=='VA':
  q=launch(['native-list-snapshot',h[role]['pid'],OUT/'VA.suggest-recent-multiline.snapshot.json']);q.wait(timeout=15);print((OUT/'VA.suggest-recent-multiline.snapshot.json').read_text(),flush=True)
 else:
  v,_=module(role,'completion-read',windowTitle='');save(role+'.suggest-recent-multiline.session.json',v);print(v,flush=True)
