using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using CodeAssist.Core;
namespace CodeAssist.Extension {
 internal sealed class SymbolNameTextBlock:TextBlock {
  public static readonly DependencyProperty NameTextProperty=DependencyProperty.Register("NameText",typeof(string),typeof(SymbolNameTextBlock),new PropertyMetadata("",Changed));
  public static readonly DependencyProperty KindTextProperty=DependencyProperty.Register("KindText",typeof(string),typeof(SymbolNameTextBlock),new PropertyMetadata("Unknown",Changed));
  public static readonly DependencyProperty NormalizeScopesProperty=DependencyProperty.Register("NormalizeScopes",typeof(bool),typeof(SymbolNameTextBlock),new PropertyMetadata(true,Changed));
  public static readonly DependencyProperty UseNavigationBarColoringProperty=DependencyProperty.Register("UseNavigationBarColoring",typeof(bool),typeof(SymbolNameTextBlock),new PropertyMetadata(false,Changed));
  internal bool UseNavigationBarColoring {get=>(bool)GetValue(UseNavigationBarColoringProperty);set=>SetValue(UseNavigationBarColoringProperty,value); }
  public string NameText{get=>(string)GetValue(NameTextProperty);set=>SetValue(NameTextProperty,value);}
  public string KindText{get=>(string)GetValue(KindTextProperty);set=>SetValue(KindTextProperty,value);}
  public SymbolNameTextBlock(){Loaded+=(s,e)=>{StartupUi.SettingsApplied+=Refresh;VSColorTheme.ThemeChanged+=ThemeChanged;Refresh();};Unloaded+=(s,e)=>{StartupUi.SettingsApplied-=Refresh;VSColorTheme.ThemeChanged-=ThemeChanged;};}
  private static void Changed(DependencyObject d,DependencyPropertyChangedEventArgs e)=>((SymbolNameTextBlock)d).Refresh();
  private void ThemeChanged(ThemeChangedEventArgs e){if(Dispatcher.CheckAccess())Refresh();else Dispatcher.BeginInvoke(new Action(Refresh));}
  private void Refresh(){
   var text=NameText??"";if((bool)GetValue(NormalizeScopesProperty))text=text.Replace("::",".");Inlines.Clear();Foreground=RecommendedColors.DefaultTextBrush();
   if(!StartupUi.Preferences.ColoringEnabled||!(UseNavigationBarColoring?StartupUi.Preferences.ColorNavigationBar:StartupUi.Preferences.ColorViews)){Text=text;return;}
   SymbolKind kind;if(!Enum.TryParse(KindText,out kind))kind=SymbolKind.Unknown;if(kind==SymbolKind.Unknown||kind==SymbolKind.Header){Text=text;return;}var cursor=0;
   foreach(var token in SymbolDisplayTokens.Parse(text,kind)){if(token.Start>cursor)Inlines.Add(new Run(text.Substring(cursor,token.Start-cursor)));var run=new Run(text.Substring(token.Start,token.Length));var brush=RecommendedColors.DisplayBrush(token.Ink);if(brush!=null)run.Foreground=brush;Inlines.Add(run);cursor=token.Start+token.Length;}
   if(cursor<text.Length)Inlines.Add(new Run(text.Substring(cursor)));
  }
 }
}
