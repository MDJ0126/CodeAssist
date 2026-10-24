from pathlib import Path
import json,collections
import numpy as np
from PIL import Image,ImageDraw
P=Path('C:/Projects/CodeAssist');O=P/'docs/validation/automated-runner-20261018/20261018-021854-OptionsVerified';C=P/'docs/validation/debug-0.0.90';D=P/'docs/validation/layout-audit-0.0.90'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def bbox(mask,offset=(0,0)):
 y,x=np.where(mask)
 return None if not len(x) else [int(x.min()+offset[0]),int(y.min()+offset[1]),int(x.max()-x.min()+1),int(y.max()-y.min()+1)]
def components(mask,off):
 visited=np.zeros(mask.shape,bool);out=[];H,W=mask.shape
 for sy,sx in zip(*np.where(mask)):
  if visited[sy,sx]:continue
  stack=[(int(sy),int(sx))];visited[sy,sx]=True;xs=[];ys=[]
  while stack:
   y,x=stack.pop();xs.append(x);ys.append(y)
   for yy,xx in [(y-1,x),(y+1,x),(y,x-1),(y,x+1)]:
    if 0<=yy<H and 0<=xx<W and mask[yy,xx] and not visited[yy,xx]:visited[yy,xx]=True;stack.append((yy,xx))
  out.append([min(xs)+off[0],min(ys)+off[1],max(xs)-min(xs)+1,max(ys)-min(ys)+1,len(xs)])
 return out
def measure(path):
 im=np.asarray(Image.open(path).convert('RGB')).astype(np.int16);H,W=im.shape[:2]
 crop=im[30:85,10:235];g=crop.mean(2);cs=components((g>60)&(g<220)&(crop.max(2)-crop.min(2)<15),(10,30));search=next((x[:4] for x in cs if 130<x[2]<220 and 20<x[3]<45),None)
 crop=im[35:82,245:W-40];heading=bbox(crop.mean(2)>75,(245,35))
 # Heading text length depends on language; only its origin and height are compared.
 crop=im[82:590,250:W-35];bg=np.array([30,30,30]);body=bbox(np.max(np.abs(crop-bg),2)>45,(250,82))
 crop=im[590:H-10,600:W-10];blue=(crop[:,:,2]-crop[:,:,0]>30)&(crop[:,:,2]-crop[:,:,1]>30);cc=components(blue,(600,590));primary=next((x[:4] for x in cc if x[2]>60 and x[3]>20),None)
 gray=(crop.mean(2)>=45)&(crop.mean(2)<=110)&(crop.max(2)-crop.min(2)<10);cc=components(gray,(600,590));secondary=next((x[:4] for x in cc if 60<x[2]<140 and 20<x[3]<45),None)
 return {'frame':[W,H],'search':search,'heading':heading,'body':body,'primary':primary,'secondary':secondary,'background':im[300,W-8].tolist(),'primaryFill':im[primary[1]+3,primary[0]+3].tolist() if primary else None}
def close(a,b,tol):return a is not None and b is not None and all(abs(x-y)<=t for x,y,t in zip(a,b,tol))
vp=read(O/'VA.plan.json')['cases'];vr={x['id']:x for x in read(O/'VA.results.json')['cases']};cp=read(C/'CA.plan.json')['cases'];cr={x['id']:x for x in read(C/'CA.results.json')['cases']};rows=[]
def count_controls(row):
 controls=(row.get('probe')or{}).get('controls') or []
 return max(0,sum(isinstance(c,dict) and c.get('type','').split('.')[-1] in ['CheckBox','RadioButton','ComboBox','TextBox','Edit'] for c in controls)-1)
