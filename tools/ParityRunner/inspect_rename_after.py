import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text());q=launch(['ui-tree',h['VA']['pid'],OUT/'VA.rename-related-file.after-tree.json']);q.wait(timeout=15)
r=json.loads((OUT/'VA.rename-related-file.after-tree.json').read_text(encoding='utf-8-sig'));print([(x['name'],x['id'],x.get('value'),x.get('toggle')) for x in r[:20]])
print((OUT/'VA.rename-related-file.commit.json').read_text())
