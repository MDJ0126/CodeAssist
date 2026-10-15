$ErrorActionPreference='Stop'
$renderer='C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe'
$records=@(
    @('Settings','Dark','','options-dark-preview.png'),
    @('Settings','Light','','options-light-preview.png'),
    @('Settings','Dark','Editor / Coloring','options-coloring-dark.png'),
    @('Settings','Light','Editor / Coloring','options-coloring-light.png'),
    @('Settings','Dark','Code Inspection','options-inspection-dark.png'),
    @('Settings','Dark','Advanced / Performance','options-performance-dark.png'),
    @('Welcome','Dark','','welcome-dark-preview.png'),
    @('Welcome','Light','','welcome-preview.png'),
    @('FirstRun','Dark','','first-run-dark-preview.png')
)
$results=@()
foreach($record in $records) {
    $renderArgs=@('-STA','-NoProfile','-ExecutionPolicy','Bypass','-File',"$PSScriptRoot/RenderStartup.ps1",'-Screen',$record[0],'-Theme',$record[1],'-Output',"$PSScriptRoot/../docs/validation/$($record[3])")
    if($record[2]) {$renderArgs+=@('-OptionsPage',$record[2])}
    if($record[2] -eq 'Editor / Coloring') {$renderArgs+='-TestInteractions'}
    & $renderer @renderArgs
    if($LASTEXITCODE -ne 0) {throw "WPF test failed: $($record[3])"}
    $result=Get-Content -LiteralPath "$PSScriptRoot/../docs/validation/$($record[3]).json" -Raw | ConvertFrom-Json
    $results+=@{Screen=$record[0];Theme=$record[1];Page=$record[2];Image=$record[3];ContrastChecks=@($result.Controls).Count}
}
@{Views=$results;Passed=$results.Count;Scope='Offline owned WPF views and control interactions; not actual VS document/toolbar/margin host testing'} | ConvertTo-Json -Depth 8 | Set-Content "$PSScriptRoot/../docs/validation/product-view-checks.json" -Encoding utf8
Write-Output "Verified $($results.Count) product views."