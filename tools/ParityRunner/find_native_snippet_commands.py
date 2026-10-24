from pathlib import Path
import re,json
root=Path('C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936fCodeAssistBackgroundVA/Extensions/n0rml0rj.3lj');p=root/'VA_X64.dll';data=p.read_bytes();found=set()
for text in [data.decode('latin1'),data.decode('utf-16-le',errors='ignore'),data[1:].decode('utf-16-le',errors='ignore')]:
 found.update(re.findall(r'[A-Za-z0-9_.]{0,45}Snippet[A-Za-z0-9_.]{0,60}',text))
print(sorted(found)[:100])
