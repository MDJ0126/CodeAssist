param([Parameter(Mandatory=$true)][int]$TargetProcessId,[string]$Output)
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if(!$Output){$Output=Join-Path $repo ('docs/validation/behavior-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))}
Push-Location $repo
try {
 $framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
 & (Join-Path $framework 'csc.exe') /nologo /platform:x64 (('/out:'+(Join-Path $PSScriptRoot 'ShortcutProbe.exe'))) (('/r:'+(Join-Path $PSScriptRoot 'EnvDTE.dll'))) (('/r:'+(Join-Path $framework 'WPF/UIAutomationClient.dll'))) (('/r:'+(Join-Path $framework 'WPF/UIAutomationTypes.dll'))) (("/r:"+(Join-Path $framework "WPF/WindowsBase.dll"))) /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'ShortcutProbe.cs') (Join-Path $PSScriptRoot 'BehaviorScenarios.cs') (Join-Path $PSScriptRoot 'KeyboardHookProbe.cs') (Join-Path $PSScriptRoot 'PairedScenarios.cs') (Join-Path $PSScriptRoot 'Remaining98.cs') (Join-Path $PSScriptRoot 'SingleCase.cs') (Join-Path $PSScriptRoot 'FileNavigationCase.cs') (Join-Path $repo 'tools/ParityRunner/DialogProbe.cs')
 if($LASTEXITCODE){throw 'Diagnostic compilation failed'}
 & (Join-Path $PSScriptRoot 'ShortcutProbe.exe') self-test
 if($LASTEXITCODE){throw 'Comparator self-test failed'}
 & (Join-Path $PSScriptRoot 'ShortcutProbe.exe') $TargetProcessId CA $Output (Join-Path $PSScriptRoot 'behavior-plan.json') behavior
 if($LASTEXITCODE){throw "Host inspection failed. Evidence: $Output"}
 $report=Get-Content (Join-Path $Output 'behavior.json') -Raw | ConvertFrom-Json
 $failed=@($report.cases | Where-Object {-not $_.passed})
 Write-Output ('Passed '+(@($report.cases).Count-$failed.Count)+'/'+@($report.cases).Count)
 if($failed.Count){throw ('Actual output mismatch: '+($failed.id -join ', '))}
} finally {Pop-Location}
