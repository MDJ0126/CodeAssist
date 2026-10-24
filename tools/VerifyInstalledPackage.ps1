param([string]$PackageReport="$PSScriptRoot/../docs/validation/package.json",[string]$Instance='17.0_ca39936f',[string]$Output='')
$ErrorActionPreference='Stop'
$report=Get-Content -LiteralPath $PackageReport -Raw|ConvertFrom-Json
if((Get-FileHash -LiteralPath "$PSScriptRoot/../installers/$($report.Version)/CodeAssist.vsix").Hash -ne $report.PackageSHA256){throw 'Package report is stale'}
$base=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) "Microsoft/VisualStudio/$Instance/Extensions"
$found=@(foreach($file in (Get-ChildItem -LiteralPath $base -Filter extension.vsixmanifest -Recurse)){
    [xml]$manifest=Get-Content -LiteralPath $file.FullName -Raw
    if($manifest.PackageManifest.Metadata.Identity.Id -eq 'CodeAssist.Extension.dd99a661-836a-478d-8de4-bbd6557ba6bb' -and $manifest.PackageManifest.Metadata.Identity.Version -eq $report.Version){$file}
})
if($found.Count -ne 1){throw "Expected one installed build; found $($found.Count)"}
$files=@(foreach($file in $report.Files){
    $path=Join-Path $found[0].DirectoryName $file.File
    $hash=(Get-FileHash -LiteralPath $path).Hash
    if($hash -ne $file.SHA256){throw "Installed binary mismatch: $($file.File)"}
    [pscustomobject]@{File=$file.File;SHA256=$hash}
})
$result=[pscustomobject]@{Version=$report.Version;Directory=$found[0].DirectoryName;PackageSHA256=$report.PackageSHA256;ManifestSHA256=(Get-FileHash -LiteralPath $found[0].FullName).Hash;Files=$files}
if(!$Output){$Output="$PSScriptRoot/../docs/validation/installed-$($report.Version).json"}
$result|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $Output -Encoding utf8
Write-Output "Installed $($report.Version): $($files.Count) binaries match the verified package."
