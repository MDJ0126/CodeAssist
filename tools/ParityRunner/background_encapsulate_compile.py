import json,subprocess
from pathlib import Path
root=Path.cwd();out=root/'docs/validation/background-103/compiler-check';work=root/'.local/parity-runs/background-103/compiled';work.mkdir(exist_ok=True);rows={}
for role in ['VA','CA']:
 r=json.loads((root/('docs/validation/background-103/'+role+'.encapsulate.after.json')).read_text());text=r['after']['text'];name='Value' if role=='VA' else 'SetValue';get='Value' if role=='VA' else 'GetValue';code=text+'\nint main() { EncapAudit instance{}; instance.'+name+'(7); return instance.'+get+'() == 7 ? 0 : 1; }\n';src=out/(role+'-encapsulate-runtime.cpp');src.write_text(code,encoding='utf-8');exe=work/(role+'-encapsulate.exe');obj=work/(role+'-encapsulate.obj');dev=Path('C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/Tools/VsDevCmd.bat');command='"'+str(dev)+'" -no_logo -arch=x64 && cl /nologo /EHsc /std:c++17 /W4 /Fe:"'+str(exe)+'" /Fo:"'+str(obj)+'" "'+str(src)+'"';script=work/(role+'-build.cmd');script.write_text('@call '+command.split(' && ',1)[0]+'\n@'+command.split(' && ',1)[1]+'\n',encoding='utf-8');p=subprocess.run(['cmd.exe','/d','/c',str(script)],cwd=work,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000);(out/(role+'-encapsulate-runtime.build.log')).write_bytes(p.stdout);row=dict(compileExit=p.returncode,source=src.name)
 if p.returncode==0:
  run=subprocess.run([str(exe)],cwd=work,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=0x08000000,timeout=5);row.update(runExit=run.returncode,contract='setter(7) followed by getter() must equal 7',passed=run.returncode==0)
 rows[role]=row
(out/'encapsulate-runtime-result.json').write_text(json.dumps(rows,indent=2));print(rows)
