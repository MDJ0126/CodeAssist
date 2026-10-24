param([string]$Output = "$PSScriptRoot/../docs/validation/welcome-preview.png", [ValidateSet("Welcome","FirstRun","Settings")][string]$Screen = "Welcome", [ValidateSet("Light","Dark")][string]$Theme = "Light", [string]$OptionsPage = "", [switch]$TestInteractions, [string]$AssemblyDirectory = "$PSScriptRoot/../CodeAssist.Extension/bin/Release")
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
        foreach (var folder in new[] { Bin, Path.Combine(Ide,"PublicAssemblies"), Path.Combine(Ide,"PrivateAssemblies"), Path.Combine(Ide,"CommonExtensions","Microsoft","Editor"), Ide })
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
$themeType = $assembly.GetType('CodeAssist.Extension.ProductTheme',$true)
$root = [Windows.Controls.Border]::new()
$themeType.GetMethod('Apply',[Reflection.BindingFlags]'Static,NonPublic').Invoke($null,@($root)) | Out-Null
$core = [Reflection.Assembly]::LoadFrom((Join-Path ([PreviewAssemblyResolver]::Bin) 'CodeAssist.Core.dll'))
$token = $core.GetType('CodeAssist.Core.SemanticToken',$true)
$kind = [Enum]::Parse($core.GetType('CodeAssist.Core.SemanticTokenKind',$true),'Function')
$item = [Activator]::CreateInstance($token,@('BeginPlay',1,9,$kind))
$contextType = $assembly.GetType('CodeAssist.Extension.ContextBarMargin',$true)
$combo = $contextType.GetMethod('CreateMethodSelector',[Reflection.BindingFlags]'Static,NonPublic').Invoke($null,@())
$combo.Items.Add($item) | Out-Null
$combo.SelectedIndex=0
$root.Child=$combo
$root.Measure([Windows.Size]::new(300,40));$root.Arrange([Windows.Rect]::new(0,0,300,40));$root.UpdateLayout()
function Read-Text($node) {if($node -is [Windows.Controls.TextBlock]) {$node.Text};for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$i++){Read-Text ([Windows.Media.VisualTreeHelper]::GetChild($node,$i))}}
$labels = @(Read-Text $root)
if($labels -notcontains 'BeginPlay' -or ($labels -join ' ') -match 'SemanticToken') { throw "Method selection rendered incorrectly: $labels" }
$hostType = $assembly.GetType('CodeAssist.Extension.WelcomeDocumentWindow',$true)
$moniker = $hostType.GetProperty('Moniker',[Reflection.BindingFlags]'Static,NonPublic').GetValue($null,$null)
if(![IO.Path]::IsPathRooted($moniker) -or [IO.Path]::GetFullPath($moniker) -ne $moniker) { throw 'Invalid Welcome document path' }
$legacy = [Windows.Controls.ComboBox]::new()
# Recreate the pre-fix shared template, so the baseline is tested rather than assumed.
$templateSource=[IO.File]::ReadAllText("$PSScriptRoot/../CodeAssist.Extension/ProductOptions.cs")
$templateStart=$templateSource.IndexOf('<ControlTemplate xmlns=')
$templateEnd=$templateSource.IndexOf('</ControlTemplate>"',$templateStart)
$legacyTemplate=$templateSource.Substring($templateStart,$templateEnd-$templateStart+'</ControlTemplate>'.Length)
$legacyTemplate=$legacyTemplate.Replace(" ContentTemplateSelector='{TemplateBinding ItemTemplateSelector}'",'').Replace(" ContentStringFormat='{TemplateBinding SelectionBoxItemStringFormat}'",'')
$legacy.Template=[Windows.Markup.XamlReader]::Parse($legacyTemplate)
$legacy.DisplayMemberPath='Name'; $legacy.Items.Add($item) | Out-Null; $legacy.SelectedIndex=0
$root.Child=$legacy
$root.Measure([Windows.Size]::new(300,40));$root.Arrange([Windows.Rect]::new(0,0,300,40));$root.UpdateLayout()
$legacyLabels = @(Read-Text $root)
if(($legacyLabels -join ' ') -notmatch 'CodeAssist.Core.SemanticToken') {throw 'Original method selection bug was not reproduced'}
$invalidPathError=$null
try { [IO.Path]::GetFullPath('codeassist://welcome') | Out-Null } catch { $invalidPathError=$_.Exception.GetBaseException().GetType().Name }
if($invalidPathError -ne 'NotSupportedException') {throw 'Original moniker validation was not reproduced'}
$record = @{Version='0.0.76';Before=@{ClosedMethodSelection=$legacyLabels;WelcomeMoniker='codeassist://welcome';PathGetFullPath=$invalidPathError};After=@{ClosedMethodSelection=$labels;WelcomeMoniker=$moniker;PathGetFullPath='Passed'};NativeVsWelcomeClick='Unverified; registered editor factory and explicit DocData require actual VS open/close/reopen verification'}
$record | ConvertTo-Json -Depth 6 | Set-Content "$PSScriptRoot/../docs/validation/welcome-button-regression.json" -Encoding UTF8
$record | ConvertTo-Json -Depth 6


