using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using CodeAssist.Core;
using CodeAssist.Indexing;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CodeAssist.Extension
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("CodeAssist Context Bar")]
    [MarginContainer(PredefinedMarginNames.Top)]
    [ContentType("C/C++")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ContextBarProvider : IWpfTextViewMarginProvider
    {
        [Import] internal ITextDocumentFactoryService Documents=null;
        [Import] internal ProjectIndexService Indexes=null;
        [Import] internal IGlyphService Glyphs=null;
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost host,IWpfTextViewMargin parent)
        {
            Documents.TryGetTextDocument(host.TextView.TextDataModel.DocumentBuffer,out var document);
            return new ContextBarMargin(host.TextView,document?.FilePath,Indexes,Glyphs,"Top");
        }
    }
    [Export(typeof(IWpfTextViewMarginProvider))][Name("CodeAssist Context Bar Bottom")][MarginContainer(PredefinedMarginNames.Bottom)][ContentType("C/C++")][TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class BottomContextBarProvider:IWpfTextViewMarginProvider {
      [Import]internal ITextDocumentFactoryService Documents=null;
      [Import]internal ProjectIndexService Indexes=null;
      [Import]internal IGlyphService Glyphs=null;
      public IWpfTextViewMargin CreateMargin(IWpfTextViewHost host,IWpfTextViewMargin parent){Documents.TryGetTextDocument(host.TextView.TextDataModel.DocumentBuffer,out var document);return new ContextBarMargin(host.TextView,document?.FilePath,Indexes,Glyphs,"Bottom");}
    }
    internal sealed class ContextBarMargin : IWpfTextViewMargin
    {
        private const string Name="CodeAssist Context Bar";
        private readonly string placement;
        private readonly IWpfTextView view;
        private readonly string filePath;
        private ITextSnapshot definitionSnapshot;
        private System.Collections.Generic.IReadOnlyList<IndexedSymbol> documentSymbols;
        private readonly DockPanel panel=new DockPanel { Height=28,LastChildFill=true };
        private readonly ComboBox methods=CreateMethodSelector();
        internal static ComboBox CreateMethodSelector()
        {
            var row=new FrameworkElementFactory(typeof(StackPanel));row.SetValue(StackPanel.OrientationProperty,Orientation.Horizontal);
            var icon=new FrameworkElementFactory(typeof(Image));icon.SetValue(Image.WidthProperty,16d);icon.SetValue(Image.HeightProperty,16d);icon.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,4,0));icon.SetBinding(Image.SourceProperty,new System.Windows.Data.Binding("Glyph"));row.AppendChild(icon);
            var label=new FrameworkElementFactory(typeof(SymbolNameTextBlock));label.SetBinding(SymbolNameTextBlock.NameTextProperty,new System.Windows.Data.Binding("Caption"));label.SetBinding(SymbolNameTextBlock.KindTextProperty,new System.Windows.Data.Binding("InkKindText"));label.SetValue(SymbolNameTextBlock.UseNavigationBarColoringProperty,true);row.AppendChild(label);
            label.Name="MemberCaption";
            var template=new DataTemplate {VisualTree=row};
            var highlight=new DataTrigger {Binding=new System.Windows.Data.Binding("IsHighlighted") {RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor,typeof(ComboBoxItem),1)},Value=true};
            highlight.Setters.Add(new Setter(TextBlock.ForegroundProperty,new System.Windows.Data.Binding("Foreground") {RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor,typeof(ComboBoxItem),1)},"MemberCaption"));template.Triggers.Add(highlight);
            var combo=new ComboBox {Width=300,Margin=new Thickness(6,1,2,1),MaxDropDownHeight=440,ItemTemplate=template};
            combo.SetResourceReference(ProductTheme.ThemeSurfaceProperty,EnvironmentColors.ToolWindowBackgroundBrushKey);
            var itemStyle=new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty,new System.Windows.Data.Binding {Source=combo,Path=new PropertyPath(ProductTheme.ThemeSurfaceProperty),Converter=ProductTheme.Palette,ConverterParameter="MethodSelection"}));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty,new DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));
            itemStyle.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)System.Windows.Markup.XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBoxItem'><Border x:Name='Row' BorderBrush='Transparent' BorderThickness='1' Background='Transparent' Padding='2,0'><ContentPresenter HorizontalAlignment='Left' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Row' Property='Background' Value='{Binding Background,RelativeSource={RelativeSource TemplatedParent}}'/><Setter TargetName='Row' Property='BorderBrush' Value='#888888'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate>")));
            combo.ItemContainerStyle=itemStyle;
            return combo;
        }
        private sealed class MethodMenuItem
        {
            public DocumentMember Target {get;set;}
            public string InkKindText=>Target.Symbol.Kind.ToString();
            public string Caption=>Target.Caption;
            public override string ToString()=>Caption;
            public ImageSource Glyph {get;set;}
        }
        private sealed class MethodForegroundConverter : System.Windows.Data.IValueConverter
        {
            public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)
            {
                var preferences=StartupUi.Preferences;
                if(!preferences.ColoringEnabled||!preferences.ColorNavigationBar||!(value is SymbolKind kind))return DependencyProperty.UnsetValue;
                return RecommendedColors.SymbolBrush(kind)??DependencyProperty.UnsetValue;
            }
            public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();
        }
        private readonly IGlyphService glyphs;
        private bool updatingMethods;
        private int methodRequest;
        internal static bool TryOpenMethodList(IWpfTextView view)
        {
            if(view==null||view.IsClosed||!view.Properties.TryGetProperty(typeof(ContextBarMargin),out ContextBarMargin margin)||!margin.Enabled)return false;
            margin.methods.Focus();margin.methods.IsDropDownOpen=true;return true;
        }
        private readonly Action settingsChanged;
        private readonly ProjectIndexService indexes;
        private readonly TextBlock contextField=new TextBlock {Margin=new Thickness(8,0,12,0),TextTrimming=TextTrimming.CharacterEllipsis};
        private readonly SymbolNameTextBlock definitionField=new SymbolNameTextBlock {UseNavigationBarColoring=true,Margin=new Thickness(0),VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis};
        private readonly System.Windows.Threading.DispatcherTimer refresh=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromMilliseconds(180)};
        private bool disposed;
        private ITextSnapshot methodSnapshot;
        internal ContextBarMargin(IWpfTextView view,string path,ProjectIndexService indexes=null,IGlyphService glyphs=null,string placement="Top")
        {
            this.placement=placement;
            this.view=view;this.filePath=path;this.indexes=indexes;this.glyphs=glyphs;
            if(view.Properties.ContainsProperty(typeof(ContextBarMargin)))view.Properties.RemoveProperty(typeof(ContextBarMargin));view.Properties.AddProperty(typeof(ContextBarMargin),this);ProductTheme.Apply(panel);panel.Height=26;definitionField.SetValue(SymbolNameTextBlock.NormalizeScopesProperty,false);
            refresh.Tick+=RefreshFields;view.Caret.PositionChanged+=CaretChanged;view.TextBuffer.Changed+=BufferChanged;refresh.Start();
            methods.ItemsSource=new[] {new {Caption=UiText.Get("메서드 선택",StartupUi.Preferences.UiLanguage),Glyph=(ImageSource)null}};methods.SelectedIndex=0;
            var go=ProductTheme.Button("Go",()=> {ThreadHelper.ThrowIfNotOnUIThread();StartupUi.Execute(StartupUi.Preferences.GotoButtonAction=="Go to Related"?0x107:0x100);});go.MinWidth=36;go.Padding=new Thickness(6,2,6,2);DockPanel.SetDock(go,Dock.Right);panel.Children.Add(go);
            var corresponding=ProductTheme.Button("⇄",()=> {ThreadHelper.ThrowIfNotOnUIThread();StartupUi.Execute(0x105);});corresponding.Content=ProductTheme.Icon("Corresponding");System.Windows.Automation.AutomationProperties.SetName(corresponding,UiText.Get("대응 파일 (Alt+O)",StartupUi.Preferences.UiLanguage));corresponding.MinWidth=30;corresponding.Padding=new Thickness(6,2,6,2);corresponding.ToolTip=UiText.Get("대응 파일 (Alt+O)",StartupUi.Preferences.UiLanguage);DockPanel.SetDock(corresponding,Dock.Right);panel.Children.Add(corresponding);
            DockPanel.SetDock(methods,Dock.Left);panel.Children.Add(methods);
            var filePath=new DockPanel {Margin=new Thickness(8,0,0,0),VerticalAlignment=VerticalAlignment.Center};
            var pathIcon=ProductTheme.Icon("Forward",14);pathIcon.Margin=new Thickness(0,0,5,0);DockPanel.SetDock(pathIcon,Dock.Left);filePath.Children.Add(pathIcon);
            filePath.ToolTip=path;filePath.Children.Add(definitionField);panel.Children.Add(filePath);
            methods.DropDownOpened+=(s,e)=>ExtensionPackage.Current?.JoinableTaskFactory.RunAsync(RefreshMethodListAsync).FileAndForget("CodeAssist/MethodList");
            void NavigateMethod() {
                if(updatingMethods||disposed||view.IsClosed||!ReferenceEquals(methodSnapshot,view.TextSnapshot)||!(methods.SelectedItem is MethodMenuItem item))return;
                var target=item.Target;
                if(target.NavigationOffset<0||target.NavigationOffset>view.TextSnapshot.Length)return;
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot,target.NavigationOffset));
                if(target.NameStart>=0&&target.NameLength>0&&target.NameStart+target.NameLength<=view.TextSnapshot.Length)
                    view.Selection.Select(new SnapshotSpan(view.TextSnapshot,target.NameStart,target.NameLength),false);
                else view.Selection.Clear();
                view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(view.TextSnapshot,target.NameStart,target.NameLength));view.VisualElement.Focus();
            }
            methods.SelectionChanged+=(s,e)=>NavigateMethod();
            methods.PreviewKeyDown+=(s,e)=> {
                if(e.Key==System.Windows.Input.Key.Return)
                    methods.Dispatcher.BeginInvoke(new Action(NavigateMethod),System.Windows.Threading.DispatcherPriority.Input);
            };
            settingsChanged=()=> {
                if(StartupUi.Preferences.NavigationBarPosition==placement)view.Properties[typeof(ContextBarMargin)]=this;
            panel.Visibility=StartupUi.Preferences.ContextBarEnabled&&StartupUi.Preferences.NavigationBarPosition==placement?Visibility.Visible:Visibility.Collapsed;
                go.Content=UiText.Get("Go",StartupUi.Preferences.UiLanguage);
                corresponding.ToolTip=UiText.Get("대응 파일 (Alt+O)",StartupUi.Preferences.UiLanguage);
                if(methodSnapshot==null){methods.ItemsSource=new[]{new {Caption=UiText.Get("메서드 선택",StartupUi.Preferences.UiLanguage),Glyph=(ImageSource)null}};methods.SelectedIndex=0;}
                if(methods.IsDropDownOpen)ExtensionPackage.Current?.JoinableTaskFactory.RunAsync(RefreshMethodListAsync).FileAndForget("CodeAssist/MethodListSettings");
                refresh.Stop();refresh.Start();
            };
            StartupUi.SettingsApplied+=settingsChanged;settingsChanged();
        }
        private async System.Threading.Tasks.Task RefreshMethodListAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if(disposed||view.IsClosed)return;
                var snapshot=view.TextSnapshot;var request=++methodRequest;
                try {
                    var rows=await System.Threading.Tasks.Task.Run(()=>DocumentMemberMenu.Build(filePath??"",snapshot.GetText(),StartupUi.Preferences.SortNavigationAlphabetically));
                    if(disposed||view.IsClosed||!methods.IsDropDownOpen||request!=methodRequest||!ReferenceEquals(snapshot,view.TextSnapshot))return;
                    updatingMethods=true;
                    try {
                        methodSnapshot=snapshot;
                        var menuItems=rows.Select(item=>new MethodMenuItem {Target=item,Glyph=glyphs?.GetGlyph(item.Symbol.Kind==SymbolKind.Type?StandardGlyphGroup.GlyphGroupClass:StandardGlyphGroup.GlyphGroupMethod,StandardGlyphItem.GlyphItemPublic)}).ToArray();
                        methods.ItemsSource=menuItems;
                        methods.SelectedItem=menuItems.Where(item=>item.Target.NameStart<=view.Caret.Position.BufferPosition.Position).OrderByDescending(item=>item.Target.NameStart).FirstOrDefault();
                    } finally {updatingMethods=false;}
                } catch(Exception error){ActivityLog.LogWarning("CodeAssist Method List",error.ToString());}
        }
        private void CaretChanged(object sender,CaretPositionChangedEventArgs e){refresh.Stop();refresh.Start();}
        private void BufferChanged(object sender,TextContentChangedEventArgs e){refresh.Stop();refresh.Start();}
        private void RefreshFields(object sender,EventArgs e)=>ExtensionPackage.Current?.JoinableTaskFactory.RunAsync(RefreshFieldsAsync).FileAndForget("CodeAssist/ContextFields");
        private async System.Threading.Tasks.Task RefreshFieldsAsync()
        {
            refresh.Stop();if(disposed||view.IsClosed||!StartupUi.Preferences.ContextBarEnabled)return;
            var snapshot=view.TextSnapshot;if(snapshot.Length>512*1024)return;
            var caret=view.Caret.Position.BufferPosition;var line=caret.GetContainingLine();var word=IdentifierParser.At(line.GetText(),caret.Position-line.Start.Position);var text=snapshot.GetText();var position=caret.Position;
            var cached = ReferenceEquals(definitionSnapshot,snapshot) ? documentSymbols : null;
            var result=await System.Threading.Tasks.Task.Run(()=>new {
                Owner=CppExpressionContext.FindEnclosingFunctionOwner(text,position),
                Symbols=cached??PersistentSymbolIndex.ReadDocumentSymbols(filePath??"",text)
            });
            var owner=result.Owner;
            if(disposed||view.IsClosed||!ReferenceEquals(snapshot,view.TextSnapshot)||view.Caret.Position.BufferPosition.Position!=position)return;
            definitionSnapshot=snapshot;documentSymbols=result.Symbols;
            var candidates=result.Symbols.Concat(indexes?.Index.SearchSymbols(word,limit:200)??Array.Empty<IndexedSymbol>());
            var symbol=ContextDefinitionResolver.Resolve(candidates.GroupBy(item=>item.Identity).Select(group=>group.First()),word,filePath,line.LineNumber+1,owner);
            if(symbol!=null&&!string.IsNullOrEmpty(symbol.OwnerType))owner=symbol.OwnerType;
            contextField.Text=UiText.Get("Context: ",StartupUi.Preferences.UiLanguage)+(string.IsNullOrEmpty(owner)?UiText.Get("Global",StartupUi.Preferences.UiLanguage):owner);contextField.ToolTip=contextField.Text;
            definitionField.NameText=(symbol==null?word:string.IsNullOrEmpty(symbol.Signature)?symbol.ValueType+" "+symbol.Name:symbol.ValueType+" "+symbol.Signature).Trim();
            definitionField.KindText=(symbol?.Kind??SymbolKind.Unknown).ToString();definitionField.ToolTip=definitionField.NameText;
            methods.ToolTip=contextField.Text;
            if(!methods.IsDropDownOpen) {
                updatingMethods=true;
                try { methods.ItemsSource=new[]{new {Caption=symbol==null?System.IO.Path.GetFileName(filePath):string.IsNullOrEmpty(symbol.OwnerType)?symbol.Name:symbol.OwnerType+"."+symbol.Name,Glyph=glyphs?.GetGlyph(symbol?.Kind==SymbolKind.Type?StandardGlyphGroup.GlyphGroupClass:StandardGlyphGroup.GlyphGroupMethod,StandardGlyphItem.GlyphItemPublic),InkKindText=(symbol?.Kind??SymbolKind.Unknown).ToString()}};methods.SelectedIndex=0; }
                finally {updatingMethods=false;}
            }
        }
        public FrameworkElement VisualElement=>panel;
        public double MarginSize=>panel.Visibility==Visibility.Visible?26:0;
        public bool Enabled=>!disposed && StartupUi.Preferences.ContextBarEnabled && StartupUi.Preferences.NavigationBarPosition==placement;
        public ITextViewMargin GetTextViewMargin(string name)=>string.Equals(name,Name,StringComparison.OrdinalIgnoreCase)?this:null;
        public void Dispose(){if(disposed)return;disposed=true;++methodRequest;if(view.Properties.TryGetProperty(typeof(ContextBarMargin),out ContextBarMargin current)&&ReferenceEquals(current,this))view.Properties.RemoveProperty(typeof(ContextBarMargin));refresh.Stop();refresh.Tick-=RefreshFields;view.Caret.PositionChanged-=CaretChanged;view.TextBuffer.Changed-=BufferChanged;StartupUi.SettingsApplied-=settingsChanged;}
    }
}