param([string]$OutputDirectory='',[string]$Instance='17.0_ca39936fCodeAssistBackgroundCA')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$started=Get-Date
if(!$OutputDirectory){$OutputDirectory=Join-Path $root ('.local/read-only-gate-'+$started.ToString('yyyyMMdd-HHmmss'))}
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(!$out.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be inside the repository'}
New-Item -ItemType Directory -Path $out -Force|Out-Null
$checks=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action,[bool]$Optional=$false){
 try{$details=@(& $Action);$checks.Add([pscustomobject]@{name=$Name;status='pass';details=$details})}
 catch{$checks.Add([pscustomobject]@{name=$Name;status=$(if($Optional){'unconfirmed'}else{'fail'});details=@($_.Exception.Message)})}
}
Push-Location $root
try {
 [xml]$manifest=Get-Content CodeAssist.Extension/source.extension.vsixmanifest -Raw
 $version=[string]$manifest.PackageManifest.Metadata.Identity.Version
 Check 'Source manifest versions agree' {
  [xml]$other=Get-Content CodeAssist.Extension/source.2019.extension.vsixmanifest -Raw
  if($other.PackageManifest.Metadata.Identity.Version -ne $version){throw 'VS2019/Modern version mismatch'}
  $archived=@(Get-ChildItem -LiteralPath (Join-Path $root 'installers') -Directory | Where-Object { $_.Name -match '^\d+\.\d+\.\d+(?:\.\d+)?$' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'CodeAssist.vsix')) } | Sort-Object { [version]$_.Name } -Descending)
  if(!$archived.Count -or $archived[0].Name -ne $version){throw 'Source version is not the latest archived Modern VSIX'}
  "Version $version; latest archived Modern VSIX confirmed"
 }
 Check 'Modern package matches Release binaries' {& tools/VerifyPackage.ps1 -Package "installers/$version/CodeAssist.vsix" -Output "$out/package.json"}
 Check 'Installed CA binaries match package' {& tools/VerifyInstalledPackage.ps1 -PackageReport "$out/package.json" -Instance $Instance -Output "$out/installed.json"}
 Check 'Core tests, fresh TRX execution' {
  & dotnet test CodeAssist.Core.Tests/CodeAssist.Core.Tests.csproj --no-restore --verbosity quiet --logger 'trx;LogFileName=core.trx' --results-directory $out *> "$out/tests.log"
  if($LASTEXITCODE -ne 0){throw "dotnet test failed: $LASTEXITCODE"}
  [xml]$trx=Get-Content "$out/core.trx" -Raw
  $c=$trx.SelectSingleNode("//*[local-name()='Counters']")
  if(!$c -or [int]$c.total -le 0 -or [int]$c.executed -ne [int]$c.passed){throw 'Missing, failed or incomplete test execution'}
  "Passed $($c.passed)/$($c.total)"
 }
 Check 'Owned CA process loads matching Extension module' {
  $hosts=Get-Content .local/parity-runs/background-103/hosts.json -Raw|ConvertFrom-Json
  $process=Get-CimInstance Win32_Process -Filter "ProcessId=$($hosts.CA.pid)"
  if(!$process -or $process.Name -ne 'devenv.exe' -or $process.CommandLine -notmatch '/RootSuffix\s+CodeAssistBackgroundCA(?:\s|$)'){throw 'Owned CA process missing or identity mismatch'}
  $installed=Get-Content "$out/installed.json" -Raw|ConvertFrom-Json
  $expected=@($installed.Files|Where-Object File -eq 'CodeAssist.Extension.dll')
  if($expected.Count -ne 1){throw 'Installed Extension receipt missing or ambiguous'}
  $expectedPath=[IO.Path]::GetFullPath((Join-Path $installed.Directory 'CodeAssist.Extension.dll'))
  $live=Get-Process -Id $process.ProcessId
  $startTicks=$live.StartTime.ToUniversalTime().Ticks
  $probe=Join-Path $root 'tools/ManagedModuleProbe/bin/Release/net8.0-windows/ManagedModuleProbe.dll'
  if(!(Test-Path -LiteralPath $probe)){throw 'Build tools/ManagedModuleProbe in Release first; runtime load unconfirmed'}
  & dotnet $probe --owned-ca $process.ProcessId $startTicks $expectedPath "$out/managed-module.json" $root *> "$out/managed-module.log"
  if($LASTEXITCODE -ne 0){throw "CLR module probe failed: $(Get-Content "$out/managed-module.log" -Raw)"}
  $loaded=Get-Content "$out/managed-module.json" -Raw|ConvertFrom-Json
  if($loaded.pid -ne $process.ProcessId -or $loaded.startTicks -ne $startTicks -or $loaded.module -ne $expectedPath){throw 'CLR receipt identity or path mismatch'}
  if($loaded.sha256 -ne $expected[0].SHA256){throw 'CLR module backing-file hash differs from installed package'}
  "PID $($process.ProcessId): CLR module path and backing-file hash match; snapshot $($loaded.elapsedMilliseconds) ms"

 } $true
 $catalog=Get-Content tools/ParityRunner/acceptance-108.json -Raw|ConvertFrom-Json
 Check 'Fixed 108 catalog integrity' {
  if($catalog.denominator -ne 108 -or $catalog.features.Count -ne 108 -or @($catalog.features.id|Sort-Object -Unique).Count -ne 108){throw 'Catalog denominator/unique IDs invalid'}
  $diff=Compare-Object @($catalog.baselineIds|Sort-Object) @($catalog.features.id|Sort-Object)
  if($diff){throw 'Catalog IDs differ from baseline'}
  '108 unique baseline feature IDs accounted for; this is not functional certification'
 }
 $features=@($catalog.features|ForEach-Object {[pscustomobject]@{id=$_.id;name=$_.name;assertions=$_.assertions;gates=$_.fullFeatureGates;catalogAdapterReady=$_.liveAdapterReady;fullCertification='not_certified_by_this_run'}})
 $summary=[pscustomobject]@{startedAt=$started.ToString('o');finishedAt=(Get-Date).ToString('o');elapsedSeconds=((Get-Date)-$started).TotalSeconds;version=$version;machinePassed=@($checks|Where-Object status -eq pass).Count;machineTotal=$checks.Count;machineGateComplete=(@($checks|Where-Object status -ne pass).Count -eq 0);computerUsePerformed=$false;checks=$checks.ToArray();features=$features;featureCount=$features.Count;newPairedRuntimePasses=0;newFullFeatureCertifications=0;scope='Read-only process/package checks and automated Core tests. No new VA interaction, UI capture or physical-key comparison.'}
 $summary|ConvertTo-Json -Depth 14|Set-Content "$out/results.json" -Encoding utf8
 $rows=($checks|ForEach-Object {'<tr><td>'+[Net.WebUtility]::HtmlEncode($_.name)+'</td><td>'+[Net.WebUtility]::HtmlEncode($_.status)+'</td><td>'+[Net.WebUtility]::HtmlEncode(($_.details -join '; '))+'</td></tr>'}) -join "`n"
 $featureRows=($features|ForEach-Object {'<tr><td>'+[Net.WebUtility]::HtmlEncode($_.id)+'</td><td>'+[Net.WebUtility]::HtmlEncode($_.name)+'</td><td>'+[Net.WebUtility]::HtmlEncode(($_.assertions -join ' / '))+'</td><td>'+[Net.WebUtility]::HtmlEncode(($_.gates -join ' / '))+'</td></tr>'}) -join "`n"
 $html='<!doctype html><html lang="ko"><meta charset="utf-8"><title>화면 비점유 자동 검사</title><style>body{background:#191b22;color:#eee;font:14px/1.6 system-ui;max-width:1400px;margin:30px auto;padding:20px}table{border-collapse:collapse;width:100%}td,th{border:1px solid #666;padding:10px}a{color:#9ad1ff}h1,h2{color:#ffd44d}</style><h1>현재 버전 '+$version+' — 화면 비점유 검사</h1><p>화면·입력·VS 명령 실행 없이 패키지·설치·프로세스를 읽고 Core 테스트를 실행했습니다. 아래 통과는 VA 기능 일치가 아닙니다. 새 VA 실행 대조 0건, 이 검사로 추가한 전체 기능 인증 0건입니다.</p><table><tr><th>확인</th><th>결과</th><th>근거</th></tr>'+$rows+'</table><p><a href="results.json">원본 결과</a> · <a href="core.trx">테스트 TRX</a> · <a href="../refactor-116/index.html">최근 실제 VA 대조: 표본 4/4</a></p><h2>108개 전체 기준 — 미실행을 통과 처리하지 않음</h2><p>모든 행은 이번 검사로 전체 기능 인증하지 않았습니다. 문서 부분 명세 목록 108/108과 실행 검증은 별개입니다. UI 배치·테마·실제 키·VA와 동일 입력 비교는 해당 변경 시 실제 실행 근거가 필요합니다.</p><table><tr><th>ID</th><th>기능</th><th>개별 주장</th><th>전체 판정에 필요한 차원</th></tr>'+$featureRows+'</table></html>'
 $latestRelative=[IO.Path]::GetRelativePath($out,(Join-Path $root 'docs/validation/refactor-116/index.html')).Replace('\','/')
 $html=$html.Replace('../refactor-116/index.html',$latestRelative)
 $html|Set-Content "$out/index.html" -Encoding utf8
 $checks|Format-Table name,status -AutoSize
 Write-Output "Report: $out/index.html"
 if(@($checks|Where-Object status -eq fail).Count){exit 1}
 if(@($checks|Where-Object status -eq unconfirmed).Count){exit 2}
} finally {Pop-Location}