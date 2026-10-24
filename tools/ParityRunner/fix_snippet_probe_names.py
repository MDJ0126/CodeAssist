from pathlib import Path
import re
p=Path('tools/ParityRunner/LiveProbe/LiveProbe.cs');s=p.read_text(encoding='utf-8-sig');a=s.index('if(op=="native-snippet-editor")');b=s.index('if(op=="utility-tab-read")',a);part=s[a:b];part=re.sub(r'\btitle\b','snippetTitle',part);part=re.sub(r'\bimage\b','snippetEditorImage',part);part=part.replace('return new{snippetTitle=snippetTitle.Text','return new{title=snippetTitle.Text').replace('text,snippetEditorImage,','text,image=snippetEditorImage,');s=s[:a]+part+s[b:];p.write_text(s,encoding='utf-8-sig')
