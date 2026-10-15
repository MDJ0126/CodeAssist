import pathlib,struct,json
b=pathlib.Path('C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936f/Extensions/zgy5sun1.aim/VA_X64.dll').read_bytes();pe=struct.unpack_from('<I',b,0x3c)[0];op=pe+24
ns=struct.unpack_from('<H',b,pe+6)[0];sz=struct.unpack_from('<H',b,pe+20)[0];sects=[]
for i in range(ns):
 o=op+sz+i*40;vs,rv,rs,ro=struct.unpack_from('<IIII',b,o+8);sects.append((rv,max(vs,rs),ro))
def off(rv):
 for v,s,o in sects:
  if v<=rv<v+s:return o+rv-v
 raise ValueError(rv)
def cstr(o):return b[o:b.index(bytes([0]),o)].decode('ascii',errors='replace')
d=off(struct.unpack_from('<I',b,op+120)[0]);m={}
while True:
 oft,tm,fc,name,ft=struct.unpack_from('<IIIII',b,d)
 if not any([oft,tm,fc,name,ft]):break
 dll=cstr(off(name));t=off(oft or ft);i=0
 while True:
  v=struct.unpack_from('<Q',b,t+i*8)[0]
  if not v:break
  sym='ordinal:'+str(v&65535) if v>>63 else cstr(off(v)+2)
  m[hex(ft+i*8)]={'dll':dll,'symbol':sym};i+=1
 d+=20
pathlib.Path('tools/obj/rea/va-import-map.json').write_text(json.dumps(m),encoding='utf-8')
for k,v in m.items():
 if k in ['0xdb7060','0xdb6ee8'] or v['symbol'] in ['CreateFileW','CreateFileA','WriteFile','ReadFile']:print(k,v)
