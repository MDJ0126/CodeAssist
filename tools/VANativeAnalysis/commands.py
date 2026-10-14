"""Collect registered command strings without executing vendor binaries."""
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
metadata = json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
commands = {}
for path in root.rglob('*'):
    if path.is_file() and path.suffix.lower() in ('.dll', '.exe', '.slp'):
        data = path.read_bytes()
        for encoding, pattern in [('ascii', rb'VAssistX\.[A-Za-z][A-Za-z0-9_]+'),
                                  ('utf-16le', rb'V\x00A\x00s\x00s\x00i\x00s\x00t\x00X\x00\.\x00(?:[A-Za-z0-9_]\x00)+')]:
            for match in re.finditer(pattern, data):
                name = match.group().decode(encoding)
                commands.setdefault(name, []).append(dict(file=str(path.relative_to(root)), offset=match.start(), encoding=encoding))
keybinds = [item for item in metadata if 'KeyBind' in item['File']]
output = Path('docs/reverse-engineering/va-2630/command-inventory.json')
output.write_text(json.dumps(dict(scope='Command strings and managed keybind metadata; presence does not prove execution or parity.',
    count=len(commands), commands=commands, keybind_assemblies=keybinds), ensure_ascii=False, indent=2), encoding='utf-8')
print(f'{len(commands)} distinct VAssistX command strings; {len(keybinds)} keybind assemblies')
