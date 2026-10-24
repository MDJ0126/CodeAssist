import sys,json,time,subprocess,os,uuid,hashlib
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save,launch,OUT
from background_client import module,RUN
from pypdf import PdfReader
from pypdf.generic import ContentStream
import pypdfium2
h=json.loads((RUN/'hosts.json').read_text());text='// Color printing 108\r\nclass PrintType108 { public: int Value() { return 108; } };\r\nint main(){PrintType108 value;return value.Value();}\r\n';env=os.environ.copy();env['CODEASSIST_FOREGROUND_VALIDATION']='1'
for role in ['VA','CA']:
 setup(role,'print-color108',text,0);v,_=module(role,'queue-command',windowTitle='',command='File.Print' if role=='VA' else '404',focusEditor=True);save(role+'.print.queued.json',v)
 time.sleep(.7);pid=h[role]['pid'];f=OUT/(role+'.print-dialog.json');w=launch(['ui-tree',pid,f]);w.wait(timeout=10);d=json.loads(f.read_text());assert any(x.get('name')=='Microsoft Print to PDF'for x in d), 'Expected virtual printer absent'
 exe=str(Path('tools/ParityRunner/PrivatePrint108.exe').resolve());req=OUT/(role+'.print.request.json');r=subprocess.run([exe,str(pid),str(req)],env=env,capture_output=True,timeout=15,creationflags=0x08000000);assert r.returncode==0,req.read_text()
 pdf=RUN/role/('print-color108-'+uuid.uuid4().hex[:8]+'.pdf')
 for i in range(25):
  time.sleep(.15);w=launch(['windows',pid,OUT/(role+'.print-save-windows.json')]);w.wait(timeout=6);wins=json.loads((OUT/(role+'.print-save-windows.json')).read_text());
  if any(x['title']=='다음 이름으로 프린터 출력 저장' and x['visible']for x in wins):break
 req=OUT/(role+'.print-save.request.json');r=subprocess.run([exe,str(pid),str(req),str(pdf.resolve())],env=env,capture_output=True,timeout=15,creationflags=0x08000000);assert r.returncode==0,req.read_text()
 for i in range(60):
  if pdf.exists() and pdf.stat().st_size>200:break
  time.sleep(.1)
 reader=PdfReader(str(pdf));alltext=''.join(page.extract_text()or''for page in reader.pages);assert 'PrintType108' in alltext;colors=sorted(set((op.decode(),tuple(float(x)for x in operands))for page in reader.pages for operands,op in ContentStream(page.get_contents(),reader).operations if op in [b'rg',b'g',b'k']));render=pypdfium2.PdfDocument(str(pdf));render[0].render(scale=1.4).to_pil().save(str(OUT/(role+'.print-page.png')));save(role+'.print.result.json',{'pdf':str(pdf),'sha256':hashlib.sha256(pdf.read_bytes()).hexdigest(),'text':alltext,'colors':colors,'sourceVerified':True,'scope':'actual Microsoft Print to PDF output; no physical printer'});print(66,role,colors,len(alltext),flush=True)
