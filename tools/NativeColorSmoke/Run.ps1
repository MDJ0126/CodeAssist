param([string]$OutputDirectory="$PSScriptRoot/../../.local/native-color-smoke")
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
$vc=Get-ChildItem 'C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC' -Directory|Sort-Object Name|Select-Object -Last 1 -ExpandProperty FullName
$sdk='C:/Program Files (x86)/Windows Kits/10';$version=Get-ChildItem "$sdk/Include" -Directory|Sort-Object Name|Select-Object -Last 1 -ExpandProperty Name
$env:PATH="$vc/bin/Hostx64/x64;"+$env:PATH
& "$vc/bin/Hostx64/x64/cl.exe" /nologo /LD /EHsc "/I$vc/include" "/I$sdk/Include/$version/ucrt" "/I$sdk/Include/$version/shared" "/I$sdk/Include/$version/um" "$PSScriptRoot/probe.cpp" "/Fo$OutputDirectory/probe.obj" /link "/OUT:$OutputDirectory/NativeColorProbe.dll" "/IMPLIB:$OutputDirectory/NativeColorProbe.lib" "/LIBPATH:$vc/lib/x64" "/LIBPATH:$sdk/Lib/$version/ucrt/x64" "/LIBPATH:$sdk/Lib/$version/um/x64" user32.lib gdi32.lib
if($LASTEXITCODE){throw 'Native fixture build failed'}
$fx='C:\Windows\Microsoft.NET\Framework64\v4.0.30319';$wpf="$fx\WPF"
& "C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csc.exe" /nologo /platform:x64 "/out:$OutputDirectory/Smoke.exe" /r:System.Core.dll "/r:$wpf\PresentationCore.dll" "/r:$wpf\WindowsBase.dll" ([IO.Path]::GetFullPath("$PSScriptRoot/Smoke.cs")) ([IO.Path]::GetFullPath("$PSScriptRoot/../../CodeAssist.Extension/LiteTreeColors.cs")) ([IO.Path]::GetFullPath("$PSScriptRoot/../../CodeAssist.Core/SymbolDisplayTokens.cs")) ([IO.Path]::GetFullPath("$PSScriptRoot/../../CodeAssist.Core/SourceSymbol.cs"))
if($LASTEXITCODE){throw 'Adapter fixture build failed'}
& "$OutputDirectory/Smoke.exe"
if($LASTEXITCODE){throw 'Adapter integration failed'}