$dataType=$assembly.GetType('CodeAssist.Extension.WelcomeDocumentData',$true)
$data=[Activator]::CreateInstance($dataType)
$dirtyArgs=[object[]]@([int]1)
if($dataType.GetMethod('IsDocDataDirty').Invoke($data,$dirtyArgs) -ne 0 -or $dirtyArgs[0] -ne 0) {throw 'Welcome is dirty'}
$editorArgs=[object[]]@([Guid]::Empty)
$dataType.GetMethod('GetGuidEditorType').Invoke($data,$editorArgs) | Out-Null
$expectedEditor=[Guid]'fcb5ef5d-27a7-43f4-8e97-cfcf48219013'
if($editorArgs[0] -ne $expectedEditor){throw 'Wrong editor identity'}
$pointer=[Runtime.InteropServices.Marshal]::GetIUnknownForObject($data)
try {
    $iid=[Guid]'D5D49C61-1C0B-4EA1-9ADB-A79FB1DBC7B5'; $persist=[IntPtr]::Zero
    $hr=[Runtime.InteropServices.Marshal]::QueryInterface($pointer,[ref]$iid,[ref]$persist)
    if($hr -ne 0){throw 'IVsPersistDocData is not exposed to COM'}
    [Runtime.InteropServices.Marshal]::Release($persist) | Out-Null
} finally {[Runtime.InteropServices.Marshal]::Release($pointer) | Out-Null}
$saveArgs=[object[]]@([Enum]::ToObject($dataType.GetMethod('SaveDocData').GetParameters()[0].ParameterType,0),$null,[int]0)
$saveResult=$dataType.GetMethod('SaveDocData').Invoke($data,$saveArgs)
if($saveResult -ge 0 -or $saveArgs[2] -ne 1){throw 'Welcome save should be unavailable'}
@{Version='0.0.76';PersistDocDataQueryInterface='Passed';Dirty=$dirtyArgs[0];EditorGuid=$editorArgs[0];SaveUnavailable='Passed';FactoryAndNativeShell='Unverified; factory requires a real VS UI thread'} | ConvertTo-Json | Set-Content "$PSScriptRoot/../docs/validation/welcome-editor-contract.json" -Encoding UTF8
Write-Output 'Welcome DocData COM identity, clean state and save contract passed.'

