import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import launch,OUT;from background_client import RUN
h=json.loads((RUN/'hosts.json').read_text());q=launch(['native-file-combo',h['VA']['pid'],OUT/'VA.view-file-nav.actual-retry.json',RUN/'VA/ViewFileTarget103.h']);q.wait(timeout=15);print((OUT/'VA.view-file-nav.actual-retry.json').read_text(encoding='utf-8-sig'),flush=True)
