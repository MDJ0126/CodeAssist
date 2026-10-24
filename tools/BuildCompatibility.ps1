param([string]$MSBuild='C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe')
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 foreach($target in @('VS2019','Modern')) {
  & $MSBuild CodeAssist.Extension/CodeAssist.Extension.csproj /restore /t:Rebuild /p:Configuration=Release /p:DeployExtension=false "/p:CodeAssistTarget=$target" /v:minimal /nologo
  if($LASTEXITCODE -ne 0){throw "Build failed: $target"}
 }
 Write-Output 'Built installers/<manifest-version>/CodeAssist.VS2019.vsix and CodeAssist.vsix'
} finally {Pop-Location}
