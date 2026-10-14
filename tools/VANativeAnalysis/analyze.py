"""Static AMD64 PE inventory and string cross-references, using installed LLVM.

No target DLL is loaded or executed. .pdata describes unwind ranges, not every
possible source function. An xref proves a machine instruction uses an address;
it does not by itself establish a feature's semantics.
"""
import argparse
import bisect
import hashlib
import json
from pathlib import Path
import re
import struct
import subprocess


class PE:
    def __init__(self, filename):
        self.path = Path(filename)
        self.data = self.path.read_bytes()
        if self.data[:2] != b'MZ':
            raise ValueError('Not a PE image')
        pe = self.u32(0x3c)
        if self.data[pe:pe+4] != b'PE\0\0':
            raise ValueError('Invalid PE signature')
        self.machine = self.u16(pe+4)
        optional = pe+24
        if self.u16(optional) != 0x20b:
            raise ValueError('Only PE32+ is supported')
        self.base = self.u64(optional+24)
        self.directories = [struct.unpack_from('<II', self.data, optional+112+i*8) for i in range(16)]
        table = optional+self.u16(pe+20)
        self.sections = []
        for i in range(self.u16(pe+6)):
            at = table+i*40
            self.sections.append(dict(name=self.data[at:at+8].rstrip(b'\0').decode('ascii'),
                virtual_size=self.u32(at+8), rva=self.u32(at+12), raw_size=self.u32(at+16),
                raw_offset=self.u32(at+20), characteristics=self.u32(at+36)))
        self.functions = []
        rva, size = self.directories[3]
        if rva:
            at = self.offset(rva)
            for i in range(size//12):
                start, end, unwind = struct.unpack_from('<III', self.data, at+i*12)
                self.functions.append(dict(start=start, end=end, unwind=unwind))
        self.functions.sort(key=lambda f: f['start'])
        self.starts = [f['start'] for f in self.functions]

    def u16(self, at): return struct.unpack_from('<H', self.data, at)[0]
    def u32(self, at): return struct.unpack_from('<I', self.data, at)[0]
    def u64(self, at): return struct.unpack_from('<Q', self.data, at)[0]
    def offset(self, rva):
        for section in self.sections:
            if section['rva'] <= rva < section['rva']+section['raw_size']:
                return section['raw_offset']+rva-section['rva']
        raise ValueError(f'RVA not backed by file bytes: {rva:#x}')
    def function_at(self, rva):
        i = bisect.bisect_right(self.starts, rva)-1
        if i >= 0 and rva < self.functions[i]['end']:
            return self.functions[i]
        return None
    def exports(self):
        rva, _ = self.directories[0]
        if not rva: return []
        at = self.offset(rva)
        count = self.u32(at+24)
        names = self.offset(self.u32(at+32))
        ordinals = self.offset(self.u32(at+36))
        addresses = self.offset(self.u32(at+28))
        result = []
        for i in range(count):
            name_at = self.offset(self.u32(names+4*i))
            name = self.data[name_at:self.data.index(b'\0', name_at)].decode('ascii')
            ordinal = self.u16(ordinals+2*i)
            result.append(dict(name=name, rva=self.u32(addresses+4*ordinal)))
        return result
    def strings(self):
        result = []
        focus = re.compile(r'parse|scope|symbol|database|cache|unreal|UCLASS|UPROPERTY|UFUNCTION|TObjectPtr|template|overload|completion|refactor|findref|clang', re.I)
        for section in self.sections:
            if section['characteristics'] & 0x20000000:
                continue
            data = self.data[section['raw_offset']:section['raw_offset']+section['raw_size']]
            for encoding, pattern in [('ascii', rb'[\x20-\x7e]{5,}'), ('utf-16le', rb'(?:[\x20-\x7e]\x00){5,}')]:
                for match in re.finditer(pattern, data):
                    value = match.group().decode(encoding)
                    if focus.search(value):
                        result.append(dict(rva=section['rva']+match.start(), bytes=len(match.group()),
                            encoding=encoding, text=value[:512], truncated=len(value)>512))
        return sorted(result, key=lambda s: s['rva'])


def analyze(pe, objdump, output, scratch):
    strings = pe.strings()
    by_start = {s['rva']:s for s in strings}
    starts = sorted(by_start)
    dump = scratch/(pe.path.name+'.asm.txt')
    with dump.open('w', encoding='utf-8') as stream:
        subprocess.run([objdump, '--disassemble', '--no-show-raw-insn', '--x86-asm-syntax=intel',
            str(pe.path)], stdout=stream, stderr=subprocess.PIPE, text=True, check=True)
    xrefs = []
    for line in dump.open(encoding='utf-8'):
        instruction = re.match(r'^([0-9a-f]+):\s+(.*)', line)
        target = re.search(r'#\s*(0x[0-9a-f]+)', line)
        if not instruction or not target:
            continue
        target_rva = int(target.group(1),16)-pe.base
        i = bisect.bisect_right(starts, target_rva)-1
        if i < 0: continue
        item = by_start[starts[i]]
        if target_rva >= item['rva']+item['bytes']: continue
        rva = int(instruction.group(1),16)-pe.base
        function = pe.function_at(rva)
        xrefs.append(dict(instruction_rva=rva, target_rva=target_rva, string_rva=item['rva'],
            function_start=function['start'] if function else None, instruction=instruction.group(2), text=item['text']))
    export_ranges = []
    for export in pe.exports():
        function = pe.function_at(export['rva'])
        if function and re.match(r'GetVaService|VaConnectSetup|DllGetClassObject', export['name']):
            destination = output/(pe.path.name+'.'+export['name']+'.asm.txt')
            with destination.open('w',encoding='utf-8') as stream:
                subprocess.run([objdump,'--disassemble','--no-show-raw-insn','--x86-asm-syntax=intel',
                    f"--start-address={pe.base+function['start']}",f"--stop-address={pe.base+function['end']}",str(pe.path)],
                    stdout=stream,stderr=subprocess.PIPE,text=True,check=True)
            export_ranges.append(dict(**export,end=function['end'],artifact=destination.name))
    report = dict(file=pe.path.name,sha256=hashlib.sha256(pe.data).hexdigest(),image_base=hex(pe.base),
        size=len(pe.data),sections=pe.sections,unwind_range_count=len(pe.functions),
        exports=pe.exports(),export_ranges=export_ranges,focused_strings=strings,string_xrefs=xrefs,
        scope='All executable sections disassembled. Strings/xrefs are discovery evidence, not semantic verification.')
    (output/(pe.path.name+'.native.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    (scratch/(pe.path.name+'.functions.json')).write_text(json.dumps(pe.functions),encoding='utf-8')
    print(f'{pe.path.name}: {len(pe.functions)} unwind ranges, {len(strings)} focused strings, {len(xrefs)} direct string xrefs')


if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('extension')
    parser.add_argument('objdump')
    parser.add_argument('--output',default='docs/reverse-engineering/va-2630/native')
    parser.add_argument('--scratch',default='tools/obj/va-native')
    parser.add_argument('--files',nargs='+',default=['VA_X64.dll','VaAux64.dll','vaIPC64.dll','valibclang2_64.dll'])
    args=parser.parse_args()
    output=Path(args.output);output.mkdir(parents=True,exist_ok=True)
    scratch=Path(args.scratch);scratch.mkdir(parents=True,exist_ok=True)
    for name in args.files: analyze(PE(Path(args.extension)/name),args.objdump,output,scratch)
