param([string]$WorkerOutput="")
$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$sources=@('ShortcutProbe.cs','BehaviorScenarios.cs','KeyboardHookProbe.cs','PairedScenarios.cs','Remaining98.cs','SingleCase.cs','FileNavigationCase.cs','PhysicalStep106.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
$sources+=Join-Path $repo 'tools\ParityRunner\DialogProbe.cs'
if(!$WorkerOutput){$WorkerOutput=Join-Path $PSScriptRoot 'SingleCaseProbe.exe'}
& (Join-Path $framework 'csc.exe') /nologo /platform:x64 (('/out:'+$WorkerOutput)) (('/r:'+(Join-Path $PSScriptRoot 'EnvDTE.dll'))) (('/r:'+(Join-Path $framework 'WPF\UIAutomationClient.dll'))) (('/r:'+(Join-Path $framework 'WPF\UIAutomationTypes.dll'))) (('/r:'+(Join-Path $framework 'WPF\WindowsBase.dll'))) /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll @sources
if($LASTEXITCODE){throw 'Single-case worker build failed'}
& (Join-Path $framework 'csc.exe') /nologo (('/out:'+(Join-Path $PSScriptRoot 'RemainingCancel.exe'))) (Join-Path $PSScriptRoot 'RemainingCancel.cs')
if($LASTEXITCODE){throw 'Legacy closer disable build failed'}

foreach($name in @('Remaining98.exe','Remaining98Repair.exe')){
 & (Join-Path $framework 'csc.exe') /nologo (('/out:'+(Join-Path $PSScriptRoot $name))) (Join-Path $PSScriptRoot 'LegacyBatchDisabled.cs')
 if($LASTEXITCODE){throw 'Legacy runner disable build failed'}
}