Add-Type -AssemblyName System.Design
$menuService=[System.ComponentModel.Design.MenuCommandService]::new($null)
$script:welcomeInvocations=0; $script:settingsInvocations=0
$welcomeAction=[Action]{ $script:welcomeInvocations++ }
$settingsAction=[Action]{ $script:settingsInvocations++ }
$registration=$assembly.GetType('CodeAssist.Extension.StartupCommandRegistration',$true).GetMethod('Register',[Reflection.BindingFlags]'Static,NonPublic')
$registration.Invoke($null,@($menuService,$welcomeAction,$settingsAction)) | Out-Null
[xml]$commands=Get-Content "$PSScriptRoot/../CodeAssist.Extension/CodeAssistCommands.vsct" -Raw
$placements=@($commands.CommandTable.CommandPlacements.CommandPlacement | Where-Object {$_.Parent.id -eq 'CodeAssistToolbarGroup'} | Sort-Object {[Convert]::ToInt32($_.priority,16)})
$expectedToolbar=@('SettingsCommandId','OpenFileCommandId','FindSymbolCommandId','FindReferencesCommandId','PreviousContextCommandId','NextContextCommandId','SnippetCommandId','SpellCheckCommandId','CorrespondingFileCommandId','SkipBreakpointsCommandId','ThreadBreakpointsCommandId','WelcomeCommandId')
if(($placements.id -join ',') -ne ($expectedToolbar -join ',')){throw 'Toolbar placement order does not match the observed VA sequence plus Welcome'}
$symbols=$commands.CommandTable.Symbols.GuidSymbol | Where-Object {$_.name -eq 'guidCodeAssistCommandSet'}
$commandGuid=[Guid]$symbols.value
$welcomeId=[Convert]::ToInt32(($symbols.IDSymbol | Where-Object {$_.name -eq 'WelcomeCommandId'}).value,16)
$settingsId=[Convert]::ToInt32(($symbols.IDSymbol | Where-Object {$_.name -eq 'SettingsCommandId'}).value,16)
$menuService.FindCommand([System.ComponentModel.Design.CommandID]::new($commandGuid,$welcomeId)).Invoke()
if($script:welcomeInvocations -ne 1 -or $script:settingsInvocations -ne 0){throw 'Welcome command routed to wrong handler'}
$menuService.FindCommand([System.ComponentModel.Design.CommandID]::new($commandGuid,$settingsId)).Invoke()
if($script:welcomeInvocations -ne 1 -or $script:settingsInvocations -ne 1){throw 'Settings command routed to wrong handler'}
$nullRejected=$false
try{$registration.Invoke($null,@($null,$welcomeAction,$settingsAction))}catch{$nullRejected=$_.Exception.GetBaseException() -is [InvalidOperationException]}
if(!$nullRejected){throw 'Missing command service was silently ignored'}
@{Version='0.0.76';ServiceType=$menuService.GetType().FullName;ToolbarButton='WelcomeCommandId';ToolbarOrder=$expectedToolbar;WelcomeInvocations=$script:welcomeInvocations;SettingsInvocations=$script:settingsInvocations;MissingServiceRejected=$nullRejected;NativeVsClick='Unverified'} | ConvertTo-Json | Set-Content "$PSScriptRoot/../docs/validation/welcome-command-routing.json" -Encoding UTF8
$menuService.Dispose()
Write-Output 'Actual VSCT Welcome placement -> registered Welcome callback passed; settings is separate; null service rejected.'

