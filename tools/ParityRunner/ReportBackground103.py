from pathlib import Path
import json,hashlib,html,datetime
root=Path('docs/validation/background-103');features=json.loads((root/'features.json').read_text());data=json.loads((root/'adjudicated.json').read_text());assert len(features)==108 and len({x['id'] for x in features})==108;by={x['id']:x for x in features};counts={s:sum(x['state']==s for x in features) for s in ['representative-match','different','unverified']};assert counts==data['counts'];determinate=108-counts['unverified']
for row in data['newJudgments']:
 for ev in row['evidence'].values():assert hashlib.sha256((root/ev['file']).read_bytes()).hexdigest()==ev['sha256'],'Evidence changed: '+ev['file']
def summary(r):
 if r.get('after'):return {'observedEditorState':r['after'],'error':r.get('error')}
 if r.get('result'):
  x=r['result'];keys=['status','title','file','line','column','selectedText','quickState','quickContent','signatures','visibleHeadings','clicked','stillVisible'];v={k:x[k] for k in keys if k in x}
  if 'controls' in x:v['visibleControlCount']=len(x['controls'])
  return v
 if 'commands' in r:return {'registeredCommandCount':len(r['commands']),'commandNames':[x['name'] for x in r['commands']]}
 v={k:r[k] for k in ['status','file','text','error','markers','compiler','targetOutput','presentation','headers','items','rvaOutput','moduleRelativeError','inputs','targetFilters'] if k in r}
 if not v:
  v={k:summary(value) if isinstance(value,dict) else value[:8] if isinstance(value,list) else value for k,value in list(r.items())[:12]}
 return v
images={65:('render-evidence/VA-parameter-info-popup.png','render-evidence/CA-parameter-info-editor.png'),67:('render-evidence/VA-quick-info-popup.png','render-evidence/CA-quick-info-editor.png'),7:('render-evidence/VA-current-line.png','render-evidence/CA-current-line.png'),60:('render-evidence/VA-syntax-colors-editor.png','render-evidence/CA-syntax-colors-editor.png'),61:('render-evidence/VA-style-native-default.png','render-evidence/CA-style-after-enable.png'),64:('render-evidence/VA-references-enabled.png','render-evidence/CA-references-enabled.png'),68:('render-evidence/VA-style-native-default.png','render-evidence/CA-style-after-enable.png'),108:('options-pages/VA-Coloring.png','options-pages/CA-Coloring.png')}
images.update({98:('render-evidence/VA-file-preview.png','render-evidence/CA-file-preview-utility.png'),57:('render-evidence/VA-column-guide.png','render-evidence/CA-column-guide.png'),15:('render-evidence/VA-editor-context-menu.png','render-evidence/CA-editor-context-menu.png'),101:('render-evidence/VA-editor-context-menu.png','render-evidence/CA-editor-context-menu.png')})
images[74]=('render-evidence/VA-spelling-edited.png','render-evidence/CA-spelling-edited.png')
body=''
for row in sorted(data['newJudgments'],key=lambda x:x['id']):
 n=int(row['id'].split('F')[1]);body+='<article><h2>'+row['id']+' · '+html.escape(by[row['id']]['name'])+' <small>'+html.escape(row['state'])+'</small></h2><p>'+html.escape(row['reason'])+'</p>'
 if row.get('checkedAssertions'):body+='<pre>'+html.escape(json.dumps(row['checkedAssertions'],ensure_ascii=False,indent=2))+'</pre>'
 body+='<div class="pair">'
 for index,(role,ev) in enumerate(row['evidence'].items()):
  body+='<section><h3>'+role+'</h3><a href="'+ev['file']+'">실행 원문 JSON</a>'
  if n in images and (root/images[n][index]).exists():body+='<a href="'+images[n][index]+'"><img loading="lazy" src="'+images[n][index]+'"></a>'
  body+='<details><summary>관찰 상태</summary><pre>'+html.escape(json.dumps(summary(ev['data']),ensure_ascii=False,indent=2))+'</pre></details></section>'
 body+='</div></article>'
contracts=''
for p in sorted((root/'compiler-check').glob('*result.json')):
 contracts+='<li><a href="compiler-check/'+p.name+'">'+html.escape(p.stem)+'</a></li>'
text='<!doctype html><meta charset="utf-8"><title>화면 비점유 대조 검증</title><style>body{font:16px Segoe UI,sans-serif;background:#181a20;color:#e5e7eb;margin:32px}h1,h2{color:#ffd346}small{font-size:13px;color:#aab}.pair{display:grid;grid-template-columns:1fr 1fr;gap:18px}article{background:#232631;padding:20px;margin:18px 0}pre{white-space:pre-wrap;overflow-wrap:anywhere;font:13px Consolas,monospace}a{color:#83c4ff}section{min-width:0}img{width:100%;height:auto;margin:12px 0}p{line-height:1.6}.notice{border-left:4px solid #ffd346;padding:12px}</style><h1>VA / CodeAssist 누적 대조 검증 — 현재 판정</h1><p>판정 확보 <b>'+str(determinate)+'/108 ('+f'{determinate/108*100:.2f}'+'%)</b>. 대표 출력 일치 '+str(counts['representative-match'])+'개 · 차이 '+str(counts['different'])+'개 · 미검증 '+str(counts['unverified'])+'개.</p><p class="notice">이 비율은 검증 범위이며 전체 제품 유사도가 아닙니다. 차이에는 커서·선택·서식 차이와 실제 컴파일/실행 오류가 함께 있으므로 항목별 근거를 구분해 읽어 주세요. 대표 시나리오만 검증했으며 기능 전체 인증으로 확대하지 않습니다. 초기 백그라운드 기록에 이후 승인된 물리 키·마우스·브라우저 실행 검증을 합친 누적 자료입니다. 이번 회차 전체 108개 신규 재실행 완료를 의미하지 않습니다.</p><p><a href="options-pages/index.html">전체 옵션 페이지 이미지 대조 (VA 30 / CA 32)</a> · <a href="features.json">108항목 상태</a> · <a href="adjudicated.json">판정·증거 해시</a> · <a href="isolation.json">입력 데스크톱 분리 검증</a></p><h2>생성 코드의 실제 컴파일·실행</h2><ul>'+contracts+'</ul><p>편집기 본문·WPF 팝업은 내부 렌더링으로 수집합니다. 렌더링에 나타나지 않은 네이티브 창이나 입력 미작동은 통과로 처리하지 않습니다. 관찰 상태는 수집 시점의 상태이며, 코드 변환 전 원문은 각 prepared/case 기록에 보관돼 있습니다.</p>'+body
(root/'index.html').write_text(text,encoding='utf-8');print(counts,determinate,'evidence hashes verified')
