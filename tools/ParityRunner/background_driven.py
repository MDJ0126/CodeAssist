import json,subprocess,time
from pathlib import Path
from isolated_desktop import state,name,u,k
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'docs/validation/background-103';WORKER=ROOT/'tools/ShortcutProbe/BackgroundVerifierNext.exe'
def write(p,v):Path(p).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def spawn(args):return subprocess.Popen([str(WORKER),*map(str,args)],creationflags=0x08000000,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
def main():
 desktop=name(u.GetThreadDesktop(k.GetCurrentThreadId()))
 if desktop==state()['inputDesktop'] or not desktop.startswith('CodeAssistVerify-'):raise RuntimeError('Refusing input desktop')
 rows=[]
 for id in ['hashtags']:
  spec=OUT/(id+'.case.json');case=json.loads(spec.read_text());row={'id':id,'feature':case['feature'],'roles':{}}
  for role,pid in [('VA',37120),('CA',33620)]:
   result=OUT/(role+'.'+id+'.driven.json');worker=spawn(['case',pid,result,spec,role]);drivers=[]
   for i,d in enumerate(case.get('drivers',{}).get(role,[])):
    ds=OUT/(role+'.'+id+'.driver'+str(i)+'.json');dp=OUT/(role+'.'+id+'.driver'+str(i)+'.result.json');write(ds,d);p=spawn(['drive',pid,dp,ds])
    try:p.wait(timeout=15)
    except subprocess.TimeoutExpired:p.kill();p.wait()
    drivers.append(json.loads(dp.read_text()) if dp.exists() else {'acted':False,'error':'driver timeout'})
   try:worker.wait(timeout=12)
   except subprocess.TimeoutExpired:
    worker.kill();worker.wait();row['roles'][role]={'status':'timeout','drivers':drivers};write(OUT/'hashtags-status.json',{'rows':rows+[row],'halted':role,'active':id});return 2
   data=json.loads(result.read_text(encoding='utf-8-sig')) if result.exists() else {'status':'missing-result'};data['drivers']=drivers;row['roles'][role]=data
  rows.append(row);write(OUT/'hashtags-status.json',{'rows':rows,'active':None})
 return 0
if __name__=='__main__':raise SystemExit(main())
