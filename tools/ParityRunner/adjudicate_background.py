from pathlib import Path
import json,hashlib,datetime
ROOT=Path(__file__).resolve().parents[2]/'docs/validation/background-103'
def read(file):return json.loads((ROOT/file).read_text(encoding='utf-8-sig'))
def update(number,state,reason,files,checks):
 assert state in ['representative-match','different'] and checks and all(isinstance(v,bool) for v in checks.values())
 if state=='representative-match':assert all(checks.values())
 features=read('features.json');data=read('adjudicated.json');id='VA-F%03d'%number;evidence={}
 for role,file in files.items():
  path=ROOT/file;evidence[role]=dict(file=file,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),data=read(file))
 row=next(x for x in features if x['id']==id);row.update(state=state,reason=reason,mainEvidence='background-103/'+files['VA'],comparisonEvidence='background-103/'+files['CA'],fullFeatureCertified=False,checkedAssertions=checks)
 previous=next((x for x in data['newJudgments'] if x['id']==id),None)
 if previous:
  folder=ROOT/'judgment-history';folder.mkdir(exist_ok=True);name=id+'-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f')+'.json';(folder/name).write_text(json.dumps(previous,ensure_ascii=False,indent=2),encoding='utf-8');data.setdefault('judgmentHistory',[]).append('judgment-history/'+name)
 data['newJudgments']=[x for x in data['newJudgments'] if x['id']!=id]+[dict(id=id,state=state,reason=reason,evidence=evidence,checkedAssertions=checks)]
 data['counts']={s:sum(x['state']==s for x in features) for s in ['representative-match','different','unverified']};data['determinate']=108-data['counts']['unverified'];data['updatedAt']=datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=9))).isoformat();(ROOT/'features.json').write_text(json.dumps(features,ensure_ascii=False,indent=2));(ROOT/'adjudicated.json').write_text(json.dumps(data,ensure_ascii=False,indent=2));print(id,data['counts'],flush=True)
