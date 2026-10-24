param(
    [string]$Python = 'python',
    [string]$VSPath = 'C:\Program Files\Microsoft Visual Studio\2022\Community',
    [switch]$Install,
    [switch]$Launch,
    [switch]$Visible,
    [switch]$Screenshots,
    [ValidateSet("VA","CA")][string[]]$Roles=@("VA","CA"),
    [string]$ResumeRoot,
    [int]$TimeoutSeconds = 90
)
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$vsix=Join-Path $PSScriptRoot 'bin\Release\CodeAssist.ParityRunner.vsix'
if ($Install) {
    if (Get-Process devenv -ErrorAction SilentlyContinue) { throw 'Close Visual Studio after saving your documents before installing the test-only VSIX. No process will be terminated.' }
    foreach($suffix in @('', 'Exp')) {
        $installArgs=@('/quiet', ('"'+$vsix+'"'))
        if($suffix){$installArgs=@('/quiet','/rootSuffix:Exp',('"'+$vsix+'"'))}
        $installer=Start-Process (Join-Path $VSPath 'Common7\IDE\VSIXInstaller.exe') -ArgumentList $installArgs -WindowStyle Hidden -PassThru -Wait
        if($installer.ExitCode -ne 0){throw "VSIX installation failed for '$suffix': $($installer.ExitCode)"}
    }
}
if (!$Launch) { Write-Output 'Runner prepared. Use -Launch to explicitly start isolated fixture tests; -Install installs the test-only VSIX first.'; return }
$existing=@(Get-CimInstance Win32_Process -Filter "Name='devenv.exe'")
if($existing.Count) {
    if($Roles.Count -ne 1 -or $Roles[0] -ne 'CA' -or @($existing | Where-Object {$_.CommandLine -match '(?i)/RootSuffix\s+"?Exp'}).Count) {throw 'A requested profile is already open; refusing to reuse it.'}
    Write-Output 'Leaving the existing normal VS untouched; launching only the separate Exp test profile.'
}
if($ResumeRoot) {
    $run=(Resolve-Path -LiteralPath $ResumeRoot).Path
    $ownedBase=[IO.Path]::GetFullPath((Join-Path $repo '.local\parity-runs')).TrimEnd('\')+'\'
    if(!$run.StartsWith($ownedBase,[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path (Join-Path $run '.codeassist-parity-owned'))){throw 'Refusing unrelated resume directory'}
    foreach($role in $Roles) {
        $priorReport=Join-Path $run "$role.results.json"
        if(Test-Path $priorReport){Copy-Item -LiteralPath $priorReport -Destination (Join-Path $run ($role+'.before-resume-'+(Get-Date -Format 'HHmmssfff')+'.json'))}
    }
    $stopFile=Join-Path $run 'STOP'
    if(Test-Path $stopFile){Remove-Item -LiteralPath $stopFile}
} else {
    $run=Join-Path $repo ('.local\parity-runs\'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    & $Python (Join-Path $PSScriptRoot 'parity.py') prepare $run
    if($LASTEXITCODE -ne 0){throw 'Plan generation failed'}
}
$prior=@{}
foreach($key in @('CODEASSIST_PARITY_PLAN','CODEASSIST_PARITY_ROOT','CODEASSIST_PARITY_ROLE','CODEASSIST_PARITY_SCREENSHOTS')){$prior[$key]=[Environment]::GetEnvironmentVariable($key,'Process')}
try {
    foreach($role in $Roles) {
        $env:CODEASSIST_PARITY_ROOT=$run
        $env:CODEASSIST_PARITY_SCREENSHOTS=if($Screenshots){'1'}else{'0'}
        $env:CODEASSIST_PARITY_ROLE=$role
        $env:CODEASSIST_PARITY_PLAN=Join-Path $run "$role.plan.json"
        $arguments=@((Join-Path $run "$role\Parity.sln"))
        if($role -eq 'CA'){$arguments+=@('/RootSuffix','Exp')}
        $arguments+=@('/Log',(Join-Path $run "$role.activity.xml"))
        # Explicit bootstrap also works when VS defers package auto-loading.
        $bootstrap=Join-Path $run "$role\Bootstrap.cpp"
        if($bootstrap -notmatch '\s'){$arguments+=@('/Command',('File.OpenFile '+$bootstrap))}
        # devenv's own parser treats quoted /switches as file names.
        # Quote paths only when needed, never /RootSuffix, /Log or Exp.
        $quotedArguments=$arguments | ForEach-Object { if($_ -match '\s'){ '"'+$_.Replace('"','\"')+'"' }else{$_} }
        $windowStyle=if($Visible){'Normal'}else{'Hidden'}
        $process=Start-Process (Join-Path $VSPath 'Common7\IDE\devenv.exe') -ArgumentList $quotedArguments -WindowStyle $windowStyle -PassThru
        Set-Content -LiteralPath (Join-Path $run "$role.launch-pid.txt") $process.Id
    }
} finally { foreach($key in $prior.Keys){[Environment]::SetEnvironmentVariable($key,$prior[$key],'Process')} }
$deadline=(Get-Date).AddSeconds($TimeoutSeconds)
while((Get-Date) -lt $deadline) {
    $done=0
    foreach($role in $Roles) {
        $path=Join-Path $run "$role.results.json"
        if(Test-Path -LiteralPath $path){try {if((Get-Content -LiteralPath $path -Raw | ConvertFrom-Json).completed){$done++}} catch {}}
    }
    if($done -eq $Roles.Count){break}
    Start-Sleep -Milliseconds 250
}
if($done -ne $Roles.Count){Set-Content -LiteralPath (Join-Path $run 'STOP') 'Deadline reached. Pending/modal scenarios are not passes.'; Write-Warning 'Runner timed out or is awaiting a dialog. Inspect the two VS processes; no process was killed.'}
& $Python (Join-Path $PSScriptRoot 'parity.py') compare $run
if($LASTEXITCODE -ne 0){throw 'Comparison report failed'}
if($Screenshots) {
    & $Python (Join-Path $PSScriptRoot 'screenshots.py') $run
    if($LASTEXITCODE -ne 0){throw 'Screenshot validation/gallery generation failed'}
    Write-Output "Screenshots: $run\screenshots.html"
}
Write-Output "Report: $run\comparison.html"
