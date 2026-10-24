using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CodeAssist.Core;
using Microsoft.VisualStudio.PlatformUI;
namespace CodeAssist.Extension {
 internal static class SourceLinkOptions {
  private sealed class LabelConverter:IValueConverter {
   internal string Language;public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>UiText.Get(value?.ToString(),Language);public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();
  }
  internal static void Render(StackPanel content,StartupPreferences draft,Action<string,bool> validate){
   string L(string text)=>UiText.Get(text,draft.UiLanguage);
   T Id<T>(T element,string id)where T:FrameworkElement{System.Windows.Automation.AutomationProperties.SetAutomationId(element,"SourceLinks."+id);return element;}
   var header=new DockPanel {Margin=new Thickness(0,0,0,10)};content.Children.Add(header);
   var enabled=Id(new CheckBox {Content=L("Enable Source Links"),IsChecked=draft.SourceLinksEnabled,Margin=new Thickness(0,0,20,0)},"Enabled");enabled.Click+=(s,e)=>draft.SourceLinksEnabled=enabled.IsChecked==true;DockPanel.SetDock(enabled,Dock.Left);header.Children.Add(enabled);
   var show=Id(new CheckBox {Content=L("Show action"),IsChecked=draft.SourceLinksShowAction,Margin=new Thickness(12,0,0,0)},"ShowAction");show.Click+=(s,e)=>draft.SourceLinksShowAction=show.IsChecked==true;DockPanel.SetDock(show,Dock.Right);header.Children.Add(show);
   var gesture=Id(new ComboBox {ItemsSource=new[]{"Double-click","Alt+click"},SelectedItem=draft.SourceLinkOpenGesture,Width=145,HorizontalAlignment=HorizontalAlignment.Right},"Gesture");gesture.SelectionChanged+=(s,e)=>draft.SourceLinkOpenGesture=gesture.SelectedItem as string??"Double-click";header.Children.Add(gesture);
   var rules=new ObservableCollection<SourceLinkRule>((draft.SourceLinkRules??new System.Collections.Generic.List<SourceLinkRule>()));
   void Publish()=>draft.SourceLinkRules=rules.ToList();
   var list=Id(new DataGrid {ItemsSource=rules,AutoGenerateColumns=false,CanUserAddRows=false,SelectionMode=DataGridSelectionMode.Single,Height=210,HeadersVisibility=DataGridHeadersVisibility.Column,RowHeaderWidth=0,Margin=new Thickness(0,0,0,10)},"Rules");
   ProductTheme.BrushFor(list,Control.BackgroundProperty,"ColorEditor");ProductTheme.BrushFor(list,Control.ForegroundProperty,"Text");
   var style=new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));style.Setters.Add(new Setter(Control.BackgroundProperty,new DynamicResourceExtension(EnvironmentColors.ToolWindowBackgroundBrushKey)));style.Setters.Add(new Setter(Control.ForegroundProperty,new DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));style.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(6,4,6,4)));list.ColumnHeaderStyle=style;
   var rowStyle=new Style(typeof(DataGridRow));rowStyle.Setters.Add(new Setter(Control.BackgroundProperty,new DynamicResourceExtension(EnvironmentColors.ToolWindowBackgroundBrushKey)));rowStyle.Setters.Add(new Setter(Control.ForegroundProperty,new DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));list.RowStyle=rowStyle;
   var cellStyle=new Style(typeof(DataGridCell));cellStyle.Setters.Add(new Setter(Control.BackgroundProperty,Brushes.Transparent));cellStyle.Setters.Add(new Setter(Control.ForegroundProperty,new DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));list.CellStyle=cellStyle;
   var checkboxStyle=new Style(typeof(CheckBox));checkboxStyle.Setters.Add(new Setter(Control.ForegroundProperty,new DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));checkboxStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center));
   list.Columns.Add(new DataGridCheckBoxColumn {Header=L("Enabled"),Binding=new Binding("Enabled"),Width=75,ElementStyle=checkboxStyle,EditingElementStyle=checkboxStyle});list.Columns.Add(new DataGridTextColumn {Header=L("Scope"),Binding=new Binding("Scope"),Width=80,IsReadOnly=true});list.Columns.Add(new DataGridTextColumn {Header=L("Name"),Binding=new Binding("Name"),Width=new DataGridLength(1,DataGridLengthUnitType.Star),MinWidth=240});content.Children.Add(list);
   var commandRow=new DockPanel {LastChildFill=false,Margin=new Thickness(0,0,0,15)};content.Children.Add(commandRow);
   var presets=Id(new ComboBox {ItemsSource=new[]{"URL","FogBugz","GitHub","Jira","File Viewer","External Tool"},SelectedIndex=0,Width=100,Margin=new Thickness(0,0,6,0)},"Preset");DockPanel.SetDock(presets,Dock.Left);commandRow.Children.Add(presets);
   var message=ProductTheme.Text("",12);message.TextWrapping=TextWrapping.Wrap;
   var form=new Grid();form.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(76)});form.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(1,GridUnitType.Star)});
   void Form(string label,FrameworkElement editor){var row=form.RowDefinitions.Count;form.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});var caption=ProductTheme.Text(L(label),12);caption.Margin=new Thickness(0,4,8,8);Grid.SetRow(caption,row);form.Children.Add(caption);editor.Margin=new Thickness(0,0,0,9);Grid.SetRow(editor,row);Grid.SetColumn(editor,1);form.Children.Add(editor);}
   var name=Id(new TextBox(),"Name");Form("Name",name);
   var provider=Id(new ComboBox {ItemsSource=new[]{"URL","FogBugz","GitHub","Jira","File Viewer","External Tool"},SelectedIndex=0},"Provider");Form("Plug-in",provider);
   var pattern=Id(new TextBox(),"Pattern");Form("Pattern",pattern);
   var keywordRow=new Grid();keywordRow.ColumnDefinitions.Add(new ColumnDefinition());keywordRow.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(70)});keywordRow.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(100)});var keywords=Id(new TextBox(),"Keywords");keywordRow.Children.Add(keywords);var suffix=Id(new TextBox {ToolTip=L("Suffixes"),Margin=new Thickness(8,0,8,0)},"Suffix");Grid.SetColumn(suffix,1);keywordRow.Children.Add(suffix);var kind=Id(new ComboBox {ItemsSource=new[]{"Number","Text"},SelectedIndex=0},"ValueKind");Grid.SetColumn(kind,2);keywordRow.Children.Add(kind);Form("Keywords",keywordRow);
   var behavior=Id(new ComboBox {ItemsSource=new[]{"Open link","Copy link"},SelectedItem=draft.SourceLinksAction},"Behavior");behavior.SelectionChanged+=(s,e)=>draft.SourceLinksAction=behavior.SelectedItem as string??"Open link";Form("Behavior",behavior);
   var target=Id(new TextBox(),"Target");Form("URL / Exe",target);var arguments=Id(new TextBox(),"Arguments");Form("Exe Args",arguments);
   var regex=Id(new CheckBox {Content=L("Regular expression"),Margin=new Thickness(0,0,0,9)},"Regex");Form("",regex);
   bool loading=false;
   SourceLinkRule Read()=>new SourceLinkRule {Name=name.Text,Enabled=(list.SelectedItem as SourceLinkRule)?.Enabled??true,Scope=(list.SelectedItem as SourceLinkRule)?.Scope??"User",Provider=(provider.SelectedItem as string)=="File Viewer"?SourceLinkProvider.FileViewer:(provider.SelectedItem as string)=="External Tool"?SourceLinkProvider.ExternalTool:SourceLinkProvider.Url,PluginId=provider.SelectedItem as string??"URL",Pattern=pattern.Text,Keywords=keywords.Text,Suffix=suffix.Text,ValueKind=kind.SelectedItem as string??"Number",Target=target.Text,Arguments=arguments.Text,RegularExpression=regex.IsChecked==true};
   void Update(){if(loading||!(list.SelectedItem is SourceLinkRule current))return;try{var edited=Read();SourceLinks.Validate(edited);var index=rules.IndexOf(current);if(index>=0){loading=true;rules[index]=edited;list.SelectedItem=edited;loading=false;}Publish();validate("sourceLink",true);message.Text="";}catch(ArgumentException error){validate("sourceLink",false);message.Text=error.Message;}}
   void Load(){if(loading)return;loading=true;var rule=list.SelectedItem as SourceLinkRule;form.IsEnabled=rule!=null;name.Text=rule?.Name??"";provider.SelectedItem=rule?.Provider==SourceLinkProvider.FileViewer?"File Viewer":rule?.Provider==SourceLinkProvider.ExternalTool?"External Tool":rule?.PluginId??"URL";pattern.Text=rule?.Pattern??"";keywords.Text=rule?.Keywords??"";suffix.Text=rule?.Suffix??"";kind.SelectedItem=rule?.ValueKind??"Number";target.Text=rule?.Target??"";arguments.Text=rule?.Arguments??"";regex.IsChecked=rule?.RegularExpression??false;arguments.IsEnabled=rule?.Provider==SourceLinkProvider.ExternalTool;loading=false;validate("sourceLink",true);message.Text="";}
   foreach(var box in new[]{name,pattern,keywords,suffix,target,arguments})box.TextChanged+=(s,e)=>Update();provider.SelectionChanged+=(s,e)=>{arguments.IsEnabled=(provider.SelectedItem as string)=="External Tool";Update();};kind.SelectionChanged+=(s,e)=>Update();regex.Click+=(s,e)=>Update();list.SelectionChanged+=(s,e)=>Load();
   void AddButton(string caption,string id,Action action){var button=Id(ProductTheme.Button(L(caption),action),id);button.MinWidth=60;button.Margin=new Thickness(0,0,6,0);DockPanel.SetDock(button,Dock.Left);commandRow.Children.Add(button);}
   AddButton("Add","Add",()=>{var rule=SourceLinks.Preset(presets.SelectedItem as string);rules.Add(rule);Publish();list.SelectedItem=rule;});
   AddButton("Duplicate","Duplicate",()=>{if(list.SelectedItem is SourceLinkRule rule){var copy=rule.Copy();copy.Name+=" copy";rules.Add(copy);Publish();list.SelectedItem=copy;}});
   AddButton("Remove","Remove",()=>{if(list.SelectedItem is SourceLinkRule rule){rules.Remove(rule);Publish();Load();}});
   var import=Id(ProductTheme.Button(L("Plug-in Directories..."),()=>{
    var layout=new StackPanel {Margin=new Thickness(16)};ProductTheme.Apply(layout);layout.Children.Add(ProductTheme.Text(L("Source link plug-in directories"),16));var paths=new TextBox {Text=draft.SourceLinkPluginDirectories,AcceptsReturn=true,Height=130,Margin=new Thickness(0,12,0,12)};layout.Children.Add(paths);var error=ProductTheme.Text("",12);layout.Children.Add(error);var window=new Window {Title=L("Source link plug-in directories"),Content=layout,Width=540,Height=330,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=Window.GetWindow(content)};ProductTheme.Apply(layout);
    layout.Children.Add(ProductTheme.Text(L("Import XML source-link provider definitions from these folders."),12));layout.Children.Add(ProductTheme.Button(L("Import"),()=>{try{var imported=draft.ConfiguredDirectories(paths.Text).SelectMany(SourceLinks.ImportDirectory).ToArray();foreach(var rule in imported){var old=rules.FirstOrDefault(r=>r.Name==rule.Name);if(old!=null)rules.Remove(old);rules.Add(rule);}draft.SourceLinkPluginDirectories=paths.Text;Publish();window.DialogResult=true;}catch(Exception failure)when(failure is ArgumentException||failure is IOException||failure is InvalidOperationException||failure is System.Xml.XmlException||failure is UnauthorizedAccessException){error.Text=failure.Message;}}));window.ShowDialog();
   }),"Import");DockPanel.SetDock(import,Dock.Right);commandRow.Children.Add(import);content.Children.Add(form);content.Children.Add(message);
   foreach(var combo in new[]{gesture,presets,provider,kind,behavior}){var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding {Converter=new LabelConverter {Language=draft.UiLanguage}});combo.ItemTemplate=new DataTemplate {VisualTree=text};}
   Load();
  }
 }
}
