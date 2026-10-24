using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CodeAssist.Core;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.Extension
{
    [Export(typeof(IIntellisensePresenterProvider))]
    [Name("CodeAssist Completion Filters")]
    [ContentType("C/C++")]
    [Order(Before="default")]
    internal sealed class CompletionFilterPresenterProvider:IIntellisensePresenterProvider
    {
        [ImportMany] internal IEnumerable<Lazy<IIntellisensePresenterProvider>> Providers=null;
        public IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session)
        {
            if(!(session is ICompletionSession completion)||!(completion.SelectedCompletionSet is PreviewCompletionSet))return null;
            foreach(var provider in Providers)
            {
                var target=provider.Value;if(ReferenceEquals(target,this))continue;
                var presenter=target.TryCreateIntellisensePresenter(session);
                if(presenter is IPopupIntellisensePresenter popup)return new CompletionFilterPresenter(completion,popup);
                if(presenter!=null)return presenter;
            }
            return null;
        }
    }
    internal sealed class CompletionFilterPresenter:IPopupIntellisensePresenter,IIntellisenseCommandTarget
    {
        private readonly ICompletionSession completion;
        private readonly IPopupIntellisensePresenter inner;
        private readonly DockPanel root=new DockPanel();
        private readonly StackPanel footer=new StackPanel {Orientation=Orientation.Horizontal,Height=23,Margin=new Thickness(0,6,0,0),HorizontalAlignment=HorizontalAlignment.Left};
        private readonly List<ToggleButton> buttons=new List<ToggleButton>();
        private UIElement content;
        private readonly List<SymbolKind?> kinds=new List<SymbolKind?>();
        private int rangeAnchor=-1;
        public CompletionFilterPresenter(ICompletionSession completion,IPopupIntellisensePresenter inner)
        {
            this.completion=completion;this.inner=inner;
            ProductTheme.Apply(root);ProductTheme.BrushFor(footer,Panel.BackgroundProperty,"Palette");
            DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
            AddFilter("List non-inherited entries first",null,KnownMonikers.Upload);
            AddFilter("Show fields",SymbolKind.Variable,CompletionIcons.For(SymbolKind.Variable));
            AddFilter("Show methods",SymbolKind.Function,CompletionIcons.For(SymbolKind.Function));
            inner.SurfaceElementChanged+=InnerSurfaceChanged;
            completion.Recalculated+=Recalculated;completion.Dismissed+=Dismissed;
            RefreshSurface();
            WelcomeDocumentWindow.RecordEvent("CompletionFilterPresenter attached");
        }
        private void AddFilter(string label,SymbolKind? kind,Microsoft.VisualStudio.Imaging.Interop.ImageMoniker icon)
        {
            var title=UiText.Get(label,StartupUi.Preferences.UiLanguage);
            var button=new ToggleButton {Width=23,Height=23,Padding=new Thickness(3),BorderThickness=new Thickness(0),Background=Brushes.Transparent,Focusable=false,IsTabStop=false,ToolTip=title,IsChecked=kind==null&&StartupUi.Preferences.ListNonInheritedMembersFirst,Content=new CrispImage {Width=16,Height=16,Moniker=icon}};
            var template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ToggleButton'><Border x:Name='Frame' Background='{TemplateBinding Background}' BorderBrush='Transparent' BorderThickness='1'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='#73B6EA'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='#73B6EA'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
            button.Template=template;
            if(kind==null){var drawing=new DrawingGroup();using(var context=drawing.Open()){var pen=new Pen(new SolidColorBrush(Color.FromRgb(90,180,235)),1.5);context.DrawGeometry(null,pen,Geometry.Parse("M3,7 L8,2 L13,7 M8,2 L8,14 L13,14"));}drawing.Freeze();button.Content=new Image {Width=16,Height=16,Source=new DrawingImage(drawing)};}
            System.Windows.Automation.AutomationProperties.SetName(button,title);
            button.Click+=(s,e)=>{
                var index=buttons.IndexOf(button);
                if(kind==null){StartupUi.Preferences.ListNonInheritedMembersFirst=button.IsChecked==true;StartupUi.Save();}
                else
                {
                    var modifiers=System.Windows.Input.Keyboard.Modifiers;
                    // ToggleButton has already toggled the clicked control; reconstruct its prior state.
                    var prior=buttons.Skip(1).Select(value=>value.IsChecked==true).ToArray();
                    prior[index-1]=!prior[index-1];
                    var selected=CompletionFilterSelection.Toggle(prior,index-1,rangeAnchor-1,
                        (modifiers&System.Windows.Input.ModifierKeys.Control)!=0,
                        (modifiers&System.Windows.Input.ModifierKeys.Shift)!=0);
                    for(var i=0;i<selected.Length;i++)buttons[i+1].IsChecked=selected[i];
                    rangeAnchor=index;
                }
                ApplyFilter();
            };
            buttons.Add(button);kinds.Add(kind);footer.Children.Add(button);
        }
        private void RefreshSurface()
        {
            if(content!=null)root.Children.Remove(content);
            content=inner.SurfaceElement;
            if(content!=null)root.Children.Add(content);
        }
        private void InnerSurfaceChanged(object sender,EventArgs e){RefreshSurface();SurfaceElementChanged?.Invoke(this,e);}
        private void Recalculated(object sender,EventArgs e){ApplyFilter();}
        private void ApplyFilter()
        {
            try {
                var selected=kinds.Where((kind,i)=>kind.HasValue&&buttons[i].IsChecked==true).Select(kind=>kind.Value).ToArray();
                (completion.SelectedCompletionSet as PreviewCompletionSet)?.SelectKinds(selected,buttons[0].IsChecked==true);
                if(EditorInputTrace.Enabled)WelcomeDocumentWindow.RecordEvent("CompletionFilter selected="+string.Join(",",selected)+" localFirst="+buttons[0].IsChecked+" count="+completion.SelectedCompletionSet?.Completions.Count);
            } catch(InvalidOperationException error){WelcomeDocumentWindow.RecordEvent("CompletionFilter failed="+error.Message);completion.Dismiss();}
              catch(ArgumentException error){WelcomeDocumentWindow.RecordEvent("CompletionFilter failed="+error.Message);completion.Dismiss();}
        }
        private void Dismissed(object sender,EventArgs e)
        {
            inner.SurfaceElementChanged-=InnerSurfaceChanged;completion.Recalculated-=Recalculated;completion.Dismissed-=Dismissed;
            if(content!=null)root.Children.Remove(content);content=null;
        }
        public UIElement SurfaceElement=>root;
        public IIntellisenseSession Session=>completion;
        public ITrackingSpan PresentationSpan=>inner.PresentationSpan;
        public PopupStyles PopupStyles=>inner.PopupStyles;
        public string SpaceReservationManagerName=>inner.SpaceReservationManagerName;
        public double Opacity {get=>inner.Opacity;set{inner.Opacity=value;root.Opacity=value;}}
        public event EventHandler SurfaceElementChanged;
        public event EventHandler PresentationSpanChanged {add=>inner.PresentationSpanChanged+=value;remove=>inner.PresentationSpanChanged-=value;}
        public event EventHandler<ValueChangedEventArgs<PopupStyles>> PopupStylesChanged {add=>inner.PopupStylesChanged+=value;remove=>inner.PopupStylesChanged-=value;}
        public bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command)=>(inner as IIntellisenseCommandTarget)?.ExecuteKeyboardCommand(command)??false;
    }
}
