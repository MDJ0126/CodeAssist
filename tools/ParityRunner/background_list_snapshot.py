import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text());q=launch(['native-list-snapshot',h['VA']['pid'],OUT/'VA.autosuggest.snapshot.json']);q.wait(timeout=15);print((OUT/'VA.autosuggest.snapshot.json').read_text())
