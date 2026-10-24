using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using CodeAssist.Core;
namespace CodeAssist.Extension
{
 internal sealed class QuickInfoFallback
 {
  internal IAsyncQuickInfoSession Session;internal ContainerElement Content;internal string Word;
  internal static bool HasNativeSignature(object item,string word)
  {
   if(item is ClassifiedTextElement text){if(text.Runs.Any(r=>NativeQuickInfoText.IsSymbolRun(r.ClassificationTypeName,r.Text,word)))return true;var value=NativeQuickInfoText.Normalize(string.Concat(text.Runs.Select(r=>r.Text)));return System.Text.RegularExpressions.Regex.IsMatch(value,@"\b"+System.Text.RegularExpressions.Regex.Escape(word)+@"\s*\(");}
   if(item is ContainerElement group)return group.Elements.Any(e=>HasNativeSignature(e,word));
   return false;
  }
 }
 [Export(typeof(IViewElementFactory))]
 [Name("CodeAssist QuickInfo fallback")]
 [TypeConversion(from:typeof(QuickInfoFallback),to:typeof(UIElement))]
 internal sealed class QuickInfoFallbackFactory:IViewElementFactory
 {
  [Import]internal IViewElementFactoryService Views=null;
  public TView CreateViewElement<TView>(ITextView view,object model) where TView:class
  {
   var fallback=model as QuickInfoFallback;if(fallback==null)return null;
   var host=new Border {Child=Views.CreateViewElement<UIElement>(view,fallback.Content)};
   Action refresh=()=>{
    if(fallback.Session.Content.Any(c=>!ReferenceEquals(c,fallback)&&QuickInfoFallback.HasNativeSignature(c,fallback.Word)))
    {host.Visibility=Visibility.Collapsed;WelcomeDocumentWindow.RecordEvent("QuickInfoFallback suppressed nativeSignature=True");}
   };
   refresh();
   EventHandler<QuickInfoSessionStateChangedEventArgs> handler=null;
   handler=(s,e)=>{if(e.NewState==QuickInfoSessionState.Visible){refresh();fallback.Session.StateChanged-=handler;}else if(e.NewState==QuickInfoSessionState.Dismissed)fallback.Session.StateChanged-=handler;};
   if(fallback.Session.State!=QuickInfoSessionState.Visible)fallback.Session.StateChanged+=handler;
   return host as TView;
  }
 }
 [Export(typeof(IViewElementFactory))]
 [Name("CodeAssist QuickInfo colors")]
 [TypeConversion(from:typeof(ClassifiedTextElement),to:typeof(UIElement))]
 [Order(Before="default")]
 internal sealed class QuickInfoColorFactory:IViewElementFactory
 {
  [ImportMany]internal IEnumerable<Lazy<IViewElementFactory,IDictionary<string,object>>> Factories=null;
  public TView CreateViewElement<TView>(ITextView view,object model)where TView:class
  {
   var text=model as ClassifiedTextElement;if(text==null)return null;
   IAsyncQuickInfoSession session;
   var active=view.Properties.TryGetProperty(typeof(QuickInfoFallback),out session)&&session.State!=QuickInfoSessionState.Dismissed;
   var map=active&&StartupUi.Preferences.ColoringEnabled&&StartupUi.Preferences.ColorTooltips;
   // Let the IDE retain its links, emphasis, font and other presentation behavior.
   // Only recolor existing native text runs; do not reconstruct the native model.
   var converted=text;
   foreach(var item in Factories)
   {
    object name,from,to;
    if(!item.Metadata.TryGetValue("Name",out name)||Equals(name,"CodeAssist QuickInfo colors"))continue;
    if(!item.Metadata.TryGetValue("FromFullName",out from)||!Equals(from,typeof(ClassifiedTextElement).AssemblyQualifiedName))continue;
    if(!item.Metadata.TryGetValue("ToFullName",out to)||!(Equals(to,typeof(UIElement).AssemblyQualifiedName)||Equals(to,typeof(FrameworkElement).AssemblyQualifiedName)))continue;
    var result=item.Value.CreateViewElement<TView>(view,converted);
    if(map&&result is DependencyObject element)
    {
     var runs=text.Runs.ToArray();ColorRuns(element,runs);
     if(element is FrameworkElement control)control.Loaded+=(s,e)=>ColorRuns(control,runs);
    }
    return result;
   }
   WelcomeDocumentWindow.RecordEvent("QuickInfoColorFactory native factory missing");
   return new System.Windows.Controls.TextBlock{Text=string.Concat(text.Runs.Select(r=>r.Text))} as TView;
  }
  private static IEnumerable<Run> Runs(InlineCollection inlines)
  {
   foreach(var inline in inlines){if(inline is Run run)yield return run;else if(inline is Span span)foreach(var child in Runs(span.Inlines))yield return child;}
  }
  private static void ColorRuns(DependencyObject element,ClassifiedTextRun[] source)
  {
   if(element is TextBlock block)
   {
    var index=0;
    foreach(var run in Runs(block.Inlines))
    {
     while(index<source.Length&&NativeQuickInfoText.Normalize(source[index].Text)!=NativeQuickInfoText.Normalize(run.Text))index++;
     if(index>=source.Length)break;
     var brush=BrushFor(source[index++].ClassificationTypeName);if(brush!=null)run.Foreground=brush;
    }
   }
   for(var i=0;i<VisualTreeHelper.GetChildrenCount(element);i++)ColorRuns(VisualTreeHelper.GetChild(element,i),source);
  }
  private static Brush BrushFor(string classification)
  {
   var name=(classification??"").ToLowerInvariant();
   if(name.Contains("namespace"))return new SolidColorBrush(RecommendedColors.ThemeColor(StartupUi.Preferences.NamespaceColor,"#B9D7A2"));
   if(name.Contains("enum member")||name.Contains("enummember"))return new SolidColorBrush(RecommendedColors.ThemeColor(StartupUi.Preferences.EnumMemberColor,"#B88020"));
   if(name.Contains("method")||name.Contains("function"))return RecommendedColors.SymbolBrush(SymbolKind.Function);
   if(name.Contains("class name")||name.Contains("struct name")||name.Contains("enum name")||name.Contains("type name"))return RecommendedColors.SymbolBrush(SymbolKind.Type);
   if(name.Contains("field")||name.Contains("local name")||name.Contains("parameter name"))return RecommendedColors.SymbolBrush(SymbolKind.Variable);
   if(name.Contains("macro"))return RecommendedColors.SymbolBrush(SymbolKind.Macro);
   return null;
  }
 }
}
