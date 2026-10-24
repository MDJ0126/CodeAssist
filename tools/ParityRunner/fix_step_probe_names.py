from pathlib import Path
p=Path('tools/ParityRunner/LiveProbe/LiveProbe.cs');s=p.read_text(encoding='utf-8-sig');a=s.index('if(op=="native-step-open")');b=s.index('if(op=="native-memory-read"',a);part=s[a:b].replace('var controls=','var nativeStepControls=').replace('controls=controls.Select','controls=nativeStepControls.Select');s=s[:a]+part+s[b:];p.write_text(s,encoding='utf-8-sig')
