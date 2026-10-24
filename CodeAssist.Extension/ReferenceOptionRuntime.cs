using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.Extension {
 [Export(typeof(IWpfTextViewCreationListener))][ContentType("C/C++")][TextViewRole(PredefinedTextViewRoles.Document)]
 internal sealed class ReferenceOptionRuntime:IWpfTextViewCreationListener {
  [Import]internal IEditorFormatMapService Formats=null;
  public void TextViewCreated(IWpfTextView view){new Binding(view,Formats?.GetEditorFormatMap(view));}
  private sealed class Binding {
   private readonly IWpfTextView view;private readonly IEditorFormatMap formats;
   internal Binding(IWpfTextView view,IEditorFormatMap formats){this.view=view;this.formats=formats;view.VisualElement.PreviewMouseUp+=Mouse;view.VisualElement.PreviewMouseWheel+=Wheel;view.Closed+=Closed;StartupUi.SettingsApplied+=Apply;Apply();}
   private void Apply(){if(formats==null||view.IsClosed)return;var p=StartupUi.Preferences;Set("CodeAssist Matched Brace",p.MatchingBraceColor);Set("CodeAssist Mismatched Brace",p.MismatchingBraceColor);Set("CodeAssist Find Selected",p.ReferenceHighlightColor);}
   private void Set(string name,string color){try{var properties=formats.GetProperties(name);properties["BackgroundColor"]=(Color)ColorConverter.ConvertFromString(color);formats.SetProperties(name,properties);}catch(FormatException){} }
   private void Mouse(object sender,MouseButtonEventArgs e){if(e.Handled)return;var p=StartupUi.Preferences;var modifier=Keyboard.Modifiers;string action=null;
    if(e.ChangedButton==MouseButton.Left&&modifier==(ModifierKeys.Control|ModifierKeys.Shift)&&p.MouseCtrlShiftClick)action=p.MouseCtrlShiftClickAction;
    else if(e.ChangedButton==MouseButton.Left&&modifier==ModifierKeys.Control&&p.MouseCtrlClick)action=p.MouseCtrlClickAction;
    else if(e.ChangedButton==MouseButton.Middle&&modifier==ModifierKeys.Alt&&p.MouseAltMiddleClick)action=p.MouseAltMiddleClickAction;
    else if(e.ChangedButton==MouseButton.Middle&&modifier==ModifierKeys.None&&p.MouseMiddleClick)action=p.MouseMiddleClickAction;
    if(action!=null){e.Handled=true;Execute(action);return;}
    if(e.ChangedButton==MouseButton.Left&&p.SourceLinksEnabled&&((p.SourceLinkOpenGesture=="Double-click"&&e.ClickCount==2&&modifier==ModifierKeys.None)||(p.SourceLinkOpenGesture=="Alt+click"&&modifier==ModifierKeys.Alt))){var position=view.Caret.Position.BufferPosition;var line=position.GetContainingLine();if(CodeAssist.Core.SourceLinks.Find(line.GetText(),p.SourceLinkRules).Any(link=>position.Position-line.Start.Position>=link.Start&&position.Position-line.Start.Position<=link.Start+link.Length)){e.Handled=true;StartupUi.Execute(0x17b);}}

   }
   private void Wheel(object sender,MouseWheelEventArgs e){if(e.Handled)return;var p=StartupUi.Preferences;var modifier=Keyboard.Modifiers;var action=modifier==ModifierKeys.Control&&p.MouseOverrideCtrlWheel?p.MouseCtrlWheelAction:modifier==(ModifierKeys.Control|ModifierKeys.Shift)&&p.MouseOverrideCtrlShiftWheel?p.MouseCtrlShiftWheelAction:null;
    if(action==null)return;e.Handled=true;if(action=="Zoom")view.ZoomLevel=Math.Max(20,Math.Min(400,view.ZoomLevel+(e.Delta>0?10:-10)));else StartupUi.Execute(action=="Move scope"?(e.Delta>0?0x10e:0x10d):(e.Delta>0?0x108:0x109));
   }
   private static void Execute(string action){StartupUi.Execute(action=="Find References"?0x102:action=="Go to Related"?0x107:action=="Open Corresponding File"?0x105:0x100);}
   private void Closed(object sender,EventArgs e){view.VisualElement.PreviewMouseUp-=Mouse;view.VisualElement.PreviewMouseWheel-=Wheel;view.Closed-=Closed;StartupUi.SettingsApplied-=Apply;}
  }
 }
}
