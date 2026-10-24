param([Parameter(Mandatory=$true)][string]$InstalledReport,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$installed=Get-Content -LiteralPath $InstalledReport -Raw|ConvertFrom-Json
$core=Join-Path $installed.Directory 'CodeAssist.Core.dll'
$expected=@($installed.Files|Where-Object File -eq 'CodeAssist.Core.dll')[0].SHA256
if((Get-FileHash -LiteralPath $core).Hash -ne $expected){throw 'Installed Core changed'}
$assembly=[Reflection.Assembly]::LoadFrom($core)
if($assembly.Location -ne $core){throw 'Assembly unification loaded another path'}
$prefsType=$assembly.GetType('CodeAssist.Core.StartupPreferences',$true)
$kindType=$assembly.GetType('CodeAssist.Core.SymbolKind',$true)
$resolve=$assembly.GetType('CodeAssist.Core.SymbolColorPalette',$true).GetMethod('Resolve')
$rows=@()
foreach($row in @(@('Type','#FFD700','#2B6C6E','TypeColor'),@('Function','#FF8000','#A31515','FunctionColor'),@('Variable','#BDB76B','#000000','VariableColor'),@('Macro','#BD63C5','#6F008A','MacroColor'))){
 foreach($light in @($false,$true)){
  $prefs=[Activator]::CreateInstance($prefsType);$kind=[Enum]::Parse($kindType,$row[0]);$actual=$resolve.Invoke($null,@($prefs,$kind,$light));$wanted=if($light){$row[2]}else{$row[1]}
  $rows+=@{case="$($row[0]) default light=$light";expected=$wanted;actual=$actual;passed=$actual -eq $wanted}
  $prefsType.GetProperty($row[3]).SetValue($prefs,'#123456');$actual=$resolve.Invoke($null,@($prefs,$kind,$light))
  $rows+=@{case="$($row[0]) custom light=$light";expected='#123456';actual=$actual;passed=$actual -eq '#123456'}
 }
}
foreach($name in @('Unknown','Header')){
 $actual=$resolve.Invoke($null,@([Activator]::CreateInstance($prefsType),[Enum]::Parse($kindType,$name),$false))
 $rows+=@{case="$name inherits IDE";expected=$null;actual=$actual;passed=$null -eq $actual}
}
$parse=$assembly.GetType('CodeAssist.Core.CppColorCategories',$true).GetMethod('Parse')
$tokens=$parse.Invoke($null,@('enum E { First = Pick(1, Bogus), Second = int{3}, Third }; Bogus();'))
$names=@($tokens|Where-Object {$_.Kind.ToString() -eq 'EnumMember'}|ForEach-Object Name)
$rows+=@{case='Nested enum initializer';expected='First,Second,Third';actual=($names -join ',');passed=($names -join ',') -eq 'First,Second,Third'}
$result=@{version=$installed.Version;scope='Installed Core execution only; no fresh VA/UI comparison';assembly=$assembly.Location;sha256=$expected;passed=@($rows|Where-Object passed).Count;total=$rows.Count;cases=$rows}
$result|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $Output
Write-Output "Installed palette regression: $($result.passed)/$($result.total)"
if($result.passed -ne $result.total){exit 1}
