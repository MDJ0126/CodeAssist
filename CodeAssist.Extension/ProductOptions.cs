using CodeAssist.Core;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.VisualStudio.Shell;

namespace CodeAssist.Extension
{
    internal static class ProductTheme
    {
        [ThreadStatic] private static ControlTemplate comboTemplate;
        [ThreadStatic] private static ControlTemplate scrollTemplate;
        internal static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(255, 211, 70));
        internal static void Apply(FrameworkElement root)
        {
            root.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.ToolWindowTextBrushKey);
            root.SetResourceReference(Control.BackgroundProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);
            foreach (var type in new[] { typeof(CheckBox), typeof(RadioButton), typeof(TreeViewItem), typeof(TreeView), typeof(TextBox), typeof(ComboBox), typeof(ComboBoxItem), typeof(ScrollViewer) })
            {
                var style = new Style(type);
                style.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));
                if (type == typeof(ScrollViewer) || type == typeof(TreeView) || type == typeof(TextBox) || type == typeof(ComboBox) || type == typeof(ComboBoxItem))
                    style.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(EnvironmentColors.ToolWindowBackgroundBrushKey)));
                if(type == typeof(ComboBox))
                    style.Setters.Add(new Setter(Control.TemplateProperty, comboTemplate ?? (comboTemplate = (ControlTemplate)XamlReader.Parse(@"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='{x:Type ComboBox}' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
 <Grid MinHeight='26'>
  <Border Background='{TemplateBinding Background}' BorderBrush='#666666' BorderThickness='1' CornerRadius='2'/>
  <ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}'>
   <ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border Background='Transparent'><TextBlock Text='▾' Foreground='#AAAAAA' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,8,0'/></Border></ControlTemplate></ToggleButton.Template>
  </ToggleButton>
  <ContentPresenter x:Name='Selected' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}' ContentTemplateSelector='{TemplateBinding ItemTemplateSelector}' ContentStringFormat='{TemplateBinding SelectionBoxItemStringFormat}' IsHitTestVisible='False' Margin='7,3,26,3' VerticalAlignment='Center' TextElement.Foreground='{TemplateBinding Foreground}'/>
  <TextBox x:Name='PART_EditableTextBox' Visibility='Collapsed' Background='Transparent' Foreground='{TemplateBinding Foreground}' BorderThickness='0' Margin='6,2,26,2' VerticalContentAlignment='Center'/>
  <Popup x:Name='PART_Popup' IsOpen='{Binding IsDropDownOpen,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}' Placement='Bottom' AllowsTransparency='True' Focusable='False'>
   <Border Background='{TemplateBinding Background}' BorderBrush='#666666' BorderThickness='1' MinWidth='{Binding ActualWidth,RelativeSource={RelativeSource TemplatedParent}}'>
    <ScrollViewer MaxHeight='{TemplateBinding MaxDropDownHeight}' CanContentScroll='True'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer>
   </Border>
  </Popup>
 </Grid>
 <ControlTemplate.Triggers><Trigger Property='IsEditable' Value='True'><Setter TargetName='Selected' Property='Visibility' Value='Hidden'/><Setter TargetName='PART_EditableTextBox' Property='Visibility' Value='Visible'/></Trigger></ControlTemplate.Triggers>
</ControlTemplate>"))));
                root.Resources[type] = style;
            }
            var scrollStyle = new Style(typeof(System.Windows.Controls.Primitives.ScrollBar));
            scrollStyle.Setters.Add(new Setter(FrameworkElement.WidthProperty, 13.0));
            scrollStyle.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(EnvironmentColors.ToolWindowBackgroundBrushKey)));
            scrollStyle.Setters.Add(new Setter(Control.TemplateProperty, scrollTemplate ?? (scrollTemplate = (ControlTemplate)XamlReader.Parse(@"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type ScrollBar}'>
 <Border Background='{TemplateBinding Background}'>
  <Track x:Name='PART_Track' IsDirectionReversed='True' Orientation='{TemplateBinding Orientation}' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' ViewportSize='{TemplateBinding ViewportSize}' Value='{Binding Value,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}'>
   <Track.DecreaseRepeatButton><RepeatButton x:Name='Decrease' Command='{x:Static ScrollBar.PageUpCommand}'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
   <Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'><Border x:Name='Grip' Background='#666666' CornerRadius='3' Margin='3,1'/><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Grip' Property='Background' Value='#999999'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
   <Track.IncreaseRepeatButton><RepeatButton x:Name='Increase' Command='{x:Static ScrollBar.PageDownCommand}'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
  </Track>
 </Border>
 <ControlTemplate.Triggers><Trigger Property='Orientation' Value='Horizontal'><Setter Property='Width' Value='Auto'/><Setter Property='Height' Value='13'/><Setter TargetName='PART_Track' Property='IsDirectionReversed' Value='False'/><Setter TargetName='Decrease' Property='Command' Value='{x:Static ScrollBar.PageLeftCommand}'/><Setter TargetName='Increase' Property='Command' Value='{x:Static ScrollBar.PageRightCommand}'/></Trigger></ControlTemplate.Triggers>
</ControlTemplate>"))));
            root.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = scrollStyle;
        }
        internal static TextBlock Text(string value, double size = 13) => new TextBlock
        { Text = UiText.Get(value, StartupUi.Preferences.UiLanguage), FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
        internal static Button Button(string label, Action click, bool primary = false)
        {
            var button = new Button { Content = UiText.Get(label, StartupUi.Preferences.UiLanguage), Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(0, 0, 10, 0), MinWidth = 88,
                Foreground = primary ? Brushes.Black : Brushes.White,
                Background = primary ? Accent : new SolidColorBrush(Color.FromRgb(62,62,66)), BorderThickness = new Thickness(0) };
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetBinding(ContentPresenter.MarginProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.AppendChild(presenter);
            button.Template = new ControlTemplate(typeof(Button)) { VisualTree = border };
            button.MouseEnter += (s,e) => button.Opacity = .85;
            button.MouseLeave += (s,e) => button.Opacity = button.IsEnabled ? 1 : .45;
            button.IsEnabledChanged += (s,e) => button.Opacity = button.IsEnabled ? 1 : .45;
            button.Click += (s,e) => click();
            return button;
        }
    }

    internal static class ProductOptions
    {
        private sealed class LanguageChoice { public LanguageChoice(string code,string label){Code=code;Label=label;} public string Code { get; } public string Label { get; } }
        internal static Window Create(bool firstRun)
        {
            var draft = StartupUi.Preferences.Copy();
            var root = new DockPanel { Margin = new Thickness(20) };
            ProductTheme.Apply(root);
            var window = new Window { Title = "CodeAssist Options", Width = 940, Height = 720, MinWidth = 780, MinHeight = 580,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
            var icon = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(ProductOptions).Assembly.Location), "Resources", "CodeAssist.png");
            if(System.IO.File.Exists(icon)) window.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(icon));
            window.WindowStyle = WindowStyle.None;
            System.Windows.Shell.WindowChrome.SetWindowChrome(window, new System.Windows.Shell.WindowChrome {
                CaptionHeight=34, ResizeBorderThickness=new Thickness(6), GlassFrameThickness=new Thickness(0), CornerRadius=new CornerRadius(6) });
            var chrome = new DockPanel();
            ProductTheme.Apply(chrome);
            var caption = new DockPanel { Height=34, LastChildFill=true, Margin=new Thickness(10,0,2,0) };
            var close = ProductTheme.Button("×",()=>window.Close());
            close.Width=40;close.Height=32;close.MinWidth=0;close.Margin=new Thickness(0);close.Padding=new Thickness(0);close.Background=Brushes.Transparent;
            close.SetResourceReference(Control.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);
            System.Windows.Automation.AutomationProperties.SetName(close,UiText.Get("닫기", StartupUi.Preferences.UiLanguage));
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(close,true);
            DockPanel.SetDock(close,Dock.Right);caption.Children.Add(close);
            var captionTitle = new StackPanel { Orientation=Orientation.Horizontal, VerticalAlignment=VerticalAlignment.Center };
            if(window.Icon != null) captionTitle.Children.Add(new Image { Source=window.Icon,Width=16,Height=16,Margin=new Thickness(0,0,6,0) });
            captionTitle.Children.Add(new TextBlock { Text="CodeAssist Options",FontSize=12,VerticalAlignment=VerticalAlignment.Center });
            caption.Children.Add(captionTitle);
            DockPanel.SetDock(caption,Dock.Top);chrome.Children.Add(caption);chrome.Children.Add(root);
            window.Content=new Border { BorderBrush=Brushes.DimGray,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(6),Child=chrome };
            window.SetResourceReference(Control.BackgroundProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);
            window.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.ToolWindowTextBrushKey);
            var footer = new DockPanel { Margin = new Thickness(230, 16, 0, 0) };
            DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
            var status = ProductTheme.Text(UiText.Get("설정은 저장할 때 적용됩니다.", StartupUi.Preferences.UiLanguage), 12);
            var footerButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            DockPanel.SetDock(footerButtons, Dock.Right); footer.Children.Add(footerButtons); footer.Children.Add(status);
            var errors = new HashSet<string>();
            var save = ProductTheme.Button(UiText.Get("저장", StartupUi.Preferences.UiLanguage), () => { ThreadHelper.ThrowIfNotOnUIThread(); draft.SetupCompleted = true; if(StartupUi.ApplyPreferences(draft)) window.DialogResult = true; }, true);
            save.IsDefault=true;
            var cancel=ProductTheme.Button(UiText.Get("취소", StartupUi.Preferences.UiLanguage), () => window.DialogResult = false);cancel.IsCancel=true;
            footerButtons.Children.Add(save); footerButtons.Children.Add(cancel);
            var sidebar = new DockPanel { Width = 210, Margin = new Thickness(0, 0, 26, 0) };
            DockPanel.SetDock(sidebar, Dock.Left); root.Children.Add(sidebar);
            var search = new TextBox { Height = 30, Padding = new Thickness(7,4,7,4), Margin = new Thickness(0, 0, 0, 12), ToolTip = UiText.Get("설정 검색", StartupUi.Preferences.UiLanguage) };
            System.Windows.Automation.AutomationProperties.SetName(search, UiText.Get("설정 검색", StartupUi.Preferences.UiLanguage));
            var searchBox = new Grid();
            searchBox.Children.Add(search);
            var placeholder = new TextBlock { Text=UiText.Get("설정 검색…  ⌕", StartupUi.Preferences.UiLanguage), Foreground=Brushes.Gray, Margin=new Thickness(8,6,6,12), IsHitTestVisible=false };
            searchBox.Children.Add(placeholder);
            search.TextChanged += (sender,args) => placeholder.Visibility=search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;
            DockPanel.SetDock(searchBox, Dock.Top); sidebar.Children.Add(searchBox);
            var tree = new TreeView { BorderThickness = new Thickness(0), Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch };
            sidebar.Children.Add(tree);
            var content = new StackPanel { Margin = new Thickness(0, 4, 8, 0) };
            root.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            var pages = new Dictionary<string, Action>();
            CheckBox Toggle(string title, bool value, Action<bool> set)
            {
                var control = new CheckBox { Content = title, IsChecked = value, Margin = new Thickness(0, 0, 0, 14) };
                control.Click += (s,e) => set(control.IsChecked == true); content.Children.Add(control); return control;
            }
            void Heading(string section, string page)
            {
                content.Children.Clear();
                var title = ProductTheme.Text("CodeAssist  |  " + UiText.Get(section, StartupUi.Preferences.UiLanguage) + (page == section ? "" : " / " + UiText.Get(page, StartupUi.Preferences.UiLanguage)), 22);
                title.FontWeight = FontWeights.SemiBold; content.Children.Add(title);
                content.Children.Add(new Border { Height = 1, Background = Brushes.DimGray, Margin = new Thickness(0, 0, 0, 22) });
            }
            void Number(string label, int value, int minimum, int maximum, Action<int> set)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,14) };
                row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,12,0) });
                var number = new ComboBox { Width = 92, ItemsSource = Enumerable.Range(minimum, maximum-minimum+1).ToArray(), SelectedItem = value, MaxDropDownHeight = 240 };
                number.SelectionChanged += (s,e) => { if(number.SelectedItem is int n) set(n); }; row.Children.Add(number); content.Children.Add(row);
            }
            void ColorChoice(string label, string value, Action<string> set)
            {
                var row = new DockPanel { Margin = new Thickness(0,0,0,12) };
                var values = new[] { value, "#FFD700", "#4EC9B0", "#BDB76B", "#9CDCFE", "#FF8000", "#DCDCAA", "#BD63C5", "#C586C0", "#D4D4D4" }.Distinct().ToArray();
                var picker = new ComboBox { Tag = label, Width = 132, ItemsSource = values, SelectedItem = value, IsEditable = true, Margin = new Thickness(0,0,12,0) };
                var swatch = new Border { Width = 22, Height = 22, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(0,0,12,0), Background = (Brush)new BrushConverter().ConvertFromString(value) };
                DockPanel.SetDock(picker, Dock.Left); DockPanel.SetDock(swatch, Dock.Left); row.Children.Add(picker); row.Children.Add(swatch);
                row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center }); content.Children.Add(row);
                picker.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((s,e) =>
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(picker.Text ?? "", "^#[0-9A-Fa-f]{6}$"))
                    { errors.Remove(label); set(picker.Text); swatch.Background = (Brush)new BrushConverter().ConvertFromString(picker.Text); }
                    else errors.Add(label);
                    save.IsEnabled = errors.Count == 0;
                    status.Text = errors.Count == 0 ? UiText.Get("설정은 저장할 때 적용됩니다.", StartupUi.Preferences.UiLanguage) : UiText.Get("색상은 #RRGGBB 형식으로 입력하세요.", StartupUi.Preferences.UiLanguage);
                }));
            }
            void Unavailable(string description)
            {
                content.Children.Add(ProductTheme.Text(description));
                content.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(30,255,211,70)), Padding = new Thickness(12),
                    Child = ProductTheme.Text(UiText.Get("이 범주는 현재 지원하지 않습니다. 제공되는 기능은 관련 설정에서 사용할 수 있습니다.", StartupUi.Preferences.UiLanguage)) });
            }
            void Page(TreeViewItem parent, string section, string title, Action body)
            {
                var item = new TreeViewItem { Header = UiText.Get(title, StartupUi.Preferences.UiLanguage), Tag = section + " / " + title, Padding = new Thickness(4,5,0,5) };
                parent.Items.Add(item); pages[(string)item.Tag] = () => { Heading(section,title); body(); };
            }
            TreeViewItem Group(string name, Action body)
            {
                var item = new TreeViewItem { Header = UiText.Get(name, StartupUi.Preferences.UiLanguage), Tag = name, IsExpanded = true, Padding = new Thickness(3,5,0,5) };
                tree.Items.Add(item); pages[name] = () => { Heading(name,name); body(); }; return item;
            }
            var editor = Group("Editor", () => {
                Toggle(UiText.Get("자동 완성 사용", StartupUi.Preferences.UiLanguage), draft.CompletionEnabled, v => draft.CompletionEnabled=v);
                Toggle(UiText.Get("심볼 색상 표시", StartupUi.Preferences.UiLanguage), draft.ColoringEnabled, v => draft.ColoringEnabled=v);
                Number(UiText.Get("클립보드 기록 항목 수", StartupUi.Preferences.UiLanguage), draft.ClipboardLimit, 1, 50, v => draft.ClipboardLimit=v);
                content.Children.Add(ProductTheme.Text(UiText.Get("자동 괄호·붙여넣기 서식·대소문자 자동 교정은 현재 VS의 편집기 설정을 따릅니다.", StartupUi.Preferences.UiLanguage)));
            });
            Page(editor,"Editor","Coloring",() => {
                Toggle(UiText.Get("향상된 심볼 색상 사용", StartupUi.Preferences.UiLanguage),draft.ColoringEnabled,v=>draft.ColoringEnabled=v);
                Toggle(UiText.Get("Visual Studio 기본 추천 색상 사용",StartupUi.Preferences.UiLanguage),draft.CompletionUseVisualStudioColors,v=>draft.CompletionUseVisualStudioColors=v);
                ColorChoice(UiText.Get("클래스와 타입", StartupUi.Preferences.UiLanguage), draft.TypeColor,v=>draft.TypeColor=v);
                ColorChoice(UiText.Get("변수", StartupUi.Preferences.UiLanguage),draft.VariableColor,v=>draft.VariableColor=v);
                ColorChoice(UiText.Get("함수", StartupUi.Preferences.UiLanguage),draft.FunctionColor,v=>draft.FunctionColor=v);
                ColorChoice(UiText.Get("Unreal 매크로", StartupUi.Preferences.UiLanguage),draft.MacroColor,v=>draft.MacroColor=v);
                content.Children.Add(ProductTheme.Button(UiText.Get("추천 팔레트 복원", StartupUi.Preferences.UiLanguage),()=> { draft.TypeColor="#FFD700";draft.VariableColor="#BDB76B";draft.FunctionColor="#FF8000";draft.MacroColor="#BD63C5";pages["Editor / Coloring"](); }));
                content.Children.Add(ProductTheme.Text(UiText.Get("추천 팔레트를 복원하거나 원하는 #RRGGBB 색상을 입력할 수 있습니다.", StartupUi.Preferences.UiLanguage)));
            });
            Page(editor,"Editor","Underlining",()=> {
                Toggle(UiText.Get("파일 내 기본 코드 진단 사용", StartupUi.Preferences.UiLanguage),draft.InspectionMode!=InspectionMode.Disabled,v=>draft.InspectionMode=v?InspectionMode.PerFile:InspectionMode.Disabled);
                content.Children.Add(ProductTheme.Text(UiText.Get("확정 구문·스코프 오류 후보를 표시합니다. 컴파일러 전체 진단과 동일하지 않습니다.", StartupUi.Preferences.UiLanguage)));
            });
            Page(editor,"Editor","Highlighting",()=> { content.Children.Add(ProductTheme.Text(UiText.Get("Find Selected: Alt+K · 다음/이전 선택 항목 이동은 기존 명령을 사용합니다.", StartupUi.Preferences.UiLanguage))); content.Children.Add(ProductTheme.Button(UiText.Get("단축키 도움말", StartupUi.Preferences.UiLanguage),()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp("shortcuts"); })); });
            Page(editor,"Editor","Floating Elements",()=>Unavailable(UiText.Get("빠른 작업은 Alt+Shift+Q로 엽니다. 부동 빠른 작업 툴바는 현재 지원하지 않습니다.", StartupUi.Preferences.UiLanguage)));
            Page(editor,"Editor","Smart Select",()=> {
                Toggle(UiText.Get("Smart Select 사용", StartupUi.Preferences.UiLanguage),draft.SmartSelectEnabled,v=>draft.SmartSelectEnabled=v);
                Toggle(UiText.Get("대문자와 밑줄 경계에서 단어 나누기", StartupUi.Preferences.UiLanguage),draft.SmartSelectSplitWords,v=>draft.SmartSelectSplitWords=v);
                Toggle(UiText.Get("줄 선택 전에 앞뒤 공백을 제외한 범위 선택", StartupUi.Preferences.UiLanguage),draft.SmartSelectTrimWhitespace,v=>draft.SmartSelectTrimWhitespace=v);
                content.Children.Add(ProductTheme.Text(UiText.Get("선택 확장 Alt+Shift+] · 축소 Alt+Shift+[ · 블록 확장 Alt+] · 축소 Alt+[.", StartupUi.Preferences.UiLanguage)));
            });
            Page(editor,"Editor","Display",()=> { Toggle(UiText.Get("편집기 상단 Context Bar 표시", StartupUi.Preferences.UiLanguage),draft.ContextBarEnabled,v=>draft.ContextBarEnabled=v); content.Children.Add(ProductTheme.Text(UiText.Get("파일 경로·메서드 이동·관련 파일 명령을 편집기 위에 표시합니다.", StartupUi.Preferences.UiLanguage))); });
            Page(editor,"Editor","Mouse",()=>Unavailable(UiText.Get("기본 마우스 동작은 Visual Studio 설정을 사용합니다.", StartupUi.Preferences.UiLanguage)));
            var navigation = Group("Navigation",()=>content.Children.Add(ProductTheme.Text(UiText.Get("Alt+G 정의/구현 · Alt+O 대응 파일 · Alt+Shift+S 심볼 · Alt+Shift+O 파일.", StartupUi.Preferences.UiLanguage))));
            Page(navigation,"Navigation","Goto",()=>content.Children.Add(ProductTheme.Text(UiText.Get("명령과 단축키는 설치된 CodeAssist 명령 테이블을 사용합니다. 기존 키보드 설정을 덮어쓰지 않습니다.", StartupUi.Preferences.UiLanguage))));
            Page(navigation,"Navigation","Source Links",()=>Unavailable(UiText.Get("외부 소스 링크 규칙 편집기는 미구현입니다.", StartupUi.Preferences.UiLanguage)));
            Page(navigation,"Navigation","CodeAssist Hashtags",()=>content.Children.Add(ProductTheme.Text(UiText.Get("주석의 #태그를 Alt+Shift+H로 검색합니다.", StartupUi.Preferences.UiLanguage))));
            var suggestions = Group("Suggestions",()=>Toggle(UiText.Get("CodeAssist 자동 완성", StartupUi.Preferences.UiLanguage),draft.CompletionEnabled,v=>draft.CompletionEnabled=v));
            Page(suggestions,"Suggestions","Enhanced Listboxes",()=>content.Children.Add(ProductTheme.Text(UiText.Get("C/C++ 완성 후보와 clangd 기반 후보를 결합합니다. 표시 결과는 프로젝트 구성과 인덱스 상태에 영향을 받습니다.", StartupUi.Preferences.UiLanguage))));
            var projects = Group("Projects",()=>content.Children.Add(ProductTheme.Text(UiText.Get("범용 C/C++ 프로젝트를 지원하며 Unreal은 추가 지원 대상입니다.", StartupUi.Preferences.UiLanguage))));
            Page(projects,"Projects","Projects and Files",()=>content.Children.Add(ProductTheme.Text(UiText.Get("현재 프로젝트의 파일과 심볼을 인덱싱합니다. 엔진·시스템 헤더 캐시는 프로젝트 심볼과 분리합니다.", StartupUi.Preferences.UiLanguage))));
            Page(projects,"Projects","C/C++ Directories",()=>content.Children.Add(ProductTheme.Text(UiText.Get("포함 경로는 프로젝트 컴파일 구성에서 읽습니다. 사용자 경로 편집은 아직 제공하지 않습니다.", StartupUi.Preferences.UiLanguage))));
            Group("Code Generation",()=>content.Children.Add(ProductTheme.Text(UiText.Get("선언 만들기 Alt+Shift+C · 스니펫 삽입은 CodeAssist 메뉴에서 실행합니다.", StartupUi.Preferences.UiLanguage))));
            var inspection = Group("Code Inspection",()=> {
                foreach(var mode in new[] { InspectionMode.Ask,InspectionMode.Project,InspectionMode.PerFile,InspectionMode.Disabled }) {
                    var captured=mode;
                    var radio=new RadioButton { Content=mode==InspectionMode.Project?UiText.Get("프로젝트 전체 기본 검사", StartupUi.Preferences.UiLanguage):mode==InspectionMode.PerFile?UiText.Get("파일별 검사", StartupUi.Preferences.UiLanguage):mode==InspectionMode.Disabled?UiText.Get("검사 사용 안 함", StartupUi.Preferences.UiLanguage):UiText.Get("상단 안내에서 선택", StartupUi.Preferences.UiLanguage), IsChecked=draft.InspectionMode==mode, Margin=new Thickness(0,0,0,12) };
                    radio.Click+=(s,e)=>draft.InspectionMode=captured;content.Children.Add(radio);
                }
                content.Children.Add(ProductTheme.Text(UiText.Get("프로젝트 전체 검사는 로컬 프로젝트 C/C++ 파일의 확정 구문 오류 후보를 검사해 오류 목록에 표시합니다. 컴파일러의 전체 진단을 대체하지 않습니다.", StartupUi.Preferences.UiLanguage)));
            });
            Page(inspection,"Code Inspection","Project-Wide",()=>pages["Code Inspection"]());
            Page(inspection,"Code Inspection","Per-File",()=> { Toggle(UiText.Get("파일별 검사 활성화", StartupUi.Preferences.UiLanguage),draft.InspectionMode!=InspectionMode.Disabled,v=>draft.InspectionMode=v?InspectionMode.PerFile:InspectionMode.Disabled); });
            Page(inspection,"Code Inspection","Checker Sets",()=>content.Children.Add(ProductTheme.Text(UiText.Get("프로젝트 전체: 괄호·제어문 구문 검사. 파일별: 타입 대소문자·식별자·구문·스코프 후보 검사. 추가 검사기는 미구현입니다.", StartupUi.Preferences.UiLanguage))));
            Group("CodeAssist Intelligence",()=>Unavailable(UiText.Get("로컬 AI·에이전트 연결은 현재 구현하지 않았습니다.", StartupUi.Preferences.UiLanguage)));
            Group("Debug Assistance",()=>Unavailable(UiText.Get("전용 디버그 보조 기능은 현재 지원하지 않습니다.", StartupUi.Preferences.UiLanguage)));
            Group("Games & GPUs",()=>content.Children.Add(ProductTheme.Text(UiText.Get("Unreal 반사 매크로와 메타 지정자·엔진 심볼을 지원합니다. GPU 전용 분석기는 미구현입니다.", StartupUi.Preferences.UiLanguage))));
            var advanced = Group("Advanced",()=> {
                Toggle(UiText.Get("시작할 때 Welcome 문서 열기", StartupUi.Preferences.UiLanguage),draft.ShowWelcomeOnStartup,v=>draft.ShowWelcomeOnStartup=v);
                Toggle(UiText.Get("상단 안내바 표시", StartupUi.Preferences.UiLanguage),!draft.HideWelcomeBar,v=>draft.HideWelcomeBar=!v);
                content.Children.Add(ProductTheme.Button(UiText.Get("CodeAssist 문서", StartupUi.Preferences.UiLanguage),()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp("startup"); }));
            });
            Page(advanced,"Advanced","Language",()=> {
                content.Children.Add(ProductTheme.Text("UI 언어"));
                var choices = new[] { new LanguageChoice("auto",UiText.Get("시스템 언어",StartupUi.Preferences.UiLanguage)) }.Concat(UiText.LanguageCodes.Select(code=>new LanguageChoice(code,UiText.LanguageName(code)))).ToArray();
                var language = new ComboBox { ItemsSource=choices, DisplayMemberPath="Label", SelectedValuePath="Code", SelectedValue=draft.UiLanguage, Width=220, HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,0,0,14) };
                language.SelectionChanged+=(s,e)=>{if(language.SelectedValue is string code)draft.UiLanguage=code;};
                content.Children.Add(language);
                content.Children.Add(ProductTheme.Text("언어는 저장 후 새로 여는 화면에 적용됩니다."));
            });
            Page(advanced,"Advanced","Performance",()=> {
                var delays = new ComboBox { ItemsSource = new[] {50,100,150,250,500,1000,draft.LiveUpdateDelay}.Distinct().OrderBy(v=>v).ToArray(), SelectedItem=draft.LiveUpdateDelay, Width=120, HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,0,0,12) };
                content.Children.Add(ProductTheme.Text(UiText.Get("편집 후 문서 인덱스 갱신 대기 (ms)", StartupUi.Preferences.UiLanguage)));
                delays.SelectionChanged+=(s,e)=>{if(delays.SelectedItem is int v) draft.LiveUpdateDelay=v;};content.Children.Add(delays);
                content.Children.Add(ProductTheme.Text(UiText.Get("빠른 입력 중 중복 분석을 합칩니다. 초기 인덱싱 시간은 프로젝트 구성에 따라 다릅니다.", StartupUi.Preferences.UiLanguage)));
            });
            tree.SelectedItemChanged += (s,e) => { if(e.NewValue is TreeViewItem item && item.Tag is string key && pages.TryGetValue(key,out var page)) page(); };
            search.TextChanged += (s,e) => {
                var query=search.Text.Trim();
                var aliases = new Dictionary<string,string> {
                    {"Editor","자동 완성 completion clipboard 클립보드"},
                    {"Editor / Coloring","색상 클래스 변수 함수 매크로 palette color"},
                    {"Editor / Underlining","밑줄 진단 diagnostic"},
                    {"Editor / Display","표시 context bar 파일 경로"},
                    {"Code Inspection","검사 진단 프로젝트 전체 파일별"},
                    {"Advanced","시작 welcome 안내 도움말"},
                    {"Advanced / Performance","성능 속도 갱신 대기 delay cpu"}
                };
                bool Matches(TreeViewItem item) => ((string)item.Header).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0 || ((string)item.Tag).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0 ||
                    (aliases.TryGetValue((string)item.Tag,out var terms) && terms.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0);

                foreach(TreeViewItem group in tree.Items) {
                    var parentMatch=Matches(group);
                    bool visible=parentMatch;
                    foreach(TreeViewItem child in group.Items) { var match=parentMatch || Matches(child);child.Visibility=match?Visibility.Visible:Visibility.Collapsed;visible|=match; }
                    group.Visibility=visible?Visibility.Visible:Visibility.Collapsed; if(query.Length>0)group.IsExpanded=true;
                }
            };
            editor.IsSelected=true;
            if(firstRun) status.Text=UiText.Get("CodeAssist에 오신 것을 환영합니다. 권장 설정을 확인하고 저장하세요.", StartupUi.Preferences.UiLanguage);
            return window;
        }
    }
}