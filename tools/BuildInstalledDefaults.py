import json,pathlib,hashlib
r=pathlib.Path('C:/Projects/CodeAssist')
a=json.loads((r/'docs/validation/va-installed-help-catalog.json').read_text(encoding='utf-8'))
by={x['file']:x for x in a['records']}
items=[]
def add(group,name,value,scope):
 f='OptionsHelp/'+group+'/'+name+'.html'; original=by[f]
 items.append({'resource':f,'sha256':original['sha256'],'helpDocumentedDefault':value,'scope':scope,'registryBinding':'unverified','runtimeResetProfile':'unverified','sourceKind':'shipped 2630 help; not runtime or code default'})
add('Goto','CheckGotoProjectFilter','Project','GoTo destination scope; system symbols may temporarily expand scope')
add('Goto','SelectSymbolOnGoto',True,'Select symbol after GoTo')
add('Hashtags','MinLenOfHashtags',3,'Minimum hashtag length excluding leading hash')
add('ListBoxes','OverwriteTextListbox','replace-unless-right-text-is-known-symbol','C/C++ enhanced listbox commit; never/always alternatives')
add('PerfGeneral','CheckParseAllFilesOnOpen',True,'Project opening parse including forced includes; startup cost relevant')
add('PerfGeneral','CheckWatchFilesExternally',False,'Reparse headers modified externally during IDE session')
add('PerfGeneral','ComboThreadPriority','Normal','General parser threads; requires IDE restart')
add('PerfGeneral','RespectVsCodeExcludedFiles',False,'Initial parse honors .vscode/settings.json files.exclude only when enabled')
add('PerfGeneral','UserDataDir','LOCALAPPDATA/VS-specific-extension/Data','Symbol database location example is VS2012+; directory change requires restart')
add('ProjectsAndFiles','CheckEnumerateVslangRefs',True,'Full project-reference enumeration; managed/native solution performance distinction')
add('ProjectsAndFiles','CheckParseAllFilesEmptySolution',True,'Parse sibling files when no solution or empty solution')
add('ProjectsAndFiles','CheckParseAllFilesWithSolution',False,'Parse sibling files with a loaded solution')
add('ProjectsAndFiles','CheckParseFilesWithoutExtensions',False,'Extensionless project files treated as C/C++ headers when enabled')
add('UnrealEngine','CheckAllowSnippetsInUnrealMarkup',False,'Snippet shortcuts inside UE macros')
add('UnrealEngine','CheckColorInsideUnrealMarkup',False,'UE macro arguments extra coloring')
add('UnrealEngine','RadioIndexPluginsRef','referenced-or-enabled-by-default','UE plugin directory parsing and indexing')
out={'scope':'16 explicit defaults in shipped VA2630 help; factory-reset execution and setting binding remain unverified','defaults':items,'limitations':['Default word occurrence alone does not prove an option default.','No current registry value is promoted to factory default.','Inspection ParserStats constants do not define general-parser settings.','Not a complete list of all factory defaults.']}
(r/'docs/validation/installed-help-defaults.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
p=r/'tools/BuildParitySpec.py';s=p.read_text(encoding='utf-8')
needle="(r/'docs/integrated-spec.md').write_text"
insert="defaultsPath=r/'docs/validation/installed-help-defaults.json'\nif defaultsPath.exists():\n defaults=json.loads(defaultsPath.read_text(encoding='utf-8'))\n data['installedHelpDefaultEvidence']='installed-help-defaults.json'\n (r/'docs/validation/integrated-spec.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')\n lines+=['','## 설치 도움말에 명시된 기본값','','[16개 기본값 장부](validation/installed-help-defaults.json)에 원본 파일 SHA-256과 적용 범위를 기록했습니다. 도움말에 명시된 값이며 실제 설정 키와 초기 프로필 동작은 미확인입니다.','', '| 도움말 자원 | 문서 기본값 | 적용 범위 |','|---|---|---|']\n for item in defaults['defaults']:\n  lines.append('| '+item['resource']+' | '+str(item['helpDocumentedDefault'])+' | '+item['scope']+' |')\n lines+=['','일반 프로젝트 시작 파싱 기본 활성 및 managed 참조 열거 기본 활성은 성능 비교의 입력 조건입니다. CPU 100% 원인이나 CodeAssist 수정 효과를 입증하지 않습니다.']\n"
if insert not in s:s=s.replace(needle,insert+needle,1)
p.write_text(s,encoding='utf-8')
print('Explicit installed-help defaults:',len(items))
