import sys,time,json
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import OUT,save

def opt(role,op,**kw):
 v,_=module(role,op,windowTitle='Visual Assist Options' if role=='VA' else 'CodeAssist Options',**kw)
 if v.get('error'):raise RuntimeError(v['error'])
 return v

def opened(role,page,**extra):
 module(role,'open-options',windowTitle='')
 for n in range(35):
  v=opt(role,'options-read',page=page,**extra)
  if v.get('result',{}).get('status')=='observed':return v
  time.sleep(.1)
 raise RuntimeError(v)
j={}
try:
 v=opened('VA','Checker Sets',inspectCheckerRows=True);save('VA.inspection-level.original.json',v);assert v['result']['rows'][0]['context']['RawName']=='readability-uniqueptr-delete-release' and v['result']['rows'][0]['selected']==2
 v=opt('VA','options-edit',checker='readability-uniqueptr-delete-release',index=4);save('VA.inspection-level.changed.json',v);opt('VA','options-button',button='Save')
 v=opened('VA','Checker Sets',inspectCheckerRows=True);save('VA.inspection-level.saved-readback.json',v);j['nativeLevel5ReadBack']=v['result']['rows'][0]['selected']==4;assert j['nativeLevel5ReadBack'];opt('VA','options-button',button='Cancel')
 v=opened('CA','Checker Sets',inspectCheckerRows=True);save('CA.inspection-level.rows.json',v);assert v['result']['rows']==[];v=opt('CA','options-read',page='Checker Sets');save('CA.inspection-level.controls.json',v);opt('CA','options-button',button='Cancel');print(j,flush=True)
finally:
 try:
  v=opened('VA','Checker Sets',inspectCheckerRows=True);opt('VA','options-edit',checker='readability-uniqueptr-delete-release',index=2);opt('VA','options-button',button='Save');v=opened('VA','Checker Sets',inspectCheckerRows=True);save('VA.inspection-level.restored.json',v);j['nativeOriginalLevel3Restored']=v['result']['rows'][0]['selected']==2;opt('VA','options-button',button='Cancel')
 except Exception as e:j['restoreError']=str(e)
 save('inspection-level.transaction.json',j);print(j,flush=True)
