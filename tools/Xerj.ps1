param(
    [ValidateSet('Start','Index','Watch','Search','Def','Mcp','Status')]
    [string]$Action = 'Status',
    [string]$Query
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$localRoot = Join-Path $projectRoot '.local\xerj'
$exe = Join-Path $localRoot 'xerj.exe'
$url = 'http://127.0.0.1:9320'
$env:XERJ_DISABLE_FEEDBACK = 'true'
if (!(Test-Path -LiteralPath $exe)) { throw "XERJ binary missing: $exe" }
function Test-Node {
    try { $node = Invoke-RestMethod "$url/" -TimeoutSec 2; return $node.cluster_name -eq 'xerj' }
    catch { return $false }
}
function Start-Node {
    if (Test-Node) { return }
    $process = Start-Process -FilePath $exe -ArgumentList @('--insecure','--bind','127.0.0.1','--port','9320','--embed-mode','lexical','--data-dir',('"' + (Join-Path $localRoot 'data') + '"')) -WorkingDirectory $projectRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $localRoot 'server.stdout.log') -RedirectStandardError (Join-Path $localRoot 'server.stderr.log') -PassThru
    $process.Id | Set-Content (Join-Path $localRoot 'server.pid')
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if (Test-Node) { return }
        Start-Sleep -Milliseconds 500
    }
    throw "XERJ startup failed; see $localRoot\server.stderr.log"
}
$stateRoot = Join-Path $env:LOCALAPPDATA 'CodeAssist\Xerj\state'
$indexArgs = @('autoindex',$projectRoot,'--url',$url,'--prefix','codeassist','--state-dir',$stateRoot,'--no-graph','--code-analyzer','code','--workers','4','--json')
function Start-Watcher {
    $pidFile = Join-Path $localRoot 'watch.pid'
    if (Test-Path $pidFile) {
        $watchProcess = Get-Process -Id ([int](Get-Content $pidFile)) -ErrorAction SilentlyContinue
        if ($watchProcess -and $watchProcess.Path -eq $exe) { return }
    }
    $quotedArgs = $indexArgs | ForEach-Object { '"' + $_ + '"' }
    $watchProcess = Start-Process -FilePath $exe -ArgumentList ($quotedArgs + '--watch') -WorkingDirectory $projectRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $localRoot 'watch.stdout.log') -RedirectStandardError (Join-Path $localRoot 'watch.stderr.log') -PassThru
    $watchProcess.Id | Set-Content $pidFile
}
switch ($Action) {
    'Start' { Start-Node }
    'Index' { Start-Node; & $exe @indexArgs; exit $LASTEXITCODE }
    'Watch' { Start-Node; & $exe @indexArgs --watch; exit $LASTEXITCODE }
    'Search' { Start-Node; & $exe search --url $url --prefix codeassist $Query; exit $LASTEXITCODE }
    'Def' { Start-Node; & $exe def --url $url --prefix codeassist $Query; exit $LASTEXITCODE }
    'Mcp' { Start-Node; Start-Watcher; & $exe mcp --url $url; exit $LASTEXITCODE }
    'Status' { if (!(Test-Node)) { throw 'XERJ node is not running' }; Invoke-RestMethod "$url/_cat/indices?format=json" | Select-Object index,'docs.count' }
}
