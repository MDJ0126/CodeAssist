import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text());q=launch(['inventory',h['VA']['pid'],OUT/'VA.current-native-commands.json']);q.wait(timeout=15)
r=json.loads((OUT/'VA.current-native-commands.json').read_text(encoding='utf-8-sig'));print(len(r['commands']));print([(x['name'],x['id']) for x in r['commands'] if any(t in x['name'].lower() for t in ['rename','create','snippet','print','spell','surround'])])
