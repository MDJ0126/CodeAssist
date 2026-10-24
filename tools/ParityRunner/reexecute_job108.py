import ast,sys,json,shutil,time,traceback
from pathlib import Path
sys.path.insert(0,'tools/ParityRunner');import background_steps as bs,background_client as bc,adjudicate_background as ab
name=sys.argv[1];O=(Path('docs/validation/reexecution-108/jobs')/name).resolve();O.mkdir(parents=True,exist_ok=True);(O/'render-evidence').mkdir(exist_ok=True)
B=Path('docs/validation/background-103')
for f in B.glob('*.json'):
 if any(x in f.name for x in ['.input.','.case.','.pick.','.drive.','native-name','input-cases.json','cases.json']):shutil.copy2(f,O/f.name)
if name=='suite-address':
 f=Path('docs/validation/reexecution-108/jobs/suite-pdb/pdb-fixture.json');shutil.copy2(f,O/f.name) if f.exists() else None
bs.OUT=O
bs.BACKGROUND_ONLY=name not in ["remaining_clipboard108","remaining_shortcuts108"]
import compiler_contracts as cc
cc.OUT=O/"compiler-check";cc.OUT.mkdir(exist_ok=True)
ab.ROOT=O if hasattr(ab,"ROOT") else O
ab.update=lambda *args,**kwargs:(O/'adjudication-candidate.json').write_text(json.dumps({'args':args,'kwargs':kwargs},ensure_ascii=False,indent=2))
def native_hwnd():
 h=json.loads((bc.RUN/'hosts.json').read_text());f=O/'VA.current-list.snapshot.json';w=bs.launch(['native-list-snapshot',h['VA']['pid'],f]);w.wait(timeout=10);d=json.loads(f.read_text(encoding='utf-8-sig'));assert len(d)==1,d;return d[0]['hwnd']
def settle_known_dialogs():
 h=json.loads((bc.RUN/'hosts.json').read_text())
 for role in ['VA','CA']:
  for attempt in range(4):
   f=O/(role+'.guard-windows'+str(attempt)+'.json');w=bs.launch(['windows',h[role]['pid'],f]);w.wait(timeout=8);wins=json.loads(f.read_text(encoding='utf-8-sig'));titles={x['title'] for x in wins if x.get('visible') and x.get('enabled')};spec=None
   pickers=[x['title'] for x in wins if x.get('visible') and x.get('enabled') and x.get('cls')=='#32770' and x['title'] in ['CodeAssist: PDB Explorer','CodeAssist: Address Resolver']]
   if pickers:spec={'title':pickers[0],'button':'취소','requiredText':'취소'}
   elif '일관성 없는 줄 끝' in titles:spec={'title':'일관성 없는 줄 끝','button':'아니요(N)','requiredText':'다음 파일의 줄 끝에 일관성이 없습니다. 줄 끝을 정규화시키시겠습니까?'}
   elif '파일 수정 감지' in titles:
    messages=[x['title'] for x in wins if x.get('visible') and x.get('enabled') and x['title'].startswith("프로젝트 'ParityCSharp103'")]
    if len(messages)==1:spec={'title':'파일 수정 감지','button':'무시(I)','requiredText':messages[0]}
   elif 'Visual Assist' in titles:
    tree=O/(role+'.guard-warning'+str(attempt)+'.json');w=bs.launch(['ui-tree',h[role]['pid'],tree]);w.wait(timeout=8);items=json.loads(tree.read_text(encoding='utf-8-sig'));texts=[x['name'] for x in items if x['type']=='ControlType.Text' and ('Automatically highlight references feature was enabled' in x['name'] or 'Extend multi-line comments' in x['name'])]
    if len(texts)==1:spec={'title':'Visual Assist','button':'아니요(N)','requiredText':texts[0]}
   if not spec:break
   req=O/(role+'.guard-action'+str(attempt)+'.spec.json');req.write_text(json.dumps(spec));r=O/(role+'.guard-action'+str(attempt)+'.json');w=bs.launch(['drive',h[role]['pid'],r,req]);w.wait(timeout=15);assert json.loads(r.read_text()).get('acted')
