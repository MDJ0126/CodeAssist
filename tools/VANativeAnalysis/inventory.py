"""Fingerprint every installed file, including nested components and resources."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct

def classify(data, suffix):
    if data[:2] != b'MZ': return dict(kind='resource',extension=suffix)
    at=struct.unpack_from('<I',data,0x3c)[0]
    if data[at:at+4] != b'PE\0\0': return dict(kind='unknown-image',extension=suffix)
    machine=struct.unpack_from('<H',data,at+4)[0]
    optional=at+24
    magic=struct.unpack_from('<H',data,optional)[0]
    directories=optional+(112 if magic==0x20b else 96)
    clr,size=struct.unpack_from('<II',data,directories+14*8)
    return dict(kind='managed-or-mixed' if clr else 'native',machine=hex(machine),
        pe_magic=hex(magic),clr_rva=clr,clr_size=size,extension=suffix)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('root');parser.add_argument('output');args=parser.parse_args()
    root=Path(args.root);rows=[]
    for file in sorted(root.rglob('*')):
        if not file.is_file(): continue
        data=file.read_bytes()
        rows.append(dict(path=file.relative_to(root).as_posix(),size=len(data),sha256=hashlib.sha256(data).hexdigest(),**classify(data,file.suffix.lower())))
    report=dict(root=str(root),files=len(rows),bytes=sum(row['size'] for row in rows),
        kinds=dict(Counter(row['kind'] for row in rows)),scope='Recursive inventory only; hashing is not behavior analysis.',items=rows)
    Path(args.output).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({key:report[key] for key in ('files','bytes','kinds')}))