$factoryType=$assembly.GetType('CodeAssist.Extension.WelcomeEditorFactory',$true)
$factory=[Activator]::CreateInstance($factoryType,[object[]]@($null))
$map=$factoryType.GetMethod('MapLogicalView')
$mapArgs=[object[]]@([Guid]::Empty,$null)
$mapResult=$map.Invoke($factory,$mapArgs)
if($mapResult -ne 0 -or $mapArgs[1] -ne 'Welcome'){throw 'Primary logical view did not resolve to the named Welcome physical view'}
$unsupportedArgs=[object[]]@([Guid]'eb48c5f7-f995-4d0b-a3f4-546e0b67a75f',$null)
$unsupportedResult=$map.Invoke($factory,$unsupportedArgs)
if($unsupportedResult -ge 0 -or $null -ne $unsupportedArgs[1]){throw 'Unsupported logical view should fail'}
$invalidArgs=[object[]]@([uint32]1,$moniker,'UnsupportedView',$null,[uint32]::MaxValue,[IntPtr]::Zero,[IntPtr]::Zero,[IntPtr]::Zero,'',[Guid]::Empty,[int]0)
$invalidResult=$factoryType.GetMethod('CreateEditorInstance').Invoke($factory,$invalidArgs)
if($invalidResult -ge 0 -or $invalidArgs[6] -ne [IntPtr]::Zero -or $invalidArgs[7] -ne [IntPtr]::Zero){throw 'Unsupported physical view allocated a document'}
@{Version='0.0.76';PrimaryView=$mapArgs[1];MapResult=$mapResult;UnsupportedLogicalViewRejected=$true;UnsupportedPhysicalViewRejected=$true;UnavailableActivityLogDoesNotBreakMapping=$true;NativeShell='Unverified'} | ConvertTo-Json | Set-Content "$PSScriptRoot/../docs/validation/welcome-view-mapping.json" -Encoding UTF8
Write-Output 'Named primary view mapping, unsupported view rejection and unavailable ActivityLog tolerance passed.'

$foregroundType=$assembly.GetType('CodeAssist.Extension.CompletionForegroundOverrides',$true)
$foreground=[Activator]::CreateInstance($foregroundType,$true)
$apply=$foregroundType.GetMethod('Apply',[Reflection.BindingFlags]'Instance,NonPublic')
$nativeSource=[Windows.Controls.Border]::new();$nativeSource.Background=[Windows.Media.Brushes]::White
$nativeText=[Windows.Controls.TextBlock]::new()
$binding=[Windows.Data.Binding]::new('Background');$binding.Source=$nativeSource
[Windows.Data.BindingOperations]::SetBinding($nativeText,[Windows.Controls.TextBlock]::ForegroundProperty,$binding)|Out-Null
$apply.Invoke($foreground,@($nativeText,[Windows.Controls.TextBlock]::ForegroundProperty,[Windows.Media.Brushes]::Orange,$true))|Out-Null
if($nativeText.Foreground.Color -ne [Windows.Media.Colors]::Orange){throw 'Completion palette was not applied'}
$apply.Invoke($foreground,@($nativeText,[Windows.Controls.TextBlock]::ForegroundProperty,[Windows.Media.Brushes]::Orange,$false))|Out-Null
$nativeSource.Background=[Windows.Media.Brushes]::Blue
if($nativeText.Foreground.Color -ne [Windows.Media.Colors]::Blue){throw 'Visual Studio foreground binding was not restored'}
$resourceText=[Windows.Controls.TextBlock]::new();$resourceText.Resources['NativeForeground']=[Windows.Media.Brushes]::Green
$resourceText.SetResourceReference([Windows.Controls.TextBlock]::ForegroundProperty,'NativeForeground')
$apply.Invoke($foreground,@($resourceText,[Windows.Controls.TextBlock]::ForegroundProperty,[Windows.Media.Brushes]::Orange,$true))|Out-Null
$apply.Invoke($foreground,@($resourceText,[Windows.Controls.TextBlock]::ForegroundProperty,[Windows.Media.Brushes]::Orange,$false))|Out-Null
$resourceText.Resources['NativeForeground']=[Windows.Media.Brushes]::Yellow
if($resourceText.Foreground.Color -ne [Windows.Media.Colors]::Yellow){throw 'Visual Studio dynamic resource was not preserved'}
@{Version='0.0.76';CustomForegroundApplied=$true;NativeBindingRestored=$true;ThemeResourcePreserved=$true;Scope='Owned WPF controls; live popup mode switch checked separately'}|ConvertTo-Json|Set-Content "$PSScriptRoot/../docs/validation/completion-native-color-contract.json" -Encoding UTF8
Write-Output 'Native completion foreground bindings and theme resources restored.'
