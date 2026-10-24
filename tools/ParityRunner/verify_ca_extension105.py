import sys,json,time
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');from background_client import module
P=Path('.local/parity-runs/remaining-105');title='CodeAssist Options';j={}
def opt(op,**kw):
 for n in range(30):
  v,_=module('CA',op,windowTitle=title,**kw)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
try:
 module('CA','open-options',windowTitle='');v=opt('options-read',page='File Extensions');boxes=[x for x in v['result']['controls'] if x['type']=='TextBox' and x['bounds']['x']>245];assert len(boxes)==1;old=boxes[0]['value'];assert old=='';j['original']=old;(P/'CA.extension.transaction.json').write_text(json.dumps(j));opt('options-edit',setting='Extensions105',oldText=old,newText='.ca105');opt('options-button',button='Save');module('CA','index-reinitialize',windowTitle='',_timeout=40);v,_=module('CA','index-state',windowTitle='',query='CustomExtension105');(P/'CA.extension-after-add.index.json').write_text(json.dumps(v,ensure_ascii=False,indent=2));assert len(v['result']['definitions'])==1,v;print('CA custom extension actually indexed',v['result']['definitions'],flush=True)
finally:
 if 'original' in j:
  module('CA','open-options',windowTitle='');opt('options-read',page='File Extensions');opt('options-edit',setting='Extensions105',oldText='.ca105',newText=j['original']);opt('options-button',button='Save');j['restored']=True;(P/'CA.extension.transaction.json').write_text(json.dumps(j));module('CA','index-reinitialize',windowTitle='',_timeout=40);v,_=module('CA','index-state',windowTitle='',query='CustomExtension105');(P/'CA.extension-after-restore.index.json').write_text(json.dumps(v,ensure_ascii=False,indent=2));assert not v['result']['definitions'];print('CA option restored; extension no longer indexed',flush=True)
