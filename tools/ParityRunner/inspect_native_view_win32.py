import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text());q=launch(['windows',h['VA']['pid'],OUT/'VA.view-file-nav.win32.json']);q.wait(timeout=15);r=json.loads((OUT/'VA.view-file-nav.win32.json').read_text(encoding='utf-8-sig'));print([x for x in r if x.get('visible') and ('Files' in x['title'] or 'Edit' in x['cls'] or 'Combo' in x['cls'])][-25:])
