param([string]$Package = "$PSScriptRoot/../CodeAssist.vsix", [string]$Output = "$PSScriptRoot/../docs/validation/package.json")
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$codeAssistPackagePath=(Resolve-Path -LiteralPath $Package).Path
$codeAssistZip=[IO.Compression.ZipFile]::OpenRead($codeAssistPackagePath)
try {
    $codeAssistReader=[IO.StreamReader]::new($codeAssistZip.GetEntry('extension.vsixmanifest').Open())
    try { [xml]$codeAssistManifest=$codeAssistReader.ReadToEnd() } finally { $codeAssistReader.Dispose() }
    $codeAssistVersion=$codeAssistManifest.PackageManifest.Metadata.Identity.Version
    foreach($codeAssistAsset in $codeAssistManifest.PackageManifest.Assets.Asset){if($null -eq $codeAssistZip.GetEntry([string]$codeAssistAsset.Path)){throw "Missing manifest asset: $($codeAssistAsset.Path)"}}
    $codeAssistPkgdefEntry=$codeAssistZip.GetEntry('CodeAssist.Extension.pkgdef'); if($null -eq $codeAssistPkgdefEntry){throw 'Missing package registration'}
    $codeAssistPkgdefReader=[IO.StreamReader]::new($codeAssistPkgdefEntry.Open()); try{$codeAssistPkgdef=$codeAssistPkgdefReader.ReadToEnd()}finally{$codeAssistPkgdefReader.Dispose()}
    if($codeAssistPkgdef -notmatch ', CTMENU, 1'){throw 'Missing managed menu registration'}
    if($codeAssistVersion -ne '0.0.49') { throw "Incorrect version: $codeAssistVersion" }
    $codeAssistFiles=foreach($codeAssistName in @('CodeAssist.Extension.dll','CodeAssist.Core.dll','CodeAssist.Indexing.dll','CodeAssist.Worker.exe','CodeAssist.Semantics.dll','Newtonsoft.Json.dll')) {
        $codeAssistEntry=$codeAssistZip.GetEntry($codeAssistName)
        if($null -eq $codeAssistEntry) {throw "Missing package entry: $codeAssistName"}
        $codeAssistStream=$codeAssistEntry.Open(); $codeAssistHash=[Security.Cryptography.SHA256]::Create()
        try { $codeAssistDigest=[BitConverter]::ToString($codeAssistHash.ComputeHash($codeAssistStream)).Replace('-','') }
        finally {$codeAssistStream.Dispose(); $codeAssistHash.Dispose()}
        $codeAssistProject=if($codeAssistName -eq 'CodeAssist.Worker.exe'){'CodeAssist.Worker'}else{[IO.Path]::GetFileNameWithoutExtension($codeAssistName)}
        $codeAssistBuiltPath=if($codeAssistName -eq 'Newtonsoft.Json.dll'){"$PSScriptRoot/../CodeAssist.Extension/bin/Release/$codeAssistName"}elseif($codeAssistName -in @('CodeAssist.Core.dll','CodeAssist.Indexing.dll','CodeAssist.Semantics.dll')){"$PSScriptRoot/../$codeAssistProject/bin/Release/netstandard2.0/$codeAssistName"}else{"$PSScriptRoot/../$codeAssistProject/bin/Release/$codeAssistName"}
        if((Get-FileHash -LiteralPath $codeAssistBuiltPath -Algorithm SHA256).Hash -ne $codeAssistDigest) {throw "Package contains stale binary: $codeAssistName"}
        [pscustomobject]@{File=$codeAssistName; Bytes=$codeAssistEntry.Length; SHA256=$codeAssistDigest; MatchesReleaseBuild=$true}
    }
    [xml]$codeAssistCommands=Get-Content -LiteralPath "$PSScriptRoot/../CodeAssist.Extension/CodeAssistCommands.vsct" -Raw
    $codeAssistBindings=@($codeAssistCommands.CommandTable.KeyBindings.KeyBinding | ForEach-Object {
        [pscustomobject]@{Command=$_.id; Key=$_.key1; Modifiers=$_.mod1}
    })
    $codeAssistExpected=@('GoToSymbolCommandId','FindReferencesCommandId','OpenFileCommandId','FindSymbolCommandId',
        'CorrespondingFileCommandId','ListMethodsCommandId','RelatedSymbolsCommandId','NavigateBackCommandId',
        'NavigateForwardCommandId','RenameCommandId','QuickActionsCommandId','CreateDeclarationCommandId','NextScopeCommandId','PreviousScopeCommandId','ResetZoomCommandId',
        'HashtagsCommandId','SelectExtendCommandId','SelectShrinkCommandId','SelectBlockExtendCommandId','SelectBlockShrinkCommandId','ClipboardCommandId','FindSelectedCommandId')
    foreach($codeAssistCommand in $codeAssistExpected) {if($codeAssistBindings.Command -notcontains $codeAssistCommand){throw "Missing keybinding: $codeAssistCommand"}}
    $codeAssistReport=[pscustomobject]@{Version=$codeAssistVersion; PackageSHA256=(Get-FileHash -LiteralPath $codeAssistPackagePath -Algorithm SHA256).Hash;
        Files=$codeAssistFiles; KeyBindings=$codeAssistBindings; Scope='Static package and VSCT verification; actual IDE key routing is unverified.'}
    $codeAssistReport | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Output -Encoding utf8
    Write-Output "Package $codeAssistVersion verified: $($codeAssistFiles.Count) matching binaries, $($codeAssistBindings.Count) registered key bindings."
} finally { $codeAssistZip.Dispose() }
