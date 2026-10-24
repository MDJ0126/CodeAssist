import json,subprocess,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'docs/validation/background-103/compiler-check';WORK=ROOT/'.local/parity-runs/background-103/compiled'
def build_run(label,role,code,contract):
 WORK.mkdir(exist_ok=True);src=OUT/(role+'-'+label+'.cpp');src.write_bytes(code.encode('utf-8'));exe=WORK/(role+'-'+label+'.exe');obj=WORK/(role+'-'+label+'.obj');dev=Path('C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/Tools/VsDevCmd.bat');script=WORK/(role+'-'+label+'-build.cmd');script.write_text('@call "'+str(dev)+'" -no_logo -arch=x64\n@cl /nologo /EHsc /std:c++17 /W4 /Fe:"'+str(exe)+'" /Fo:"'+str(obj)+'" "'+str(src)+'"\n',encoding='utf-8');p=subprocess.run(['cmd.exe','/d','/c',str(script)],cwd=WORK,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000,timeout=25);(OUT/(role+'-'+label+'.build.log')).write_bytes(p.stdout);row=dict(passed=False,compileExit=p.returncode,source=src.name,sourceSha256=hashlib.sha256(src.read_bytes()).hexdigest(),contract=contract)
 if p.returncode==0:
  run=subprocess.run([str(exe)],cwd=WORK,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000,timeout=5);row.update(runExit=run.returncode,passed=run.returncode==0,executableSha256=hashlib.sha256(exe.read_bytes()).hexdigest())
 return row
if __name__=='__main__':
 rows={}
 for role in ['VA','CA']:
  p=ROOT/('docs/validation/background-103/'+role+'.extract.after.json');r=json.loads(p.read_text());code=r['after']['text'];assert 'void StepOne() {}' in code and 'void StepTwo() {}' in code;code='int trace = 0;\n'+code.replace('void StepOne() {}','void StepOne() { trace = trace * 10 + 1; }').replace('void StepTwo() {}','void StepTwo() { trace = trace * 10 + 2; }')+'\nint main() { ExtractAudit(); return trace == 12 ? 0 : 1; }\n';rows[role]=build_run('extract-runtime',role,code,'ExtractAudit calls StepOne then StepTwo exactly once: trace == 12')
 (OUT/'extract-runtime-result.json').write_text(json.dumps(rows,indent=2));print(rows)
def build_files(label,role,files,contract):
 folder=WORK/(role+'-'+label);folder.mkdir(exist_ok=True);archive=OUT/(role+'-'+label);archive.mkdir(exist_ok=True)
 for name,text in files.items():
  assert Path(name).name==name and name not in ['.','..'];(folder/name).write_bytes(text.encode('utf-8'));(archive/name).write_bytes(text.encode('utf-8'))
 sources=' '.join('"'+n+'"' for n in files if n.endswith('.cpp'));dev=Path('C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/Tools/VsDevCmd.bat');script=folder/'build.cmd';script.write_text('@call "'+str(dev)+'" -no_logo -arch=x64\n@cl /nologo /EHsc /std:c++17 /W4 /Fe:program.exe '+sources+'\n',encoding='utf-8');p=subprocess.run(['cmd.exe','/d','/c',str(script)],cwd=folder,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000,timeout=30);(archive/'build.log').write_bytes(p.stdout);row=dict(compileExit=p.returncode,contract=contract,files={n:hashlib.sha256((archive/n).read_bytes()).hexdigest() for n in files},passed=False)
 if p.returncode==0:
  run=subprocess.run([str(folder/'program.exe')],cwd=folder,creationflags=0x08000000,timeout=5);row.update(runExit=run.returncode,passed=run.returncode==0)
 return row
