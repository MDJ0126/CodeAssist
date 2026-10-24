param(
 [Parameter(Mandatory=$true)][int]$TargetProcessId,
 [string]$Python='C:/Users/ehdwn/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',
 [string]$Output
)
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if(!$Output){$Output=Join-Path $repo ('docs/validation/fixed-108-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))}
Push-Location $repo
try {
 & $Python (Join-Path $PSScriptRoot 'Aggregate108.py') --self-test
 if($LASTEXITCODE){throw 'Evidence aggregation self-test failed'}
 $live=Join-Path $Output 'live'
 try { & (Join-Path $repo 'tools/ShortcutProbe/RunBehavior.ps1') -TargetProcessId $TargetProcessId -Output $live }
 catch { Write-Warning ('Live verification failed; retaining failed cases: '+$_.Exception.Message) }
 $evidence=Join-Path $live 'behavior.json'
 if(!(Test-Path -LiteralPath $evidence)){throw 'No live evidence. Existing prior run will not be substituted.'}
 & $Python (Join-Path $PSScriptRoot 'Aggregate108.py') --live-report $evidence --output $Output
 if($LASTEXITCODE){throw '108-row evidence aggregation failed'}
 Write-Output ('Report: '+(Join-Path $Output 'index.html'))
} finally {Pop-Location}
