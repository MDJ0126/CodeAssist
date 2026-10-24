param([string]$OutputName="BackgroundVerifier.exe")
if([IO.Path]::GetFileName($OutputName) -ne $OutputName){throw "Output must be a file name"}
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$probe=Join-Path $repo 'tools\ShortcutProbe'
$references=@((Join-Path $probe 'EnvDTE.dll'),(Join-Path $probe 'Microsoft.VisualStudio.TextManager.Interop.dll'),(Join-Path $probe 'Microsoft.VisualStudio.Shell.Interop.dll'),(Join-Path $probe 'Microsoft.VisualStudio.OLE.Interop.dll'),'System.Drawing.dll','System.Web.Extensions.dll',(Join-Path $framework 'WPF\UIAutomationClient.dll'),(Join-Path $framework 'WPF\UIAutomationTypes.dll'),(Join-Path $framework 'WPF\WindowsBase.dll')) | ForEach-Object {'/r:'+$_}
& (Join-Path $framework 'csc.exe') /nologo /platform:x64 ('/out:'+(Join-Path $probe $OutputName)) @references (Join-Path $PSScriptRoot 'BackgroundWorker.cs') (Join-Path $PSScriptRoot 'NativeCommandNames.cs')
if($LASTEXITCODE){throw 'Background verifier build failed; no scenarios launched'}
