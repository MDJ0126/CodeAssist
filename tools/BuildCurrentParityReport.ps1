param([string]$Output='docs/validation/parity-171')
$ErrorActionPreference='Stop'
$catalogPath='docs/validation/background-103/adjudicated.json'
$catalog=Get-Content -LiteralPath $catalogPath -Raw|ConvertFrom-Json
$rows=@($catalog.newJudgments|Sort-Object id)
$expected=@(1..108|ForEach-Object {'VA-F{0:000}' -f $_})
if($rows.Count-ne108-or(@($rows.id|Sort-Object -Unique).Count-ne108)-or(Compare-Object $expected @($rows.id))){throw 'Expected exactly the fixed108 unique feature IDs'}
$matches=@($rows|Where-Object state -eq 'representative-match').Count
$differences=@($rows|Where-Object state -eq 'different').Count
$unknown=108-$matches-$differences
if($unknown-lt0){throw 'Invalid catalog counts'}
$snippetChecks=@()
foreach($state in @('on','off')){
 $va=Get-Content "$Output/va-snippet-$state.json" -Raw|ConvertFrom-Json
 $ca=Get-Content "$Output/ca172-snippet-$state.json" -Raw|ConvertFrom-Json
 $snippetChecks+=@{state=$state;criterion='Full text including newline';passed=($va.editorText-ceq$ca.editorText)}
 $snippetChecks+=@{state=$state;criterion='Caret';passed=(($va.selection.activeLine-eq$ca.selection.activeLine)-and($va.selection.activeColumn-eq$ca.selection.activeColumn))}
}
$passed=@($snippetChecks|Where-Object passed).Count
$base=Get-Content docs/validation/options-166/summary.json -Raw|ConvertFrom-Json
if($base.selectedOptionSamples.passed-ne10-or$base.selectedOptionSamples.total-ne12){throw 'Review cumulative options before updating; prevents double counting'}
$selected=10+[int]($passed-eq4)
$names=@{};(Get-Content docs/validation/va-feature-baseline.json -Raw|ConvertFrom-Json).features|ForEach-Object {$names[$_.id]=$_.name}
$export=@($rows|ForEach-Object {[ordered]@{id=$_.id;name=$names[$_.id];historicalState=$_.state;reason=$_.reason;current172FullRetest='not performed'}})
$export|ConvertTo-Json -Depth 6|Set-Content "$Output/historical-108.json" -Encoding utf8
$summary=[ordered]@{startedAt='2026-10-24T14:12:43+09:00';recordedAt=[DateTimeOffset]::Now.ToString('o');proposed='20–30 minutes';version='0.0.172';historicalWholeCatalog=@{date=$catalog.updatedAt;matched=$matches;different=$differences;unverified=$unknown;total=108;percent=[Math]::Round(100*$matches/108,2);scope='Historical representative behavior, not current172 full parity';sourceSHA256=(Get-FileHash $catalogPath).Hash};currentWholeProductPercent=$null;currentFullCatalogRetested=0;selectedOptions=@{passed=$selected;total=12;percent=[Math]::Round(100*$selected/12,2);scope='Cumulative selected OnOff sample; not product-wide'};snippet=@{passed=$passed;total=4;percent=100*$passed/4;checks=$snippetChecks};coreTests=@{passed=458;total=458;percent=100};partialDocumentSpecification=@{count=108;total=108;percent=100};remaining=@('Physical held-key repeat rate: supported Computer Use API cannot hold a key','Ctrl/Shift+mouse multi-filter input: supported API cannot hold modifiers during click','Full108 certification on current172, including UI, keys and remaining differences');implementationCommit='8fdf90b';xerj='Used DoubleKeyRepeat search; confirmed current source with rg';completion='partial: physical input and full current catalog remain'}
$summary.elapsedSeconds=[Math]::Round(([DateTimeOffset]::Now-[DateTimeOffset]::Parse($summary.startedAt)).TotalSeconds)
$summary|ConvertTo-Json -Depth 9|Set-Content "$Output/summary.json" -Encoding utf8
$body=[Text.StringBuilder]::new()
[void]$body.Append('<!doctype html><html lang="ko"><meta charset="utf-8"><title>VA·CA 전체 집계와172 실제 검증</title><style>body{margin:28px;background:#181b22;color:#eee;font:16px/1.7 Segoe UI,sans-serif}h1,h2,strong{color:#ffdc54}a{color:#92caff}table{border-collapse:collapse}td,th{padding:8px;border:1px solid #596473}.grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0;background:#292e38;padding:10px}img{width:100%}</style><h1>전체 카탈로그 집계와 최신172 검증</h1>')
[void]$body.Append("<p><strong>마지막 전체108개 대표 동작 실측 일치율: $($summary.historicalWholeCatalog.percent)% ($matches/108개)</strong>. 차이 $differences/108개, 미판정 $unknown/108개. 기준 시각: $($catalog.updatedAt). 현재172의 제품 전체 동기화율로 주장하지 않습니다. 108개 고유 ID와 분모를 실제 JSON에서 검산했습니다.</p>")
[void]$body.Append("<p><strong>최신 선정 옵션 누적 검증: $($summary.selectedOptions.percent)% ($selected/12개)</strong>. 기존10개 표본에 이번 With Snippets 켜짐·꺼짐 검증1개를 더했습니다. 전체108개 동기화율과 분모가 다릅니다. 문서·도움말 부분 명세108/108개(100%)이며 전체 역분석 완료율이 아닙니다.</p>")
[void]$body.Append('<h2>이번 실제 실행</h2><p>사용자가 허용한 임시 스니펫 하나로 token171 선택 후 z를 물리 입력했습니다. VA·CA 모두 켜짐은 CHECK171(token171)와 CRLF, 꺼짐은 z가 됐고 본문과 커서4/4개 기준(100%)이 일치했습니다. 초기171은 한 줄 문서에서 LF를 사용해 실패했습니다.172에서 편집기 NewLineCharacter 설정을 사용하도록 수정하고 동일 입력으로 재검증했습니다. 사용자 VA 스니펫 파일은 원래 바이트 해시로 복원했고 CA 검사 스니펫도 원복했습니다.</p><div class="grid"><figure><figcaption>VA 켜짐</figcaption><img src="capture-1116.jpg"></figure><figure><figcaption>CA172 켜짐</figcaption><img src="capture-1150.jpg"></figure><figure><figcaption>VA 꺼짐</figcaption><img src="capture-1140.jpg"></figure><figure><figcaption>CA172 꺼짐</figcaption><img src="capture-1159.jpg"></figure></div><p><a href="https://www.wholetomato.com/documentation/code-snippets/va-snippets-for-surround-with">VA 공식 스니펫 문서</a>의 선택 영역·SurroundWith 문자 동작을 참고했습니다. 검사 스니펫에는 사용자 내용 대신 자체 CHECK171 문구만 사용했습니다.</p><h2>미검증과 실행 실패</h2><p>키 홀드 반복률과 Ctrl/Shift를 유지한 마우스 클릭은 현재 Computer Use API가 지원하지 않습니다. press_key는 한 번의 조합 입력이고 click에는 modifier 매개변수가 없습니다. 다른 입력 주입 프로그램으로 우회하지 않았으며 단위검사를 물리 대조로 계산하지 않았습니다. 이 둘은 아직 미검증입니다. 최신172 전체108개 재인증도 시행하지 않았습니다.</p><p>VA 옵션 저장 뒤 검사 프로세스가 종료돼 재시작했습니다. Windows 오류 이벤트를 로컬에 보관했으며 원인까지 단정하지 않습니다. 재시작 후 With Snippets 꺼짐 입력은 실제로 확인했고 원래 켜짐 값을 다시 저장했습니다. 사용자 스니펫과 검사 문서는 양쪽 원본 해시로 복원했습니다.</p><h2>빌드·설치·푸시</h2><p>전체 자동검사458/458개(100%), VS2019·Modern 빌드 성공, 검사 CA 설치 바이너리6개 일치, 단축키 등록48개 확인. 실제 실행한 CLR 모듈 경로·해시도172 설치본과 일치했습니다. 실제 UI 실행은 VS2022이며 VS2019 실행 인증은 아닙니다. 제품 소스161개 파일은8fdf90b로 GitHub main에 푸시했습니다. 사용자 스니펫·개인 파일·다운로드 바이너리는 커밋하지 않았습니다. 푸시 커밋만 새 폴더에 푼 재현 빌드에서 VS2019 Newtonsoft.Json 경로 의존성을 발견하여 Content 경로를 $(OutputPath) 기준으로 수정했습니다. 원래 실패 로그와 수정 후 빌드 로그를 구분해 보존했습니다.</p><p><a href="summary.json">집계 JSON</a> · <a href="historical-108.json">과거108개 판정</a> · <a href="tests172.log">자동검사</a> · <a href="installed172.json">설치</a> · <a href="loaded172.json">실제 CLR 모듈</a></p><h2>108개 과거 판정표</h2><p>아래는 과거 실행 기록입니다. 최신 버전으로 재시험했다고 표시하지 않습니다.</p><table><tr><th>ID</th><th>기능</th><th>과거 판정</th><th>근거 요약</th></tr>')
foreach($row in $export){[void]$body.Append('<tr><td>'+[Net.WebUtility]::HtmlEncode($row.id)+'</td><td>'+[Net.WebUtility]::HtmlEncode($row.name)+'</td><td>'+[Net.WebUtility]::HtmlEncode($row.historicalState)+'</td><td>'+[Net.WebUtility]::HtmlEncode($row.reason)+'</td></tr>')}
[void]$body.Append("</table><p>제안20~30분. 실제 작업 기록: $($summary.recordedAt), $($summary.elapsedSeconds)초. XERJ 사용, 누락은 rg로 보완. 작업 전체 완료로 표시하지 않습니다.</p></html>")
[IO.File]::WriteAllText((Join-Path (Get-Location) "$Output/index.html"),$body.ToString())
Write-Output ("Historical=$matches/108; selected=$selected/12; snippet=$passed/4; current whole=null")