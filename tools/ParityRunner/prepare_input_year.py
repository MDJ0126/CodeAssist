from pathlib import Path
import json
p=Path('tools/ParityRunner/background_snippet_shared.py');s=p.read_text(encoding='utf-8-sig').replace('BackgroundOptionsV2.exe','BackgroundOptionsV28.exe').replace('.snippet-header.final','.snippet-input-year').replace("OUT/'snippet-year.drive.json'","OUT/'snippet-year2037.drive.json'")
s=s.replace("(OUT/'snippet-library-restore.json')","(OUT/'snippet-input-year.library-restore.json')")
Path('tools/ParityRunner/background_snippet_input_year.py').write_text(s,encoding='utf-8-sig');out=Path('docs/validation/background-103');(out/'snippet-year2037.drive.json').write_text(json.dumps(dict(title='VA Snippet: Doxygen - Header Comment',button='OK',edit='2037')),encoding='utf-8')
