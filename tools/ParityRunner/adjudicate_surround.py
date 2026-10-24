import sys,json
sys.path.insert(0,'tools/ParityRunner');from adjudicate_background import read,update,ROOT
files={}
for role in ['VA','CA']:
 a=read(role+'.surround-parentheses.input.json');b=read(role+'.surround-parentheses.undo.json');assert b['after']['text']=='int SurroundAudit103() { return 1 + 2; }\r\n';assert a['after']['text']=='int SurroundAudit103() { return (1 + 2); }\r\n'
 name=role+'.surround-parentheses.bundle.json';(ROOT/name).write_text(json.dumps(dict(input=a,undo=b),ensure_ascii=False,indent=2),encoding='utf-8');files[role]=name
va=read(files['VA']);ca=read(files['CA']);keys=['text','caret','selectionStart','selectionLength','selectedText'];checks={s+'StateEqual':all(va[s]['after'][k]==ca[s]['after'][k] for k in keys) for s in ['input','undo']};checks['selectedExpressionSurroundedBoth']=True
update(56,'representative-match','선택한 1 + 2에 괄호 문자를 입력하면 양쪽 모두 (1 + 2)로 감싸고 커서·선택 상태가 같습니다. Undo 후 원문과 원래 선택 영역도 일치합니다. 괄호 한 가지 표본이며 다른 감싸기·물리 입력 전체 인증은 아닙니다.',files,checks)
