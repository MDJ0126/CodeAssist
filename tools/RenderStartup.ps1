param([string]$Output = "$PSScriptRoot/../docs/validation/welcome-preview.png", [ValidateSet("Welcome","FirstRun","Settings","Navigation","Help","References")][string]$Screen = "Welcome", [ValidateSet("Light","Dark")][string]$Theme = "Light", [string]$OptionsPage = "", [ValidateSet("auto","ko","en")][string]$Language="ko", [switch]$SeedSourceLinkExamples, [switch]$TestInteractions, [switch]$TestThemeSwitch, [string]$AssemblyDirectory = "$PSScriptRoot/../CodeAssist.Extension/bin/Release")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
Add-Type @"
using System;
using System.IO;
using System.Reflection;
public static class PreviewAssemblyResolver
{
    public static string Bin;
    public static string Ide;
    public static void Install() { AppDomain.CurrentDomain.AssemblyResolve += Resolve; }
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name + ".dll";
        foreach (var folder in new[] { Bin, Path.Combine(Ide,"PublicAssemblies"), Path.Combine(Ide,"PrivateAssemblies"), Ide })
        {
            var path = Path.Combine(folder, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }
}
"@
[PreviewAssemblyResolver]::Bin = (Resolve-Path -LiteralPath $AssemblyDirectory).Path
[PreviewAssemblyResolver]::Ide = 'C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE'
[PreviewAssemblyResolver]::Install()
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path ([PreviewAssemblyResolver]::Bin) 'CodeAssist.Extension.dll'))
$type = $assembly.GetType('CodeAssist.Extension.StartupUi', $true)
$preferences=$type.GetProperty('Preferences',[Reflection.BindingFlags]'Static,NonPublic').GetValue($null,$null)
$preferences.UiLanguage=$Language
if($SeedSourceLinkExamples){
 $linkType=[Reflection.Assembly]::LoadFrom((Join-Path ([PreviewAssemblyResolver]::Bin) 'CodeAssist.Core.dll')).GetType('CodeAssist.Core.SourceLinks',$true)
 $preferences.SourceLinkRules.Clear()
 foreach($name in @('URL','FogBugz','GitHub','Jira','File Viewer','External Tool')){$preferences.SourceLinkRules.Add($linkType.GetMethod('Preset').Invoke($null,@($name)))}
}