original_module=bc.module
def guarded_module(role,operation,**kwargs):
 v=original_module(role,operation,**kwargs)
 if operation=='options-button' and kwargs.get('button')=='Save':settle_known_dialogs()
 return v
bc.module=guarded_module
class Transform(ast.NodeTransformer):
 def visit_Call(self,n):
  if any(k.arg=='hwnd' and isinstance(k.value,ast.Constant) and isinstance(k.value.value,int) for k in n.keywords):
   for k in n.keywords:
    if k.arg=='hwnd':k.value=ast.Call(func=ast.Name(id='native_hwnd',ctx=ast.Load()),args=[],keywords=[])
  return self.generic_visit(n)
 def visit_Constant(self,n):
  if isinstance(n.value,str) and name=="suite-tool-fix":n.value=n.value.replace("quickfix-compiled","quickfix-tool108")
  if isinstance(n.value,str) and name in ["suite-file-create","suite-file-rename","suite-file-new"]:
   for token in ["CreateFileAudit103","RenameFileAudit103","RenamedFileAudit103","selection-new-file-output103"]:n.value=n.value.replace(token,token.replace("103","108"))
  if n.value=="docs/validation/background-103":return ast.Constant(str(O.resolve()))
  return n
 def visit_Assign(self,n):
  if any(isinstance(t,ast.Name)and t.id=='OUT'for t in n.targets):n.value=ast.parse('Path('+repr(str(O))+')',mode='eval').body
  return self.generic_visit(n)
 def visit_FunctionDef(self,n):
  if n.name=='launch':return ast.parse('def launch(args):\n return __import__("background_steps").launch(args)').body[0]
  return self.generic_visit(n)
preps={
'background_change_signature':('change-signature.input.json','change-signature','VAssistX.RefactorChangeSignature'),
'background_add_member':('add-member.input.json','add-member','VAssistX.RefactorAddMember'),
'background_similar_member':('similar-member.input.json','similar-member','VAssistX.RefactorAddSimilarMember'),
'background_extract':('extract.input.json','extract','VAssistX.RefactorExtractMethod'),
'background_implement_virtual':('implement-virtual.input.json','implement-virtual','VAssistX.RefactorImplementInterface')}
if name in preps:
 file,label,command=preps[name];c=json.loads((O/file).read_text());bs.setup('VA',label,c['text'],c['start'],c.get('length',0));v,_=bc.module('VA','queue-command',windowTitle='',command=command,focusEditor=True);bs.save('VA.'+label+'.prep-command.json',v);time.sleep(.3)
if name=='background_encapsulate':
 text='class EncapAudit {\r\npublic:\r\n    int value;\r\n    int Read() { return value; }\r\n};\r\n';bs.setup('VA','encapsulate',text,text.index('value')+2);v,_=bc.module('VA','queue-command',windowTitle='',command='VAssistX.RefactorEncapsulateField',focusEditor=True);bs.save('VA.encapsulate.prep-command.json',v)

