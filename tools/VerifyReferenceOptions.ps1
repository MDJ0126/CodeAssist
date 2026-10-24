param([string]$OutputDirectory="docs/validation/options-174",[string]$AssemblyDirectory="CodeAssist.Extension/bin/Release")
$ErrorActionPreference='Stop'
$bootstrap=Get-Content tools/RenderStartup.ps1 -Raw
$start=$bootstrap.IndexOf("`$ErrorActionPreference =")
$stop=$bootstrap.IndexOf("`$type =")
Invoke-Expression $bootstrap.Substring($start,$stop-$start)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$renderer=$assembly.GetType('CodeAssist.Extension.ReferenceOptions',$true).GetMethod('Render',[Reflection.BindingFlags]'Static,NonPublic')
$core=[Reflection.Assembly]::LoadFrom((Join-Path ([PreviewAssemblyResolver]::Bin) 'CodeAssist.Core.dll'))
$preferencesType=$core.GetType('CodeAssist.Core.StartupPreferences',$true)
$pages=@('Goto','Highlighting','Underlining','Display','Hashtags','Enhanced Listboxes','Projects and Files','C/C++ Directories','Code Generation','Project-Wide','Per-File','Debug Assistance','Mouse','Performance','Startup','Logging and Recovery','System Info')
$rows=[Collections.Generic.List[object]]::new()
$invalid=[Collections.Generic.HashSet[string]]::new()
$validator=[Action[string,bool]]{param($key,$valid) if($valid){[void]$invalid.Remove($key)}else{[void]$invalid.Add($key)}}
function Nodes($node){$node;foreach($child in [Windows.LogicalTreeHelper]::GetChildren($node)){if($child -is [Windows.DependencyObject]){Nodes $child}}}
foreach($page in $pages){
 $draft=[Activator]::CreateInstance($preferencesType);$draft.UiLanguage='en'
 $panel=[Windows.Controls.StackPanel]::new()
 [void]$renderer.Invoke($null,[object[]]@($page,$panel,$draft,$validator))
 $nodes=@(Nodes $panel)
 $checks=@($nodes|Where-Object {$_ -is [Windows.Controls.CheckBox] -and $_.Tag -like 'Option.*'})
 foreach($check in $checks){$name=$check.Tag.Substring(7);$property=$preferencesType.GetProperty($name);$before=[bool]$property.GetValue($draft,$null);$check.IsChecked=!$before;$check.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));if([bool]$property.GetValue($draft,$null) -eq $before){throw "Unbound checkbox: $page/$name"}}
 $combos=@($nodes|Where-Object {$_ -is [Windows.Controls.ComboBox] -and $_.Tag -like 'Option.*'})
 foreach($combo in $combos){if($combo.Items.Count -gt 1){$combo.SelectedIndex=if($combo.SelectedIndex -eq 0){1}else{0};$name=$combo.Tag.Substring(7);if($preferencesType.GetProperty($name).GetValue($draft,$null).ToString() -ne $combo.SelectedItem.Tag){throw "Unbound combo: $page/$name"}}}
 $numbers=@($nodes|Where-Object {$_ -is [Windows.Controls.TextBox] -and $_.Tag -like 'Option.*' -and $preferencesType.GetProperty($_.Tag.Substring(7)).PropertyType -eq [int]})
 foreach($number in $numbers){$saved=$number.Text;$number.Text='invalid';if(!$invalid.Contains($number.Tag.Substring(7))){throw "No validation: $page"};$number.Text=$saved;if($invalid.Contains($number.Tag.Substring(7))){throw "Validation did not clear: $page"}}
 $rows.Add([pscustomobject]@{page=$page;constructed=$true;checkboxBindings=$checks.Count;comboBindings=$combos.Count;numericValidation=$numbers.Count;topLevelGroups=$panel.Children.Count})
}
$draft=[Activator]::CreateInstance($preferencesType);$draft.UiLanguage='en'
$panel=[Windows.Controls.StackPanel]::new()
$sourceRenderer=$assembly.GetType('CodeAssist.Extension.SourceLinkOptions',$true).GetMethod('Render',[Reflection.BindingFlags]'Static,NonPublic')
[void]$sourceRenderer.Invoke($null,[object[]]@($panel,$draft,$validator))
function SourceNode($id){@(Nodes $panel)|Where-Object {[Windows.Automation.AutomationProperties]::GetAutomationId($_) -eq ('SourceLinks.'+$id)}|Select-Object -First 1}
$presets=SourceNode 'Preset';$add=SourceNode 'Add'
for($i=0;$i -lt $presets.Items.Count;$i++){$presets.SelectedIndex=$i;$add.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));if($draft.SourceLinkRules.Count -ne ($i+1)){throw 'Source-link Add is not bound'}}
$duplicate=SourceNode 'Duplicate';$duplicate.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));if($draft.SourceLinkRules.Count -ne 7){throw 'Source-link Duplicate is not bound'}
$remove=SourceNode 'Remove';$remove.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));if($draft.SourceLinkRules.Count -ne 6){throw 'Source-link Remove is not bound'}
$list=SourceNode 'Rules';$list.SelectedIndex=1;$target=SourceNode 'Target';$target.Text='https://example.com/review/{id}';if($draft.SourceLinkRules[1].Target -ne $target.Text){throw 'Source-link target edits not bound'}
$target.Text='javascript:alert(1)';if(!$invalid.Contains('sourceLink')){throw 'Invalid link did not block Save'};$target.Text='https://example.com/{id}';if($invalid.Contains('sourceLink')){throw 'Valid link did not unblock Save'}
$rows.Add([pscustomobject]@{page='Source Links';constructed=$true;providerAdd=6;duplicate=$true;remove=$true;targetEditing=$true;invalidTargetGate=$true})
@{pages=$rows;passed=$rows.Count;total=($pages.Count+1);scope='Offline WPF actual event handlers; no window Show, no Computer Use, no IDE input or settings save'}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $OutputDirectory 'bindings.json') -Encoding utf8
Write-Output ('Offline option binding pages passed: '+$rows.Count+'/'+($pages.Count+1))
