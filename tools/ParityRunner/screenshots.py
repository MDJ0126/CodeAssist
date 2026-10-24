"""Validate captured images and build a paired, evidence-linked gallery. No pixel score is a product similarity score."""
import json,sys,hashlib,html,collections
from pathlib import Path
from PIL import Image,ImageStat,ImageOps,ImageDraw

def analyze(root):
 root=Path(root);folder=root/'screenshots';plan=json.loads((root/'VA.plan.json').read_text(encoding='utf-8-sig'));rows=[];counts=collections.Counter();cards=[]
 results={role:json.loads((root/(role+'.results.json')).read_text(encoding='utf-8-sig')) if (root/(role+'.results.json')).exists() else {} for role in ('VA','CA')}
 for c in plan['cases']:
  entry={'id':c['id'],'feature':c['feature'],'captures':{}}
  for role in ('VA','CA'):
   for phase in ('before','after','dialog'):
    key=role+'-'+c['id']+'-'+phase;meta=folder/(key+'.json');rec={'status':'missing'}
    if meta.exists():
     rec=json.loads(meta.read_text(encoding='utf-8-sig'));image=folder/rec.get('file','missing')
     if rec.get('role')!=role or rec.get('id')!=c['id'] or rec.get('phase')!=phase or rec.get('targetPid')!=results[role].get('processId'):rec['status']='evidence-mismatch'
     if rec.get('status')=='captured' and image.exists():
      try:
       im=Image.open(image).convert('RGB');im.load();stat=ImageStat.Stat(im.resize((128,128)))
       rec['sha256']=hashlib.sha256(image.read_bytes()).hexdigest();rec['pixelStdDev']=stat.stddev
       if max(stat.stddev)<1:rec['status']='blank-image'
       if im.size!=(rec.get('width'),rec.get('height')):rec['status']='dimension-mismatch'
      except Exception as e:rec['status']='invalid-image';rec['validationError']=str(e)
    if phase!='dialog':counts[rec['status']]+=1
    entry['captures'][role+'-'+phase]=rec
  rows.append(entry)
  cells=[]
  for role in ('VA','CA'):
   parts=[]
   for phase in ('before','dialog','after'):
    rec=entry['captures'][role+'-'+phase];f=rec.get('file');label=role+' / '+phase+' / '+rec['status']
    parts.append('<figure><figcaption>'+html.escape(label)+'</figcaption>'+('<a href="screenshots/'+html.escape(f)+'"><img loading="lazy" src="screenshots/'+html.escape(f)+'"></a>' if f and (folder/f).exists() else '')+'</figure>')
   cells.append('<td>'+''.join(parts)+'</td>')
  cards.append('<tr><th>'+html.escape(c['id'])+'<br>Feature '+str(c['feature'])+'</th>'+''.join(cells)+'</tr>')
 expected=len(plan['cases'])*2*2;summary={'expectedBeforeAfter':expected,'validBeforeAfter':counts['captured'],'captureCoveragePercent':round(100*counts['captured']/expected,2),'counts':dict(counts),'featureSimilarity':None,'warning':'Capture coverage is not functional pass rate or visual similarity. Dialog images are additional and counted separately.'}

 # Link every screenshot set to the command result without inferring functionality from pixels.
 if (root/'comparison.json').exists():
  comparison=json.loads((root/'comparison.json').read_text(encoding='utf-8-sig'))
  linked={c['id']:c for c in comparison['cases']}
  for row in rows:
   result=linked.get(row['id'],{});row['executionComparison']=result
   states=result.get('sides',{}).values()
   if 'requested-page-not-observed' in states:action='Resolve native options page selection; images show the opened page, not necessarily the requested one.'
   elif 'execution-error' in states:action='Verify VA command name, availability and fixture prerequisites; do not infer missing product functionality.'
   elif 'product-error-dialog' in states:action='Inspect captured error and supply a supported declaration/expression before classifying a product defect.'
   elif result.get('status')=='different':action='Compare actual buffers/selection and the paired images.'
   elif result.get('status')=='scenario-match':action='This sample matched its assertions; remaining feature gates are unverified.'
   else:action='Add explicit behavior assertions; opening/cancelling a dialog is not a feature pass.'
   row['nextAction']=action

 modal_groups={}
 for role in ('VA','CA'):
  hashes=collections.defaultdict(list)
  for row in rows:
   rec=row['captures'][role+'-dialog']
   if rec.get('status')=='captured':hashes[rec['sha256']].append(row['id'])
  modal_groups[role]={'uniqueImages':len(hashes),'duplicateGroups':[ids for ids in hashes.values() if len(ids)>1]}
 summary['dialogImageGroups']=modal_groups
 summary['validDialogImages']=sum(row['captures'][role+'-dialog'].get('status')=='captured' for row in rows for role in ('VA','CA'))
 summary['totalValidImages']=summary['validBeforeAfter']+summary['validDialogImages']
 (root/'screenshots-analysis.json').write_text(json.dumps({'summary':summary,'cases':rows},ensure_ascii=False,indent=2),encoding='utf-8')
 (root/'screenshots.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><title>시나리오 화면 대조</title><style>body{background:#191c22;color:#eee;font:16px system-ui;margin:24px}table{width:100%;table-layout:fixed}th{width:130px}td{width:45%;vertical-align:top;border:1px solid #596170}img{width:100%}figure{margin:10px}a{color:#ffd955}</style><h1>자동 화면 수집·대조</h1><p>'+html.escape(json.dumps(summary,ensure_ascii=False))+'</p><p>전체 유사도 미확인. 화면별 원본 링크를 열어 확대할 수 있습니다. 스크린샷만으로 기능 통과를 인증하지 않습니다.</p><table><tr><th>항목</th><th>VA</th><th>CA</th></tr>'+''.join(cards)+'</table></html>',encoding='utf-8')
 # Contact sheets for batch visual review; preserve full-size originals.
 for offset in range(0,len(rows),12):
  sheet=Image.new('RGB',(1600,12*250),(30,30,30));draw=ImageDraw.Draw(sheet)
  for i,row in enumerate(rows[offset:offset+12]):
   draw.text((8,i*250+3),row['id'],fill='white')
   for j,role in enumerate(('VA','CA')):
    rec=row['captures'][role+'-dialog']
    if rec.get('status')!='captured':rec=row['captures'][role+'-after']
    f=rec.get('file')
    if f and (folder/f).exists():
     im=Image.open(folder/f).convert('RGB');im.thumbnail((780,220));sheet.paste(im,(j*800+10,i*250+25))
    draw.text((j*800+10,i*250+10),role+' '+rec['status'],fill='yellow')
  sheet.save(root/('contact-%03d.png'%(offset+1)))
 print(json.dumps(summary,ensure_ascii=False));return summary
if __name__=='__main__':analyze(sys.argv[1])