sequences={
'suite-create-all':['background_create_all'],
'suite-move-all':['background_move_all_start','background_move_all_finish'],
'suite-to-header':['crossfile_move'],
'suite-related':['background_related_prepare','background_related_menu','background_related_choose'],
'suite-view':['background_va_view_nav','background_ca_view_nav','background_ca_view_ready'],
'suite-platform':['background_add_custom_dir','background_platform_actual','background_platform_restore_adjudicate'],
'suite-external':['background_add_custom_dir','background_external_source_dir','background_external_lookup','background_external_finish'],
'suite-move-source':['background_move_restored_source'],
'suite-file-create':['background_create_file','background_create_file_commit'],
'suite-file-rename':['background_rename_file','background_rename_commit'],
'suite-file-new':['background_new_file','background_new_file_commit','background_new_file_retry'],
'suite-preview':['background_native_file_preview','background_ca_file_preview'],
'suite-editor-fix':['background_native_quickfix','background_ca_compiled_quickfix','background_ca_apply_quickfix'],
'suite-tool-fix':['background_native_tool_quickfix','background_ca_compiled_quickfix','background_ca_apply_quickfix','background_finish_quickfix_checks'],
'suite-debug':['launch_debuggee103','background_attach_debuggee','background_break_debuggee','background_open_memory103','background_memory_both_read','background_step_baseline','background_step_filtered'],
'suite-usage':['background_create_usage','background_create_usage_commit'],
'suite-recent':['background_recent_multiline','background_recent_accept'],
'suite-csharp':['background_csharp_using','background_csharp_typing','background_csharp_accept'],
'suite-pdb':['background_pdb_fixture','background_pdb_load','background_pdb_ca','background_pdb_filter'],
'suite-address':['background_address_start','background_address_load','background_address_ca_relative']}
start=time.time()
try:
 settle_known_dialogs()
 csharp_original=None
 if name=='suite-csharp':
  bc.module('VA','open-options',windowTitle='');v,_=bc.module('VA','options-read',windowTitle='Visual Assist Options',page='Suggestions');rows=v['result']['controls'];csharp_original=next(x['value'] for x in rows if x.get('name')=='E_nable in C#' and 'CheckBox' in x['type']);bs.save('VA.csharp-original.json',v);bc.module('VA','options-button',windowTitle='Visual Assist Options',button='Cancel')
 for phase in ['before']:
  for role in ['VA','CA']:
   v,_=bc.send(role,{});bs.save(role+'.'+phase+'.render.json',v)
 for script in sequences.get(name,[name]):
  if script=='background_native_tool_quickfix':
   text='int* ReadAudit103() { return 0; }\r\nint main(void) { return ReadAudit103() != 0; }\r\n';bs.setup('VA','inspection-nullptr',text,text.index('return 0')+7)
  if script=='background_external_source_dir':bc.module('VA','options-button',windowTitle='Visual Assist Options',button='Save')
  if script=='crossfile_move':sys.argv=[str(Path('tools/ParityRunner/crossfile_move.py')),'to-header']
  if script=='background_move_all_finish':
   h=json.loads((bc.RUN/'hosts.json').read_text());w=bs.launch(['ui-tree',h['VA']['pid'],O/'VA.move-all-source.native-dialog-tree.json','Move Implementations -- Visual Assist']);w.wait(timeout=10)
  if script=='background_create_all':
   c=json.loads((O/'create-all.input.json').read_text());(bc.RUN/'VA/create-all.cpp').write_bytes(c['source'].encode());bs.setup('VA','create-all',c['header'],c['start'],file='create-all.h',extras=['create-all.cpp']);v,_=bc.module('VA','queue-command',windowTitle='',command='VAssistX.RefactorCreateImplementation',focusEditor=True);bs.save('VA.create-all.started.json',v)
  if script=='background_pdb_fixture':
   v,_=bc.module('VA','queue-command',windowTitle='',command='VAssistX.PDBExplorer',focusEditor=True);bs.save('VA.pdb-open.json',v)
  path=Path('tools/ParityRunner')/(script+'.py');tree=Transform().visit(ast.parse(path.read_text(encoding='utf-8-sig')));ast.fix_missing_locations(tree);g={'__name__':'__main__','__file__':str(path.resolve()),'Path':Path,'native_hwnd':native_hwnd};exec(compile(tree,str(path),'exec'),g)
 for role in ['VA','CA']:
  v,_=bc.send(role,{});bs.save(role+'.after.render.json',v)
 status='completed';error=None
except BaseException as e:status='error';error=traceback.format_exc();print(error,flush=True)
if name=='suite-csharp' and 'csharp_original' in locals() and csharp_original is not None:
 bc.module('VA','open-options',windowTitle='');bc.module('VA','options-read',windowTitle='Visual Assist Options',page='Suggestions');bc.module('VA','options-toggle',windowTitle='Visual Assist Options',toggle='E_nable in C#',value=csharp_original);v,_=bc.module('VA','options-button',windowTitle='Visual Assist Options',button='Save');bs.save('VA.csharp-finally-restored.json',v)
(O/'execution.json').write_text(json.dumps({'script':name,'status':status,'error':error,'started':start,'ended':time.time(),'pidScopedHosts':json.loads((bc.RUN/'hosts.json').read_text())},ensure_ascii=False,indent=2));print('JOB',name,status,flush=True)
