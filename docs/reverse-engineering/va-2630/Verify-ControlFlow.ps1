# Tests the recovered branch model using stubs; does not execute Visual Assist.
$ErrorActionPreference = 'Stop'
function Invoke-RecoveredSelection([int]$Index, [int]$Count, [bool]$Matches) {
    if ($Index -ge $Count) { return 'FindItem' }
    if ($Index -lt 0) { return $Index }
    if ($Matches) { return $Index }
    return 'FindItem'
}
$vaCases = @(
    @{Index=3; Count=3; Matches=$true; Expected='FindItem'},
    @{Index=-1; Count=3; Matches=$false; Expected=-1},
    @{Index=1; Count=3; Matches=$true; Expected=1},
    @{Index=1; Count=3; Matches=$false; Expected='FindItem'}
)
foreach ($vaCase in $vaCases) {
    $vaActual = Invoke-RecoveredSelection -Index $vaCase.Index -Count $vaCase.Count -Matches $vaCase.Matches
    if ($vaActual -cne $vaCase.Expected) { throw "Branch model mismatch: $vaActual" }
}
Write-Output 'Recovered ValidateItemIndex branch model: 4/4 passed (stub verification only).'