if ($Screen -eq 'References') {
    $view=[Activator]::CreateInstance($assembly.GetType('CodeAssist.Extension.ReferencePane',$true),[Reflection.BindingFlags]'Instance,Public,NonPublic',$null,@(),$null)
} elseif ($Screen -eq 'Help') {
    $helpType=$assembly.GetType('CodeAssist.Extension.HelpView',$true)
    $view=[Activator]::CreateInstance($helpType,[Reflection.BindingFlags]'Instance,Public,NonPublic',$null,@(),$null)
    $helpType.GetMethod('Navigate',[Reflection.BindingFlags]'Instance,NonPublic').Invoke($view,@($(if($OptionsPage){$OptionsPage}else{'shortcuts'}))) | Out-Null
} elseif ($Screen -eq 'Navigation') {
    $entryType=$assembly.GetType('CodeAssist.Extension.NavigationEntry',$true)
    $entries=[Array]::CreateInstance($entryType,3)
    $entries.SetValue([Activator]::CreateInstance($entryType,@('AFixtureActor::Select','C:/Fixture/FixtureActor.cpp',14,20,'int AFixtureActor::Select(int Value)','Function')),0)
    $entries.SetValue([Activator]::CreateInstance($entryType,@('AFixtureActor','C:/Fixture/FixtureActor.h',22,7,'class AFixtureActor : public ABaseActor','Type')),1)
    $entries.SetValue([Activator]::CreateInstance($entryType,@('Health','C:/Fixture/FixtureActor.h',16,11,'float Health','Variable')),2)
    $pickerType=$assembly.GetType('CodeAssist.Extension.NavigationPicker',$true)
    $picker=[Activator]::CreateInstance($pickerType,[Reflection.BindingFlags]'Instance,Public,NonPublic',$null,@('CodeAssist: Symbols',$entries,$null,'C:/Fixture',$true),$null)
    $view=$picker.Content;$picker.Content=$null
} elseif ($Screen -eq 'Welcome') {
    $method = $type.GetMethod('Welcome', [Reflection.BindingFlags]'Static,NonPublic')
    $view = $method.Invoke($null, @())
} else {
    $method = $type.GetMethod('CreateConfigurationWindow', [Reflection.BindingFlags]'Static,NonPublic')
    $window = $method.Invoke($null, @([bool]($Screen -eq 'FirstRun')))
    $view = $window.Content
    $window.Content = $null
}
$width = if ($Screen -eq "Welcome") { 1440 } elseif ($Screen -eq "References") { 435 } elseif ($Screen -eq "Navigation") { 886 } elseif ($Screen -eq "Help") { 890 } elseif ($Screen -eq "Settings") { 900 } else { 940 }
$height = if ($Screen -eq "Welcome") { 2100 } elseif ($Screen -eq "References") { 520 } elseif ($Screen -eq "Navigation") { 396 } elseif ($Screen -eq "Help") { 600 } elseif ($Screen -eq "Settings") { 680 } else { 720 }
$root = [Windows.Controls.Border]::new()
$foreground = if ($Theme -eq "Dark") { [Windows.Media.BrushConverter]::new().ConvertFromString("#FFF1F1F1") } else { [Windows.Media.Brushes]::Black }
$background = if ($Theme -eq "Dark") { [Windows.Media.BrushConverter]::new().ConvertFromString("#FF1E1E1E") } else { [Windows.Media.Brushes]::White }
$environmentColors = [Type]::GetType('Microsoft.VisualStudio.PlatformUI.EnvironmentColors, Microsoft.VisualStudio.Shell.15.0', $true)
$foregroundKey = $environmentColors.GetProperty('ToolWindowTextBrushKey').GetValue($null,$null)
$backgroundKey = $environmentColors.GetProperty('ToolWindowBackgroundBrushKey').GetValue($null,$null)
$root.Resources.Add($foregroundKey,$foreground)
$root.Resources.Add($backgroundKey,$background)
$root.Background = $background
$root.SetValue([Windows.Controls.Control]::ForegroundProperty, $foreground)
$root.Child = $view
$root.Width = $width
$root.Height = $height
$root.Measure([Windows.Size]::new($width,$height))
$root.Arrange([Windows.Rect]::new(0,0,$width,$height))
$root.UpdateLayout()
if($OptionsPage) {
    function Select-OptionsPage($node) {
        if($node -is [Windows.Controls.TreeViewItem] -and [string]$node.Tag -eq $OptionsPage) { $node.IsSelected=$true; return }
        for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$i++) { Select-OptionsPage ([Windows.Media.VisualTreeHelper]::GetChild($node,$i)) }
    }
    Select-OptionsPage $root
    $root.UpdateLayout()
}
if($SeedSourceLinkExamples){
 function Select-SourceFixture($node){if($node -is [Windows.Controls.DataGrid]){$node.SelectedIndex=2};for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$i++){Select-SourceFixture ([Windows.Media.VisualTreeHelper]::GetChild($node,$i))}}
 Select-SourceFixture $root
 $root.UpdateLayout()
}
if($TestInteractions) {
    $nodes=[Collections.Generic.List[object]]::new()
    function Gather-Nodes($node) {
        $nodes.Add($node)
        for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$i++) { Gather-Nodes ([Windows.Media.VisualTreeHelper]::GetChild($node,$i)) }
    }
    Gather-Nodes $root
    $picker=$nodes | Where-Object { $_ -is [Windows.Controls.Button] -and $_.Tag -eq '클래스와 타입' } | Select-Object -First 1
    $saveButton=$nodes | Where-Object { $_ -is [Windows.Controls.Button] -and $_.Content -eq '저장' } | Select-Object -First 1
    $searchBox=$nodes | Where-Object { $_ -is [Windows.Controls.TextBox] -and [Windows.Automation.AutomationProperties]::GetName($_) -eq '설정 검색' } | Select-Object -First 1
    $editorGroup=$nodes | Where-Object { $_ -is [Windows.Controls.TreeViewItem] -and $_.Tag -eq 'Editor' } | Select-Object -First 1
    $projectGroup=$nodes | Where-Object { $_ -is [Windows.Controls.TreeViewItem] -and $_.Tag -eq 'Projects' } | Select-Object -First 1
    if($null -eq $picker -or $null -eq $saveButton -or $null -eq $searchBox) { throw 'Interaction controls not found' }
    $preferences=$type.GetProperty('Preferences',[Reflection.BindingFlags]'Static,NonPublic').GetValue($null,$null)
    $originalPreference=$preferences.TypeColor
    if([string]$picker.ToolTip -notmatch '^#[0-9A-Fa-f]{6}$') { throw 'Color swatch is not bound to a valid color' }
    if($preferences.TypeColor -ne $originalPreference) { throw 'Creating color controls mutated live preferences' }
    $searchBox.Text='클립보드'
    $root.UpdateLayout()
    if($editorGroup.Visibility -ne 'Visible' -or $projectGroup.Visibility -ne 'Collapsed') { throw 'Setting keyword search did not filter categories' }
    $searchBox.Text=''
    $root.UpdateLayout()
    [pscustomobject]@{ColorSwatchBound=$true;DraftDoesNotMutateLivePreferences=$true;SettingKeywordSearchFilters=$true;Scope='Offline owned WPF controls; no actual IDE automation'} | ConvertTo-Json | Set-Content -LiteralPath ($Output+'.interactions.json') -Encoding utf8
}
$checks = [Collections.Generic.List[object]]::new()
function Confirm-ToggleForeground($node) {
    if (($node -is [Windows.Controls.CheckBox]) -or ($node -is [Windows.Controls.RadioButton]) -or ($node -is [Windows.Controls.ComboBox])) {
        $actual = $node.Foreground.Color.ToString()
        $backNode = [Windows.Media.VisualTreeHelper]::GetParent($node)
        $back = $background
        while($null -ne $backNode) {
            $candidate = if ($backNode -is [Windows.Controls.Panel] -or $backNode -is [Windows.Controls.Border] -or $backNode -is [Windows.Controls.Control]) { $backNode.Background } else { $null }
            if($candidate -is [Windows.Media.SolidColorBrush] -and $candidate.Color.A -eq 255) { $back=$candidate; break }
            $backNode=[Windows.Media.VisualTreeHelper]::GetParent($backNode)
        }
        function Luminance($color) {
            $channels=@($color.R,$color.G,$color.B) | ForEach-Object { $v=$_/255.0; if($v -le 0.04045){$v/12.92}else{[Math]::Pow(($v+0.055)/1.055,2.4)} }
            return 0.2126*$channels[0]+0.7152*$channels[1]+0.0722*$channels[2]
        }
        $frontL=Luminance $node.Foreground.Color
        $backL=Luminance $back.Color
        $contrast=([Math]::Max($frontL,$backL)+0.05)/([Math]::Min($frontL,$backL)+0.05)
        if($contrast -lt 4.5) { throw "Unreadable $Theme toggle: $($node.Content): $actual on $($back.Color), contrast $contrast" }
        $checks.Add([pscustomobject]@{Label=[string]$node.Content; Foreground=$actual; Background=$back.Color.ToString(); Contrast=$contrast})
    }
    for ($i=0; $i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) {
        Confirm-ToggleForeground ([Windows.Media.VisualTreeHelper]::GetChild($node,$i))
    }
}
Confirm-ToggleForeground $root
if($checks.Count -eq 0 -and $Screen -ne 'Help' -and $Screen -ne 'References' -and $OptionsPage -ne 'Advanced / System Info') { throw 'No toggle controls validated.' }
if($Screen -eq 'Help') {
    $helpTexts=[Collections.Generic.List[object]]::new()
    function Get-HelpTextNodes($node) {
        if($node -is [Windows.Controls.TextBlock]){$helpTexts.Add($node)}
        for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$i++){Get-HelpTextNodes ([Windows.Media.VisualTreeHelper]::GetChild($node,$i))}
    }
    Get-HelpTextNodes $root
    $keys=@($helpTexts|Where-Object {$_.FontFamily.Source -eq 'Consolas'})
    if($OptionsPage -eq 'shortcuts' -and $keys.Count -ne 15){throw "Expected 15 separate shortcut rows, found $($keys.Count)"}
    foreach($key in $keys){if($key.TextWrapping -ne 'NoWrap' -or $key.ActualWidth -lt $key.DesiredSize.Width-1){throw "Shortcut label clipped: $($key.Text)"}}
    @{Topic=$OptionsPage;Language=$Language;ShortcutRows=$keys.Count;KeyLabelsNotWrapped=$true;Scope='Offline assembled WPF HelpView; not live IDE'}|ConvertTo-Json|Set-Content ($Output+'.layout.json') -Encoding utf8
}
[pscustomobject]@{Screen=$Screen; Theme=$Theme; Controls=$checks; Scope='Offline WPF resource and label-color regression; not actual Visual Studio UI'} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath ($Output+'.json') -Encoding utf8
$bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($width,$height,96,96,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($root)
$encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream = [IO.File]::Create([IO.Path]::GetFullPath($Output))
try { $encoder.Save($stream) } finally { $stream.Dispose() }
Write-Output "Rendered CodeAssist WPF $Screen offline; this is not an IDE integration test: $Output"
if($TestThemeSwitch) {
    $themeNodes=[Collections.Generic.List[object]]::new()
    function Get-ThemeNodes($node) {
        $themeNodes.Add($node)
        for($index=0;$index -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$index++) { Get-ThemeNodes ([Windows.Media.VisualTreeHelper]::GetChild($node,$index)) }
    }
    Get-ThemeNodes $root
    $probe=$themeNodes|Where-Object {($_ -is [Windows.Controls.CheckBox]) -or ($_ -is [Windows.Controls.TextBox])}|Select-Object -First 1
    if($null -eq $probe){throw 'No live theme probe'}
    $before=$probe.Foreground.Color.ToString()
    $root.Resources[$foregroundKey]=if($Theme -eq 'Dark'){[Windows.Media.Brushes]::Black}else{[Windows.Media.Brushes]::White}
    $root.Resources[$backgroundKey]=if($Theme -eq 'Dark'){[Windows.Media.Brushes]::White}else{[Windows.Media.BrushConverter]::new().ConvertFromString('#FF1E1E1E')}
    $root.UpdateLayout()
    [Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke([Action]{},[Windows.Threading.DispatcherPriority]::DataBind)
    $after=$probe.Foreground.Color.ToString()
    if($before -eq $after){throw 'Open view did not follow theme resource change'}
    @{Before=$before;After=$after;Passed=$true;Scope='Same open WPF view, theme resource switch; not actual IDE theme action'}|ConvertTo-Json|Set-Content ($Output+'.theme-switch.json') -Encoding utf8
}
