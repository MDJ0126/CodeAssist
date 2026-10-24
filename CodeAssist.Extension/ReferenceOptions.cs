using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CodeAssist.Core;
namespace CodeAssist.Extension {
 internal static class ReferenceOptions {
  internal static void Render(string page,StackPanel content,StartupPreferences draft,Action<string,bool> validation) {
   string L(string text)=>UiText.Get(text,draft.UiLanguage);
   void Label(string text){var label=ProductTheme.Text(L(text),12);label.Margin=new Thickness(0,10,0,8);content.Children.Add(label);}
   PropertyInfo Property(string key)=>typeof(StartupPreferences).GetProperty(key)??throw new ArgumentException(key);
   void Identify(FrameworkElement element,string key,string caption){element.Tag="Option."+key;System.Windows.Automation.AutomationProperties.SetAutomationId(element,"Option."+key);System.Windows.Automation.AutomationProperties.SetName(element,L(caption));}
   void Check(string caption,string key,int indent=0){var property=Property(key);var check=new CheckBox {Content=L(caption),IsChecked=(bool)property.GetValue(draft),Margin=new Thickness(indent,0,0,9)};Identify(check,key,caption);check.Click+=(s,e)=>property.SetValue(draft,check.IsChecked==true);content.Children.Add(check);}
   void Text(string caption,string key,bool multiline=false){Label(caption);var property=Property(key);var input=new TextBox {Text=(string)property.GetValue(draft),AcceptsReturn=multiline,Height=multiline?100:26,Margin=new Thickness(20,0,0,10),VerticalScrollBarVisibility=multiline?ScrollBarVisibility.Auto:ScrollBarVisibility.Disabled};Identify(input,key,caption);input.TextChanged+=(s,e)=>{try{if(key=="ProjectAdditionalExtensions")FileExtensionSettings.Parse(input.Text);if(key.EndsWith("Directory",StringComparison.Ordinal)){foreach(var directory in draft.ConfiguredDirectories(input.Text))Path.GetFullPath(directory);}property.SetValue(draft,input.Text);validation(key,true);}catch(ArgumentException){validation(key,false);}catch(NotSupportedException){validation(key,false);}catch(PathTooLongException){validation(key,false);}};content.Children.Add(input);}
   void Number(string caption,string key,int min,int max){var property=Property(key);var row=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(20,0,0,10)};row.Children.Add(ProductTheme.Text(L(caption),12));var input=new TextBox {Text=property.GetValue(draft).ToString(),Width=65,Margin=new Thickness(10,0,0,0)};Identify(input,key,caption);input.TextChanged+=(s,e)=>{int number;var valid=int.TryParse(input.Text,out number)&&number>=min&&number<=max;if(valid)property.SetValue(draft,number);validation(key,valid);};row.Children.Add(input);content.Children.Add(row);}
   void Choice(string caption,string key,params string[] values){var property=Property(key);var choices=values.Select(v=>new ComboBoxItem {Content=L(v),Tag=v}).ToArray();var row=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(20,0,0,10)};row.Children.Add(ProductTheme.Text(L(caption),12));var combo=new ComboBox {ItemsSource=choices,SelectedItem=choices.FirstOrDefault(v=>(string)v.Tag==property.GetValue(draft).ToString()),MinWidth=170,MaxWidth=260,Margin=new Thickness(12,0,0,0)};Identify(combo,key,caption);combo.SelectionChanged+=(s,e)=>{if(combo.SelectedItem is ComboBoxItem item)property.SetValue(draft,property.PropertyType.IsEnum?Enum.Parse(property.PropertyType,(string)item.Tag):(object)(string)item.Tag);};row.Children.Add(combo);content.Children.Add(row);}
   void Radios(string caption,string key,params string[] values){Label(caption);var property=Property(key);foreach(var value in values){var captured=value;var radio=new RadioButton {Content=L(value),GroupName="Reference"+key,IsChecked=(string)property.GetValue(draft)==value,Margin=new Thickness(20,0,0,8)};Identify(radio,key+"."+value,caption+" "+value);radio.Checked+=(s,e)=>property.SetValue(draft,captured);content.Children.Add(radio);}}
   void Color(string caption,string key){var property=Property(key);var row=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(20,0,0,10)};var button=ProductTheme.Button("",()=>{});button.Width=38;button.MinWidth=38;button.Height=23;button.Margin=new Thickness(0,0,8,0);var swatch=new System.Windows.Controls.Border {Width=28,Height=14};void Paint(){try{swatch.Background=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString((string)property.GetValue(draft));}catch(FormatException){swatch.Background=System.Windows.Media.Brushes.Transparent;}}Paint();button.Content=swatch;Identify(button,key,caption);button.Click+=(s,e)=>ColorSelectionPopup.Show(button,L(caption),(string)property.GetValue(draft),draft,color=>{property.SetValue(draft,color);Paint();});row.Children.Add(button);row.Children.Add(ProductTheme.Text(L(caption),12));content.Children.Add(row);}
   switch(page){
   case "Goto":
    Radios("Scope of Goto","GotoScope","Project","Solution");
    Radios("Execute on press of Go button","GotoButtonAction","Goto","Go to Related");
    Choice("Matches","GotoMatchMode","Show matches first","Go directly to first match");
    Check("Prefer implementation on Goto","GotoPreferImplementation",0);
    Check("Select symbol on Goto","GotoSelectSymbol",0);
    break;
   case "Highlighting":
    Check("Highlight write references","HighlightWriteReferences",0);
    Color("Write references","WriteReferenceColor");
    Check("Highlight write and read references to symbol under cursor","AutomaticReferenceHighlights",0);
    Color("References","ReferenceHighlightColor");
    Check("Highlight matching words when caret is in comments and strings","HighlightCommentWords",0);
    Check("Highlight results of Quick Find and Find in Files","FindResultHighlighting",0);
    Check("Highlight matching braces","HighlightMatchingBraces",0);
    Color("Matching braces","MatchingBraceColor");
    Check("Highlight mismatching braces","HighlightMismatchingBraces",0);
    Color("Mismatching braces","MismatchingBraceColor");
    Check("Highlight solution file paths in build output window","OutputFileHighlighting",0);
    Check("Highlight current line with","HighlightCurrentLine",0);
    Radios("Line appearance","CurrentLineStyle","Thin frame","Background color","Ray lines");
    Color("Current line","CurrentLineColor");
    break;
   case "Underlining":
    Check("Underline spelling errors in comments and strings","SpellUnderliningEnabled",0);
    Color("Spelling underline color","UnderlineSpellingColor");
    Check("Underline unrecognized symbols","UnderlineUnknownSymbols",0);
    Color("Unrecognized symbol underline color","UnderlineUnknownColor");
    break;
   case "Display":
    Check("Switch to integrated main menu","IntegratedMainMenu",0);
    Check("Display CodeAssist Navigation Bar","ContextBarEnabled",0);
    Radios("Position","NavigationBarPosition","Top","Bottom");
    Label("Sort dropdown by");
    var sort=new ComboBox {ItemsSource=new[]{L("Occurrence"),L("Alphabetically")},SelectedIndex=draft.SortNavigationAlphabetically?1:0,Width=180,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(20,0,0,10)};sort.SelectionChanged+=(s,e)=>draft.SortNavigationAlphabetically=sort.SelectedIndex==1;content.Children.Add(sort);
    Check("Display indicator after column","ColumnIndicatorEnabled",0);
    Number("Column","ColumnIndicator",20,500);
    Check("Show Quick Info tooltips for more symbols","QuickInfoEnabled",0);
    Check("Show comments from source files when available","QuickInfoComments",20);
    Check("Show tooltips for braces and structural keywords","QuickInfoStructural",20);
    Check("Format doxygen comments in Quick Info tooltips","QuickInfoDoxygen",20);
    Check("Use enhanced macro expansion","EnhancedMacroExpansion",0);
    Check("Format enhanced expansion","FormatMacroExpansion",20);
    Check("Keep both original and enhanced expansions","KeepOriginalMacroExpansion",20);
    Check("Use last active document context for miscellaneous files","MiscLastDocumentContext",0);
    Check("Use forward slashes in navigation bar for include paths","NavigationForwardSlashes",0);
    break;
   case "Hashtags":
    Check("Allow hyphens in hashtags","HashtagAllowHyphens",0);
    Check("Allow hashtags consisting solely of hexadecimal letters","HashtagAllowHex",0);
    Check("Display global commands in hashtag context menu","HashtagGlobalCommands",0);
    Number("Minimum hashtag length","HashtagMinimumLength",1,100);
    break;
   case "Enhanced Listboxes":
    Check("Display filtering toolbar when hovering","CompletionToolbarOnHover",0);
    Check("Shrink as you type","CompletionShrink",0);
    Check("Allow Acronyms and Shorthand","AcronymCompletionEnabled",0);
    Check("Prefer Acronym matches","CompletionPreferAcronyms",20);
    Check("Bold non-inherited members","CompletionBoldNonInherited",0);
    Check("List non-inherited entries first","ListNonInheritedMembersFirst",0);
    Label("Accept completion with");
    Check("Tab","CompletionCommitWithTab",20);
    Check("Additional characters","CompletionAdditionalCharacters",20);
    Check("Enter","CompletionCommitWithEnter",20);
    Radios("Completion list","CompletionProvider","Visual Assist","Default Intellisense");
    Choice("Selection style","CompletionSelectionStyle","Default","Select best match","Select first item");
    break;
   case "Projects and Files":
    Check("Parse files without extensions as headers","ProjectParseExtensionless",0);
    Text("Additional file extensions","ProjectAdditionalExtensions");
    Label("Parse miscellaneous files");
    Check("If Solution is empty","ProjectParseEmptySolution",20);
    Check("If Solution is not empty","ProjectParseNonEmptySolution",20);
    Check("Enumerate project references thoroughly","ProjectThoroughReferences",0);
    Check("Display project nodes in Find References results","ReferenceProjectNodes",0);
    Check("Display project nodes in Rename dialog references lists","RenameProjectNodes",0);
    break;
   case "C/C++ Directories":
    Choice("Platform","DirectoryPlatform","Custom","Win32","x64","ARM64");
    Choice("Show directories for","DirectoryKind","Include files","Source files","Excluded directories");
    var paths=new TextBox {AcceptsReturn=true,Height=180,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(20,0,0,10)};string DirectoryProperty()=>draft.DirectoryKind=="Source files"?"UserSourceDirectories":draft.DirectoryKind=="Excluded directories"?"UserExcludedDirectories":"UserIncludeDirectories";paths.Text=(string)Property(DirectoryProperty()).GetValue(draft);paths.TextChanged+=(s,e)=>Property(DirectoryProperty()).SetValue(draft,paths.Text);content.Children.Add(paths);
    var selector=content.Children.OfType<StackPanel>().SelectMany(row=>row.Children.OfType<ComboBox>()).Last();selector.SelectionChanged+=(s,e)=>paths.Text=(string)Property(DirectoryProperty()).GetValue(draft);
    Label("One directory per line; environment variables are supported.");
    break;
   case "Code Generation":
    Check("Maintain order in header and source files for generated implementations","GenerationMaintainOrder");
    Number("Lines between generated implementations","GenerationSpacing",0,20);
    Check("Include default parameter values in generated implementations","GenerationIncludeDefaults");
    Choice("Add keyword to generated C++ methods","GenerationMethodKeyword","override","final","");
    Check("Insert open brace on new line during Introduce Variable and Add Braces","GenerationOpenBraceNewLine");
    Choice("Add Include style","GenerationIncludeStyle","Context dependent","Quotes","Angle brackets");
    Choice("Add Include path","GenerationIncludePath","Shortest possible path","Project relative","Full path");
    break;
   case "Project-Wide":
    Label("When a solution is opened");
    Choice("Project-wide inspections","InspectionMode","Ask","Project","Disabled");
    Label("Tool window");
    Choice("Show questions in","InspectionQuestionSurface","Visual Studio main window","Tool window");
    Check("Show inspection window when a question needs an answer","InspectionShowQuestions");
    Check("Show the Stats tab","InspectionShowStats");
    Label("Projects");
    Choice("Include initially","InspectionIncludeInitially","Ask","Current project","All projects");
    Check("Skip Unreal Engine project when including all projects","InspectionSkipUnreal");
    Choice("When a project is added","InspectionProjectAdded","Ask","Include","Exclude");
    Choice("When a project is excluded","InspectionProjectExcluded","Leave results in database","Remove results");
    Label("When the build target changes");
    Choice("Configuration or platform changed","InspectionBuildTargetChanged","Ask","Reparse","Keep results");
    Label("Checkers");
    Choice("Checker set","InspectionCheckerSet","Combined","Syntax","Identifiers");
    Choice("When the checker set changes","InspectionCheckerChanged","Ask","Reparse","Keep results");
    Label("Unsaved editor buffers");
    Check("Parse what is in the editor before saving","InspectionParseBeforeSave");
    Label("When a header change affects many files");
    Number("If it affects more than (files)","InspectionHeaderThreshold",0,100000);
    Number("Wait before reparsing (ms)","InspectionHeaderDelay",0,60000);
    Choice("Then","InspectionHeaderAction","Reparse all on save","Reparse immediately","Ask");
    Label("Cores and priorities");
    Choice("Cores used for processing","InspectionCorePolicy","Half","All","Explicit");
    Number("Explicit core count","InspectionExplicitCores",0,256);
    Choice("Of those, low-priority","InspectionLowPriorityPolicy","All but one","None","Explicit");
    Number("Explicit low-priority cores","InspectionExplicitLowPriorityCores",0,256);
    Label("Applying quick fixes");
    Check("Confirm before applying all listed quick fixes","InspectionConfirmFixes");
    Label("Database and logs");
    Number("Backups to keep","InspectionDatabaseBackups",0,100);
    Number("Max log size (MB)","InspectionMaxLogMegabytes",0,100000);
    Label("Results list");
    Number("Stop loading results after","InspectionResultLimit",1,1000000);
    break;
   case "Per-File":
    Choice("Checkers","InspectionCheckerSet","Combined","Syntax","Identifiers");
    Check("Underline and dim code issues","InspectionUnderline",0);
    Check("Show ticks in vertical scroll bar","InspectionScrollbarTicks",0);
    Check("Reparse automatically","InspectionAutoReparse",0);
    Check("Even when on battery power","InspectionOnBattery",0);
    Check("Enable while debugging","InspectionWhileDebugging",0);
    break;
   case "Debug Assistance":
    Check("Enable debugger integration required by Step Filter","StepFilterEnabled",0);
    Radios("Step filter settings storage","DebugFilterStorage","Solution","User");
    Text("Step filter function patterns","StepFilterPatterns",true);
    break;
   case "Mouse":
    Check("Execute Ctrl + click","MouseCtrlClick",0);
    Choice("Ctrl + click action","MouseCtrlClickAction","Goto","Find References","Go to Related","Open Corresponding File");
    Check("Execute Ctrl + Shift + click","MouseCtrlShiftClick",0);
    Choice("Ctrl + Shift + click action","MouseCtrlShiftClickAction","Goto","Find References","Go to Related","Open Corresponding File");
    Check("Execute middle click","MouseMiddleClick",0);
    Choice("Middle click action","MouseMiddleClickAction","Goto","Find References","Go to Related","Open Corresponding File");
    Check("Execute Alt + middle click","MouseAltMiddleClick",0);
    Choice("Alt + middle click action","MouseAltMiddleClickAction","Goto","Find References","Go to Related","Open Corresponding File");
    Check("Alt + click: Go to definition","AltClickGoToDefinition",0);
    Check("Override Ctrl + Wheel","MouseOverrideCtrlWheel",0);
    Choice("Ctrl + Wheel action","MouseCtrlWheelAction","Zoom","Move scope","Navigate references");
    Check("Override Ctrl + Shift + Wheel","MouseOverrideCtrlShiftWheel",0);
    Choice("Ctrl + Shift + Wheel action","MouseCtrlShiftWheelAction","Zoom","Move scope","Navigate references");
    Check("Display help content on hover in options dialog","OptionsHoverHelp",0);
    break;
   case "Performance":
    Check("Enable multithreaded parsing","ParsingMultithreaded",0);
    Check("Limit maximum number of concurrent threads","ParsingLimitThreads",20);
    Number("Parser threads","ParseConcurrency",1,16);
    Choice("Thread priority","ParsingPriority","Normal","Below normal","Lowest");
    Check("Watch externally modified files and reparse when necessary","ParsingWatchExternal",0);
    Check("Parse all files when opening a project","ParsingOnProjectOpen",0);
    Check("Do not parse files excluded by .vscode/settings.json","ParsingRespectVsCodeExcludes",0);
    Check("Use alternate directory for symbol database","UseAlternateSymbolDirectory",0);
    Text("Symbol database directory","AlternateSymbolDirectory");
    Number("Document update delay (ms)","LiveUpdateDelay",50,1000);
    break;
   case "Startup":
    Check("Open Welcome on startup","ShowWelcomeOnStartup",0);
    Check("Hide project inspection prompt","HideWelcomeBar",0);
    Label("Integration");
    Check("Enable enhanced completion","CompletionEnabled",0);
    Check("Enable enhanced symbol coloring","ColoringEnabled",0);
    break;
   case "Logging and Recovery":
    Check("Enable diagnostic logging","DiagnosticLoggingEnabled",0);
    Text("Diagnostic log directory","DiagnosticLogDirectory");
    Check("Keep previous settings backup for recovery","RecoveryKeepBackup",0);
    Label("Settings backup is created when saving preferences. Diagnostic paths are local to this computer.");
    break;
   case "System Info":
    Label("CodeAssist "+ExtensionPackage.ProductVersion);
    Label("Visual Studio runtime: "+Environment.Version);
    Label("Process: "+(Environment.Is64BitProcess?"64-bit":"32-bit"));
    Label("Operating system: "+Environment.OSVersion);
    Label("Processors: "+Environment.ProcessorCount);
    Label("Settings: "+StartupUi.StatePath);
    break;
   default:throw new ArgumentException(page);
   }
  }
 }
}