notes={'Goto':'범위·Go 버튼 동작·이동 우선순위 그룹 누락','Highlighting':'강조 색상 선택·줄 표시 방식 그룹 누락','Display':'탐색 표시줄 상하 위치·Quick Info 하위 토글 누락','VA Hashtags':'체크박스·최소 길이 설정 대신 안내문','Editor':'Surround 하위 항목·키 반복·포인터 변환 하위 옵션 차이','Enhanced Listboxes':'멤버 표시·제안 공급자·완성 동작 그룹 차이','Coloring':'6색 중 4색만 제공·적용 대상 체크 그룹 누락','Underlining':'미인식 심볼·밑줄 색상 선택 그룹 누락','Project-Wide':'드롭다운·프로젝트 수명 설정 대신 간략 모드 선택','Per-File':'체커 선택·표시·동작 시점 그룹 누락','Code Generation':'생성 규칙 설정 대신 안내문','Debug Assistance':'원본 라디오 선택 대신 패턴 편집 필드','Source Links':'목록 높이·규칙 필드·활성화 구성 차이','Projects and Files':'파일·프로젝트 처리 토글 대신 안내문','Mouse':'클릭·휠 조합 다수 대신 Alt 클릭 하나','C/C++ Directories':'플랫폼·디렉터리 목록 편집 대신 안내문','Performance':'파싱 수명·우선순위·디렉터리 설정 구성 차이'}
for v in vp:
 c=next(x for x in cp if x.get('page')==v['page'] and (cr[x['id']].get('probe')or{}).get('pageSelected'));a=measure(O/'screenshots'/f"VA-{v['id']}-dialog.png");b=measure(C/f"CA-{c['id']}-dialog.png");va=count_controls(vr[v['id']]);ca=count_controls(cr[c['id']]);shell={'navigationHierarchy':False,'frameSize':close(a['frame'],b['frame'],[18,14]),'searchBox':close(a['search'],b['search'],[8,8,10,8]),'headingOrigin':close(a['heading'][:2] if a['heading'] else None,b['heading'][:2] if b['heading'] else None,[8,8]),'primaryButton':close(a['primary'],b['primary'],[10,10,10,8]),'secondaryButton':close(a['secondary'],b['secondary'],[10,10,10,8])};body={'firstContentOrigin':close(a['body'][:2] if a['body'] else None,b['body'][:2] if b['body'] else None,[12,12]),'contentExtent':close(a['body'][2:] if a['body'] else None,b['body'][2:] if b['body'] else None,[45,34]),'interactiveControlCount':va>0 and abs(va-ca)<=int(va*.1),'groupComposition':False};design={'backgroundColor':close(a['background'],b['background'],[8,8,8]),'primaryButtonColor':close(a['primaryFill'],b['primaryFill'],[8,8,8]),'controlComposition':False}
 row={'page':v['page'],'vaId':v['id'],'caId':c['id'],'va':a,'ca':b,'vaControls':va,'caControls':ca,'shellChecks':shell,'bodyChecks':body,'designChecks':design,'difference':notes[v['page']]};rows.append(row)
 for role,root,id in [('VA',O/'screenshots',v['id']),('CA',C,c['id'])]:
  src=root/f'{role}-{id}-dialog.png';im=Image.open(src).convert('RGB');draw=ImageDraw.Draw(im)
  for key,box in row[role.lower()].items():
   if key in ['search','heading','body','primary','secondary'] and box:
    x,y,w,h=box;draw.rectangle((x,y,x+w-1,y+h-1),outline={'body':'#ff7070','heading':'#6bff9b'}.get(key,'#6fc9ff'),width=2)
  im.save(D/f'{role}-{id}-annotated.png')
shellPass=sum(sum(r['shellChecks'].values()) for r in rows);bodyPass=sum(sum(r['bodyChecks'].values()) for r in rows);designPass=sum(sum(r['designChecks'].values()) for r in rows);summary={'pages':len(rows),'shellPassed':shellPass,'shellTotal':102,'bodyPassed':bodyPass,'bodyTotal':68,'layoutScore':round(50*shellPass/102+50*bodyPass/68,2),'designPassed':designPass,'designTotal':51,'designScore':round(100*designPass/51,2),'wholePageCompositionMatches':0,'rows':rows,'method':'Layout = 50% shell criterion pass rate + 50% body criterion pass rate; 17 pages; each family equal checks. Design = 3 criterion pass rate. Not whole-product similarity.'}
(D/'layout-measurements.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print({k:v for k,v in summary.items() if k!='rows'});print([(r['page'],r['va']['search'],r['ca']['search'],r['va']['primary'],r['ca']['primary'],r['vaControls'],r['caControls']) for r in rows])
