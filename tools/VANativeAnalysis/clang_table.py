import json
from pathlib import Path
from analyze import PE

root = Path('C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936f/Extensions/zgy5sun1.aim')
pe = PE(root / 'valibclang2_64.dll')
table = 0x2d72878
slots = []
for i in range(128):
    address = pe.u64(pe.offset(table) + i * 8)
    rva = address - pe.base
    if not any(section['rva'] <= rva < section['rva'] + section['virtual_size'] and section['characteristics'] & 0x20000000 for section in pe.sections):
        break
    slots.append(dict(slot=i, rva=hex(rva), address=hex(address)))
Path('docs/reverse-engineering/va-2630/native/clang-interface-table.json').write_text(json.dumps(dict(
    table_rva=hex(table), global_rva='0x3b77300', initializer_rva='0x3690', export_rva='0x4e8e0',
    scope='Contiguous executable pointers at the address stored as the object first field. Slot signatures/roles unverified.', slots=slots), indent=2), encoding='utf-8')
Path('tools/VANativeAnalysis/clang-method-targets.txt').write_text('\n'.join(slot['rva'][2:] for slot in slots), encoding='utf-8')
print(f'{len(slots)} executable pointers in interface table')
