param([Parameter(Mandatory=$true)][int]$VaProcessId,[Parameter(Mandatory=$true)][int]$CaProcessId,[Parameter(Mandatory=$true)][string]$Plan,[string]$Output,[string]$Python='C:/Users/ehdwn/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',[switch]$SkipBuild)
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if(!$Output){$Output=Join-Path $repo ('docs/validation/bounded-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))}
if(!$SkipBuild){& (Join-Path $repo 'tools/ShortcutProbe/BuildBounded.ps1')}
Push-Location $repo
try {& $Python (Join-Path $PSScriptRoot 'bounded.py') --plan $Plan --va-pid $VaProcessId --ca-pid $CaProcessId --output $Output;if($LASTEXITCODE){throw 'Bounded runner failed; inspect the recorded item statuses'}}finally{Pop-Location}
