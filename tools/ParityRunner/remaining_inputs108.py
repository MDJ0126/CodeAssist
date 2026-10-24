import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner')
from background_steps import setup,save,OUT,launch
from background_client import module,send,RUN
from compiler_contracts import build_run
from PIL import Image
B=Path('docs/validation/background-103');h=json.loads((RUN/'hosts.json').read_text())
for c in json.loads((B/'input-cases.json').read_text())['cases']:
 if c['feature'] not in [42,50]:continue
 for role in ['VA','CA']:
  prepared=setup(role,'new-'+c['id']+'108',c['text'],c['start'],c.get('length',0));steps=[]
  for i,action in enumerate(c['actions']):
   v,_=send(role,dict(action,kind='input',expectedFile=prepared['before']['file']));save(role+'.'+c['id']+'.step'+str(i)+'.json',v);assert v.get('status')=='complete',v;steps.append(v)
  save(role+'.'+c['id']+'.result.json',{'before':prepared['before'],'steps':steps,'after':steps[-1].get('after')});print(c['feature'],role,repr(steps[-1].get('after',{}).get('text')),flush=True)
text='bool Suggestion108() { bool value = ; return value; }\r\n'
for role in ['VA','CA']:
 setup(role,'boolean108',text,text.index('; return'));v,_=module(role,'posted-text',windowTitle='',text='tru');save(role+'.boolean.typed.json',v)
 if role=='VA':
  f=OUT/'VA.boolean.native-list.json';w=launch(['native-list-snapshot',h[role]['pid'],f]);w.wait(timeout=10);lists=json.loads(f.read_text());assert len(lists)==1,lists;v,_=module(role,'native-list-accept',windowTitle='',hwnd=lists[0]['hwnd'],choice='true',expectedText=v['result']['after']['text'])
 else:
  session,_=module(role,'completion-read',windowTitle='');save('CA.boolean.session.json',session);v,_=module(role,'completion-commit',windowTitle='',choice='true')
 save(role+'.boolean.accept.json',v);assert v['status']=='complete',v;code=v['result']['after']['text'];r=build_run('boolean108',role,code+'\nint main(){return Suggestion108()?0:1;}\n','accepted true evaluates true');save(role+'.boolean.compiler.json',r);print(52,role,r['passed'],flush=True)
text='void Line108() {\r\n    int first = 1;\r\n    int second = 2;\r\n}\r\n'
for role in ['VA','CA']:
 setup(role,'line108',text,text.index('first')+2)
 rows=[]
 for label,offset in [('first',text.index('first')+2),('second',text.index('second')+2)]:
  module(role,'caret-set',windowTitle='',offset=offset);v,_=send(role,{});save(role+'.line.'+label+'.render.json',v);entry=next(x for x in v['rows']if x['label']=='editor'and x['status']=='captured');im=Image.open(entry['path']).convert('RGB');x=im.width-5;pixels=[im.getpixel((x,y)) for y in range(im.height)];rows.append({'caret':offset,'image':entry['path'],'edgeRows':[y for y,c in enumerate(pixels)if max(c)-min(c)<4 and 50<=c[0]<=150]})
 save(role+'.line.frame-observation.json',{'states':rows,'scope':'current installed combined IDE/extension line frame; no VA-only causal assertion'});print(7,role,rows[0]['edgeRows'][:8],rows[1]['edgeRows'][:8],flush=True)
