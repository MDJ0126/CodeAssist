param(
    [Parameter(Mandatory=$true)][string]$ExtensionFolder,
    [Parameter(Mandatory=$true)][string]$DumpbinPath,
    [string]$OutputFolder = "$PSScriptRoot/../../docs/reverse-engineering/va-2630"
)
$ErrorActionPreference = 'Stop'
$vaRoot = (Resolve-Path -LiteralPath $ExtensionFolder).Path
$vaOutput = [IO.Path]::GetFullPath($OutputFolder)
New-Item -ItemType Directory -Path $vaOutput -Force | Out-Null
dotnet run --project "$PSScriptRoot/VAStaticAnalysis.csproj" -- $vaRoot (Join-Path $vaOutput 'managed-metadata.json')
if ($LASTEXITCODE -ne 0) { throw 'Metadata inspection failed' }
foreach ($vaName in @('VA_X64.dll','VaAux64.dll','vaIPC64.dll','valibclang2_64.dll')) {
    $vaDump = & $DumpbinPath /nologo /headers /imports /exports (Join-Path $vaRoot $vaName)
    if ($LASTEXITCODE -ne 0) { throw "dumpbin failed: $vaName" }
    $vaDump | Set-Content -LiteralPath (Join-Path $vaOutput ($vaName + '.pe.txt')) -Encoding utf8
}
$vaInventory = Get-ChildItem -LiteralPath $vaRoot -File | Where-Object Extension -in '.dll','.exe' | ForEach-Object {
    $vaSignature = Get-AuthenticodeSignature -LiteralPath $_.FullName
    [pscustomobject]@{File=$_.Name; Version=$_.VersionInfo.FileVersion; Bytes=$_.Length;
        SignatureStatus=$vaSignature.Status.ToString(); Signer=$vaSignature.SignerCertificate.Subject}
}
$vaInventory | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $vaOutput 'inventory.json') -Encoding utf8
$vaRegistration = Get-ChildItem -LiteralPath $vaRoot -Filter '*.pkgdef' | ForEach-Object {
    [pscustomobject]@{File=$_.Name; Lines=@(Get-Content -LiteralPath $_.FullName |
        Where-Object {$_ -match 'Packages|AutoLoadPackages|CodeBase|InprocServer32|AllowsBackgroundLoad|IsAsyncQueryable|"Name"|"Class"'})}
}
$vaRegistration | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $vaOutput 'registration.json') -Encoding utf8
$vaAll = Get-Content -LiteralPath (Join-Path $vaOutput 'managed-metadata.json') -Raw | ConvertFrom-Json
$vaEvidence = foreach ($vaBinary in $vaAll) {
    foreach ($vaType in $vaBinary.FocusedTypes) {
        if ($vaType.Name -match 'VaCompletionUtils$|CompletionList`1$|Classification.MacroParser$|BufferListener$|ProjectItemInspector$|Server.Program$') {
            [pscustomobject]@{File=$vaBinary.File; SHA256=$vaBinary.SHA256; Type=$vaType.Name; Methods=$vaType.Methods}
        }
    }
}
$vaEvidence | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $vaOutput 'focused-evidence.json') -Encoding utf8
