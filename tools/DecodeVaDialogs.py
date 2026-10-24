"""Decode Win32 dialogs using the payload offsets and SHA256 returned by REA."""
import json,struct,hashlib,sys
from pathlib import Path

def decode(blob):
    pos=0
    def take(fmt):
        nonlocal pos
        v=struct.unpack_from(fmt,blob,pos);pos+=struct.calcsize(fmt);return v[0] if len(v)==1 else v
    def field():
        w=take('<H')
        if not w:return ''
        if w==65535:return {'ordinal':take('<H')}
        words=[w]
        while True:
            w=take('<H')
            if not w:break
            words.append(w)
        return struct.pack('<'+'H'*len(words),*words).decode('utf-16-le')
    extended=blob[:4]==b'\x01\x00\xff\xff'
    if extended:take('<HH');take('<I');ex=take('<I');style=take('<I')
    else:style=take('<I');ex=take('<I')
    count=take('<H');rect=take('<hhhh');field();field();title=field()
    if style&64:
        take('<H')
        if extended:take('<HBB')
        field()
    controls=[]
    for i in range(count):
        pos=(pos+3)&~3
        if extended:take('<I');cex=take('<I');cs=take('<I')
        else:cs=take('<I');cex=take('<I')
        cr=take('<hhhh');cid=take('<I' if extended else '<H');cc=field();ct=field();extra=take('<H')
        if extra:pos+=extra-2
        controls.append({'id':cid,'rectDialogUnits':cr,'class':cc,'text':ct,'style':hex(cs)})
    return {'extended':extended,'title':title,'rectDialogUnits':rect,'controls':controls}

def main():
    source=Path(sys.argv[1]);out=Path(sys.argv[2]);e=json.loads(source.read_text(encoding='utf-8-sig'));n=e['normalized_result'];data=Path(n['artifact']['path']).read_bytes()
    assert hashlib.sha256(data).hexdigest()==n['artifact']['sha256']
    dialogs=[]
    for res in n['resources']:
        if res['type'].get('id')!=5:continue
        loc=res['payload']['location'];blob=data[loc['offset']:loc['offset']+loc['bytes']]
        assert hashlib.sha256(blob).hexdigest()==res['payload']['sha256']
        dialogs.append({'resourceId':res['name'],'location':loc,'payloadSHA256':res['payload']['sha256'],**decode(blob)})
    out.write_text(json.dumps({'reaEvidenceId':e['evidence_id'],'artifactSHA256':n['artifact']['sha256'],'scope':'REA-verified Win32 dialog resource layout; not runtime defaults or native decompilation','dialogs':dialogs},ensure_ascii=False,indent=2),encoding='utf-8')
    print('Decoded dialogs:',len(dialogs))
    for d in dialogs:
        matches=[c for c in d['controls'] if any(t in str(c['text']).lower() for t in ('surround','extend multi','repair case','format after','closing','smart select'))]
        if matches:print(d['resourceId'],json.dumps(matches,ensure_ascii=False))
if __name__=='__main__':main()