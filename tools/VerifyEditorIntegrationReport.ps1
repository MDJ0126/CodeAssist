param([string]$InputReport='C:/Users/ehdwn/AppData/Local/CodeAssist/editor-integration-checks.json', [string]$PackageReport="$PSScriptRoot/../docs/validation/package.json")
$ErrorActionPreference='Stop'
$package=Get-Content -LiteralPath  $PackageReport -Raw|ConvertFrom-Json
$installed=Get-Content -LiteralPath "$PSScriptRoot/../docs/validation/installed-$($package.Version).json" -Raw|ConvertFrom-Json
$raw=Get-Content -LiteralPath $InputReport -Raw
$report=$raw|ConvertFrom-Json
if($report.version -ne $package.Version){throw 'Stale integration test version'}
$dll=Join-Path $installed.Directory 'CodeAssist.Extension.dll'
$expected=($package.Files|Where-Object File -eq 'CodeAssist.Extension.dll').SHA256
if((Get-FileHash -LiteralPath $dll).Hash -ne $expected){throw 'Installed DLL changed'}
$assembly=[Reflection.Assembly]::LoadFile($dll)
if($assembly.ManifestModule.ModuleVersionId.ToString() -ne $report.mvid){throw 'Integration MVID mismatch'}
if($report.error -or $report.cleanupError){throw "Integration failed: $($report.error) $($report.cleanupError)"}
if($report.cases.Count -ne 14 -or @($report.cases|Select-Object -ExpandProperty name -Unique).Count -ne 14){throw 'Incomplete scenario inventory'}
if(@($report.cases|Where-Object {-not $_.passed}).Count){throw 'Integration scenario failed'}
$output="$PSScriptRoot/../docs/validation/editor-integration-$($report.version).json"
$raw|Set-Content -LiteralPath $output -Encoding utf8
Write-Output "Actual VS editor integration $($report.version): 14 passed, matching installed MVID; not VA parity."
