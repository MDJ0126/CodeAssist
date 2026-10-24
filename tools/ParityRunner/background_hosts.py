"""Starts only the two owned background profiles; no input-desktop activation."""
import json,os,subprocess,sys,time,argparse,ctypes
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];RUN=ROOT/'.local/parity-runs/background-103';OUT=ROOT/'docs/validation/background-103';PY=sys.executable;LAUNCH=ROOT/'tools/ParityRunner/isolated_desktop.py';IDE=Path('C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE')
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--roles',nargs='+',choices=['VA','CA'],default=['VA','CA']);parser.add_argument('--skip-configuration',action='store_true');parser.add_argument('--visible-input-desktop',action='store_true');args=parser.parse_args();hosts=json.loads((RUN/'hosts.json').read_text()) if (RUN/'hosts.json').exists() else {};env=os.environ.copy();env['PATH']=str(IDE)+';'+env.get('PATH','');env['CODEASSIST_PARITY_ROOT']=str(RUN);env['CODEASSIST_BACKGROUND_CAPTURE']='1';env.pop('CODEASSIST_PARITY_PLAN',None)
 for role in args.roles:
  if role in hosts:
   check=subprocess.run(['powershell.exe','-NoProfile','-Command','if(Get-Process -Id '+str(hosts[role]['pid'])+' -ErrorAction SilentlyContinue){exit 9}'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=0x08000000)
   if check.returncode==9:raise RuntimeError('Owned host is already running; refusing duplicate '+role)
   hosts.pop(role,None)
  prefix='CodeAssistBackground'+role
  if not args.skip_configuration:
   p=subprocess.Popen([PY,str(LAUNCH),'CodeAssistVerify-runtime',str(IDE/'devenv.exe'),'/RootSuffix',prefix,'/UpdateConfiguration','/Log',str(OUT/(role+'.module-setup.xml'))],env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,creationflags=0x08000000)
   try:out,err=p.communicate(timeout=90)
   except subprocess.TimeoutExpired:raise RuntimeError('Owned profile configuration has not finished; no host launched')
   (OUT/(role+'.module-setup.launch.log')).write_bytes(out+err)
   values=[json.loads(line) for line in out.decode().splitlines() if line.startswith('{')]
   if not any(v.get('exitCode')==0 for v in values):raise RuntimeError('Profile setup failed '+role)
  env['CODEASSIST_PARITY_ROLE']=role
  log=OUT/(role+'.module-host.launch.log');f=log.open('wb');host_args=[str(IDE/'devenv.exe'),str(RUN/role/'Parity.sln'),'/RootSuffix',prefix,'/Log',str(OUT/(role+'.module-host.activity.xml'))];broker=subprocess.Popen(host_args if args.visible_input_desktop else [PY,str(LAUNCH),'CodeAssistVerify-runtime',*host_args],env=env,stdout=f,stderr=subprocess.STDOUT,creationflags=0x08000000);f.close()
  if args.visible_input_desktop:hosts[role]={'pid':broker.pid,'brokerPid':broker.pid,'profile':prefix,'desktop':'Default'}
  deadline=time.monotonic()+15
  while role not in hosts and time.monotonic()<deadline:
   lines=log.read_text(errors='replace').splitlines()
   for line in lines:
    if line.startswith('{'):
     value=json.loads(line)
     if 'launchedPid' in value:hosts[role]={'pid':value['launchedPid'],'brokerPid':broker.pid,'profile':prefix,'desktop':value['desktop']};break
   if role in hosts:break
   time.sleep(.1)
  if role not in hosts:raise RuntimeError('No owned process receipt '+role)
  (RUN/'hosts.json').write_text(json.dumps(hosts,indent=2));print(role,hosts[role]['pid'],flush=True)
 return 0
if __name__=='__main__':raise SystemExit(main())
