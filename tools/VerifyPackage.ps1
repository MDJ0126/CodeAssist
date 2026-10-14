param([string]$Package = "$PSScriptRoot/../UEAssist.vsix", [string]$Output = "$PSScriptRoot/../docs/validation/package.json")
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$uePackagePath=(Resolve-Path -LiteralPath $Package).Path
$ueZip=[IO.Compression.ZipFile]::OpenRead($uePackagePath)
try {
    $ueReader=[IO.StreamReader]::new($ueZip.GetEntry('extension.vsixmanifest').Open())
    try { [xml]$ueManifest=$ueReader.ReadToEnd() } finally { $ueReader.Dispose() }
    $ueVersion=$ueManifest.PackageManifest.Metadata.Identity.Version
    foreach($ueAsset in $ueManifest.PackageManifest.Assets.Asset){if($null -eq $ueZip.GetEntry([string]$ueAsset.Path)){throw "Missing manifest asset: $($ueAsset.Path)"}}
    $uePkgdefEntry=$ueZip.GetEntry('UEAssist.Extension.pkgdef'); if($null -eq $uePkgdefEntry){throw 'Missing package registration'}
    $uePkgdefReader=[IO.StreamReader]::new($uePkgdefEntry.Open()); try{$uePkgdef=$uePkgdefReader.ReadToEnd()}finally{$uePkgdefReader.Dispose()}
    if($uePkgdef -notmatch ', CTMENU, 1'){throw 'Missing managed menu registration'}
    if($ueVersion -ne '0.0.49') { throw "Incorrect version: $ueVersion" }
    $ueFiles=foreach($ueName in @('UEAssist.Extension.dll','UEAssist.Core.dll','UEAssist.Indexing.dll','UEAssist.Worker.exe','UEAssist.Semantics.dll','Newtonsoft.Json.dll')) {
        $ueEntry=$ueZip.GetEntry($ueName)
        if($null -eq $ueEntry) {throw "Missing package entry: $ueName"}
        $ueStream=$ueEntry.Open(); $ueHash=[Security.Cryptography.SHA256]::Create()
        try { $ueDigest=[BitConverter]::ToString($ueHash.ComputeHash($ueStream)).Replace('-','') }
        finally {$ueStream.Dispose(); $ueHash.Dispose()}
        $ueProject=if($ueName -eq 'UEAssist.Worker.exe'){'UEAssist.Worker'}else{[IO.Path]::GetFileNameWithoutExtension($ueName)}
        $ueBuiltPath=if($ueName -eq 'Newtonsoft.Json.dll'){"$PSScriptRoot/../UEAssist.Extension/bin/Release/$ueName"}elseif($ueName -in @('UEAssist.Core.dll','UEAssist.Indexing.dll','UEAssist.Semantics.dll')){"$PSScriptRoot/../$ueProject/bin/Release/netstandard2.0/$ueName"}else{"$PSScriptRoot/../$ueProject/bin/Release/$ueName"}
        if((Get-FileHash -LiteralPath $ueBuiltPath -Algorithm SHA256).Hash -ne $ueDigest) {throw "Package contains stale binary: $ueName"}
        [pscustomobject]@{File=$ueName; Bytes=$ueEntry.Length; SHA256=$ueDigest; MatchesReleaseBuild=$true}
    }
    [xml]$ueCommands=Get-Content -LiteralPath "$PSScriptRoot/../UEAssist.Extension/UEAssistCommands.vsct" -Raw
    $ueBindings=@($ueCommands.CommandTable.KeyBindings.KeyBinding | ForEach-Object {
        [pscustomobject]@{Command=$_.id; Key=$_.key1; Modifiers=$_.mod1}
    })
    $ueExpected=@('GoToSymbolCommandId','FindReferencesCommandId','OpenFileCommandId','FindSymbolCommandId',
        'CorrespondingFileCommandId','ListMethodsCommandId','RelatedSymbolsCommandId','NavigateBackCommandId',
        'NavigateForwardCommandId','RenameCommandId','QuickActionsCommandId','CreateDeclarationCommandId','NextScopeCommandId','PreviousScopeCommandId','ResetZoomCommandId',
        'HashtagsCommandId','SelectExtendCommandId','SelectShrinkCommandId','SelectBlockExtendCommandId','SelectBlockShrinkCommandId','ClipboardCommandId','FindSelectedCommandId')
    foreach($ueCommand in $ueExpected) {if($ueBindings.Command -notcontains $ueCommand){throw "Missing keybinding: $ueCommand"}}
    $ueReport=[pscustomobject]@{Version=$ueVersion; PackageSHA256=(Get-FileHash -LiteralPath $uePackagePath -Algorithm SHA256).Hash;
        Files=$ueFiles; KeyBindings=$ueBindings; Scope='Static package and VSCT verification; actual IDE key routing is unverified.'}
    $ueReport | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Output -Encoding utf8
    Write-Output "Package $ueVersion verified: $($ueFiles.Count) matching binaries, $($ueBindings.Count) registered key bindings."
} finally { $ueZip.Dispose() }
