param(
    [Parameter(Mandatory=$true)][string]$Targets,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$Binary='VA_X64.dll',
    [string]$Extension='C:/Users/ehdwn/AppData/Local/Microsoft/VisualStudio/17.0_ca39936f/Extensions/zgy5sun1.aim',
    [switch]$Reuse
)
$ErrorActionPreference='Stop'
$vaRepo=[IO.Path]::GetFullPath("$PSScriptRoot/../..")
$vaTools=Join-Path $vaRepo 'tools/obj/re-tools'
$vaGhidra=@(Get-ChildItem -LiteralPath "$vaTools/ghidra" -Directory)
$vaJdk=@(Get-ChildItem -LiteralPath "$vaTools/jdk" -Directory)
if($vaGhidra.Count -ne 1 -or $vaJdk.Count -ne 1){throw 'Expected exactly one extracted Ghidra and JDK'}
$vaOutput=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $vaOutput -Force | Out-Null
$vaProject=Join-Path $vaRepo 'tools/obj/ghidra-projects'
New-Item -ItemType Directory -Path $vaProject -Force | Out-Null
$vaArgs=@('-Xmx2g','-XX:ParallelGCThreads=2','-XX:CICompilerCount=2',
    '-Djava.system.class.loader=ghidra.GhidraClassLoader','-Dfile.encoding=UTF8',
    "-Dapplication.settingsdir=$vaRepo/tools/obj/ghidra-settings",
    "-Dapplication.cachedir=$vaRepo/tools/obj/ghidra-cache",
    "-Dapplication.tempdir=$vaRepo/tools/obj/ghidra-temp",
    '-cp',"$($vaGhidra[0].FullName)/Ghidra/Framework/Utility/lib/Utility.jar",'ghidra.Ghidra',
    'ghidra.app.util.headless.AnalyzeHeadless',$vaProject,'VA2630')
if($Reuse){$vaArgs+=@('-process',$Binary,'-noanalysis')}
else{$vaArgs+=@('-import',(Join-Path $Extension $Binary),'-max-cpu','2','-analysisTimeoutPerFile','900')}
$vaArgs+=@('-scriptPath',$PSScriptRoot,'-postScript','ExportVaFunctions.java',
    [IO.Path]::GetFullPath($Targets),$vaOutput,'-log',"$vaOutput/analysis.log",'-scriptlog',"$vaOutput/scripts.log")
& "$($vaJdk[0].FullName)/bin/java.exe" @vaArgs
if($LASTEXITCODE -ne 0){throw 'Ghidra analysis failed'}
