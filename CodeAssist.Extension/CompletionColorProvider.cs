using System;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Data;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodeAssist.Core;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.Extension
{
    internal sealed class ColoredCompletion : Completion4
    {
        internal SymbolKind Kind { get; }
        internal ColoredCompletion(SymbolKind kind,string display,string insert,string description,ImageMoniker icon,string automation,IEnumerable<CompletionIcon2> attributes,string suffix)
            : base(display,insert,description,icon,automation,attributes,suffix) { Kind=kind; }
    }
    [Export(typeof(IUIElementProvider<Completion,ICompletionSession>))]
    [Name("CodeAssist Completion Colors")]
    [ContentType("C/C++")]
    [Order(Before="default")]
    internal sealed class CompletionColorProvider : IUIElementProvider<Completion,ICompletionSession>
    {
        private static readonly HashSet<UIElementType> observed = new HashSet<UIElementType>();
        public UIElement GetUIElement(Completion item,ICompletionSession session,UIElementType elementType)
        {
            if (!(item is ColoredCompletion colored) || (!StartupUi.Preferences.ColoringEnabled || StartupUi.Preferences.CompletionUseVisualStudioColors)) return null;
            if(observed.Add(elementType)) WelcomeDocumentWindow.RecordEvent("CompletionColorProvider " + elementType);
            var preferences=StartupUi.Preferences;
            var color=colored.Kind==SymbolKind.Function?preferences.FunctionColor:
                colored.Kind==SymbolKind.Type?preferences.TypeColor:
                colored.Kind==SymbolKind.Variable?preferences.VariableColor:
                colored.Kind==SymbolKind.Macro||colored.Kind==SymbolKind.Specifier?preferences.MacroColor:null;
            if(color==null)return null;
            Brush brush;
            try { brush=(Brush)new BrushConverter().ConvertFromString(color); }
            catch(System.FormatException) { return null; }
            var text=new TextBlock { Text=elementType==UIElementType.Tooltip?item.Description:item.DisplayText, Foreground=brush, TextWrapping=TextWrapping.NoWrap };
            System.Windows.Automation.AutomationProperties.SetName(text,text.Text);
            return text;
        }
    }
    // Work only on this session's public WPF popup surface; never scan editor windows.
    internal sealed class CompletionPopupColors
    {
        private readonly ICompletionSession session;
        private IPopupIntellisensePresenter presenter;
        private FrameworkElement surface;
        private readonly CompletionForegroundOverrides foregrounds=new CompletionForegroundOverrides();
        private bool painting;
        private Dictionary<string,Brush> colorCache;
        private readonly Dictionary<string,Brush> brushes = new Dictionary<string,Brush>();
        private CompletionPopupColors(ICompletionSession session)
        {
            this.session=session;
            session.PresenterChanged+=PresenterChanged;
            session.Dismissed+=Dismissed;
            session.Recalculated+=Recalculated;
            StartupUi.SettingsApplied+=SettingsChanged;
            Attach();
        }
        internal static void Connect(ICompletionSession session)
        { session.Properties.GetOrCreateSingletonProperty(()=>new CompletionPopupColors(session)); }
        private void PresenterChanged(object sender,EventArgs args) { Attach(); }
        private void SurfaceChanged(object sender,EventArgs args) { AttachSurface(); }
        private void SettingsChanged() { brushes.Clear(); colorCache=null; Paint(); }
        private void Recalculated(object sender,EventArgs args) { colorCache=null; Paint(); }
        private void LayoutChanged(object sender,EventArgs args) { Paint(); }
        private void Attach()
        {
            if(presenter!=null)presenter.SurfaceElementChanged-=SurfaceChanged;
            presenter=session.Presenter as IPopupIntellisensePresenter;
            if(presenter!=null)presenter.SurfaceElementChanged+=SurfaceChanged;
            AttachSurface();
        }
        private void AttachSurface()
        {
            if(surface!=null)surface.LayoutUpdated-=LayoutChanged;
            surface=presenter?.SurfaceElement as FrameworkElement;
            if(surface!=null){surface.LayoutUpdated+=LayoutChanged;Paint();}
        }
        private void Dismissed(object sender,EventArgs args)
        {
            session.PresenterChanged-=PresenterChanged;session.Dismissed-=Dismissed;session.Recalculated-=Recalculated;
            StartupUi.SettingsApplied-=SettingsChanged;
            if(presenter!=null)presenter.SurfaceElementChanged-=SurfaceChanged;
            if(surface!=null)surface.LayoutUpdated-=LayoutChanged;
            surface=null;presenter=null;brushes.Clear();foregrounds.Clear();
        }
        private Brush Color(SymbolKind kind)
        {
            var p=StartupUi.Preferences;
            var value=kind==SymbolKind.Function?p.FunctionColor:kind==SymbolKind.Type?p.TypeColor:
                kind==SymbolKind.Variable?p.VariableColor:kind==SymbolKind.Macro||kind==SymbolKind.Specifier?p.MacroColor:null;
            if(value==null)return null;
            if(!brushes.TryGetValue(value,out var brush))
            {try{brush=(Brush)new BrushConverter().ConvertFromString(value);brush.Freeze();brushes[value]=brush;}catch(FormatException){return null;}}
            return brush;
        }
        private void Paint()
        {
            if(painting||surface==null||session.IsDismissed)return;
            var set=session.SelectedCompletionSet;
            if(set==null||set.Moniker!="CodeAssistPreview")return;
            painting=true;
            try
            {
                var colors=colorCache ?? (colorCache=set.Completions.OfType<ColoredCompletion>().GroupBy(c=>c.DisplayText).ToDictionary(g=>g.Key,g=>Color(g.First().Kind)));
                var pending=new Stack<DependencyObject>();pending.Push(surface);
                var count=0;
                while(pending.Count>0&&count++<2048)
                {
                    var node=pending.Pop();
                    if(node is TextBlock text&&colors.TryGetValue(text.Text,out var brush)&&brush!=null)
                    {
                        var enabled=StartupUi.Preferences.ColoringEnabled && !StartupUi.Preferences.CompletionUseVisualStudioColors;
                        foregrounds.Apply(text,TextBlock.ForegroundProperty,brush,enabled);
                        foreach(var inline in text.Inlines)foregrounds.Apply(inline,TextElement.ForegroundProperty,brush,enabled);
                    }
                    for(var i=VisualTreeHelper.GetChildrenCount(node)-1;i>=0;i--)pending.Push(VisualTreeHelper.GetChild(node,i));
                }
            }
            finally {painting=false;}
        }
    }

    internal sealed class CompletionForegroundOverrides
    {
        private sealed class OriginalForeground { internal object Value; internal BindingBase Binding; }
        private readonly Dictionary<DependencyObject,OriginalForeground> originals=new Dictionary<DependencyObject,OriginalForeground>();
        internal void Clear(){originals.Clear();}
        internal void Apply(DependencyObject node,DependencyProperty property,Brush brush,bool enabled)
        {
            if(enabled)
            {
                if(!originals.ContainsKey(node))originals[node]=new OriginalForeground {Value=node.ReadLocalValue(property),Binding=BindingOperations.GetBindingBase(node,property)};
                if(!ReferenceEquals(node.GetValue(property),brush))node.SetCurrentValue(property,brush);
            }
            else if(originals.TryGetValue(node,out var original))
            {
                if(original.Binding!=null)BindingOperations.SetBinding(node,property,original.Binding);
                else if(original.Value==DependencyProperty.UnsetValue)node.ClearValue(property);
                else if(original.Value is Brush)node.SetValue(property,original.Value);
                else node.InvalidateProperty(property);
                originals.Remove(node);
            }
        }
    }

}