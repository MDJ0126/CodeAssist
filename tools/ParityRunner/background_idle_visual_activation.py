import sys,subprocess
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import save
v,_=module('VA','owned-activation-message',windowTitle='',enabled=True);save('VA.idle-visual.activation-message.json',v);assert v['status']=='complete',v
try:
 p=subprocess.run([sys.executable,'tools/ParityRunner/background_unknown_edited.py'],timeout=90,creationflags=0x08000000);print('unknown run',p.returncode,flush=True)
 p=subprocess.run([sys.executable,'tools/ParityRunner/background_brace_visual.py'],timeout=90,creationflags=0x08000000);print('brace run',p.returncode,flush=True)
finally:
 v,_=module('VA','owned-activation-message',windowTitle='',enabled=False);save('VA.idle-visual.activation-restored.json',v)
