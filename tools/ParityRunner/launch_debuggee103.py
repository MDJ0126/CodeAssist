import subprocess,json,time,sys
from pathlib import Path
root=Path.cwd();folder=root/'.local/parity-runs/background-103/debug-artifacts';out=root/'docs/validation/background-103';p=subprocess.run(['cmd.exe','/d','/c',str(folder/'build-debug103.cmd')],cwd=folder,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000,timeout=30);(out/'debuggee-103.build.log').write_bytes(p.stdout);assert p.returncode==0,p.stdout
roles={}
for role in ['VA','CA']:
 log=out/(role+'.debuggee103.launch.log');f=log.open('wb');child=subprocess.Popen([sys.executable,str(root/'tools/ParityRunner/isolated_desktop.py'),'CodeAssistVerify-runtime',str(folder/'ParityDebug103.exe')],stdout=f,stderr=subprocess.STDOUT,creationflags=0x08000000);f.close();deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  lines=log.read_text(errors='replace').splitlines();receipt=next((json.loads(x) for x in lines if x.startswith('{') and 'launchedPid' in x),None)
  if receipt:roles[role]=dict(pid=receipt['launchedPid'],broker=child.pid,desktop=receipt['desktop']);break
  time.sleep(.05)
 assert role in roles
(out/'debuggee103.hosts.json').write_text(json.dumps(roles,indent=2));print(roles,flush=True)
