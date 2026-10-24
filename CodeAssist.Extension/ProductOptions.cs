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
using System.Windows.Data;
using System.Globalization;

namespace CodeAssist.Extension
{
    internal static class ProductTheme
    {
        [ThreadStatic] private static ControlTemplate comboTemplate;
        [ThreadStatic] private static ControlTemplate scrollTemplate;
        internal static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(255, 211, 70));
        // DynamicResource supplies the IDE background; the binding recalculates on theme changes.
        internal static readonly DependencyProperty ThemeSurfaceProperty = DependencyProperty.RegisterAttached("ThemeSurface", typeof(Brush), typeof(ProductTheme));
        internal static readonly ThemeBrushConverter Palette = new ThemeBrushConverter();
        internal static void BrushFor(FrameworkElement element, DependencyProperty property, string role)
        {
            element.SetResourceReference(ThemeSurfaceProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);
            BindingOperations.SetBinding(element, property, new Binding { Source=element, Path=new PropertyPath(ThemeSurfaceProperty), Converter=Palette, ConverterParameter=role });
        }
        internal sealed class ThemeBrushConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var color=(value as SolidColorBrush)?.Color ?? Colors.Black;
                var dark=(color.R*299+color.G*587+color.B*114)<128000;
                string hex;
                switch(parameter as string)
                {
                    case "Palette": hex=dark?"#2D2D2D":"#F0F0F0"; break;
                    case "ColorEditor": hex=dark?"#333333":"#F0F0F0"; break;
                    case "ColorInput": hex=dark?"#3D3D3D":"#FFFFFF"; break;
                    case "Canvas": hex=dark?"#000000":"#FFFFFF"; break;
                    case "Card": hex=dark?"#10111B":"#F2F2F5"; break;
                    case "Muted": hex=dark?"#B9BFCC":"#50505A"; break;
                    case "Accent": hex=dark?"#FFD346":"#735500"; break;
                    case "Link": hex=dark?"#73C8FF":"#0065A9"; break;
                    case "Type": hex=dark?"#FFD700":"#735500"; break;
                    case "Function": hex=dark?"#FF8000":"#984000"; break;
                    case "Variable": hex=dark?"#BDB76B":"#526000"; break;
                    case "Macro": hex=dark?"#BD63C5":"#77358B"; break;
                    case "Primary": hex="#403582"; break;
                    case "Selection": hex=dark?"#3D3D3D":"#DCE9F8"; break;
                    case "Button": hex=dark?"#3D3D3D":"#E5E5E5"; break;
                    case "MethodSelection": hex=dark?"#3D3D3D":"#FFFFFF"; break;
                    case "MenuSelection": hex=dark?"#3D3D3D":"#C9DDF5"; break;
                    default: hex=dark?"#F1F1F1":"#202020"; break;
                }
                var brush=(SolidColorBrush)new BrushConverter().ConvertFromString(hex);brush.Freeze();return brush;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
        }

        private static readonly DependencyProperty ChromeReadyProperty=DependencyProperty.RegisterAttached("ChromeReady",typeof(bool),typeof(ProductTheme),new PropertyMetadata(false));
        private static void AttachChrome(Window window)
        {
            if(window==null||(bool)window.GetValue(ChromeReadyProperty)||window.WindowStyle==WindowStyle.None||!window.Title.StartsWith("CodeAssist",StringComparison.Ordinal))return;
            window.SetValue(ChromeReadyProperty,true);window.SetResourceReference(Control.BackgroundProperty,EnvironmentColors.ToolWindowBackgroundBrushKey);window.SetResourceReference(Control.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);var content=window.Content as UIElement;if(content==null)return;window.Content=null;
            window.WindowStyle=WindowStyle.None;
            System.Windows.Shell.WindowChrome.SetWindowChrome(window,new System.Windows.Shell.WindowChrome {CaptionHeight=30,ResizeBorderThickness=new Thickness(5),GlassFrameThickness=new Thickness(0),CornerRadius=new CornerRadius(0)});
            var chrome=new DockPanel();chrome.SetResourceReference(Control.BackgroundProperty,EnvironmentColors.ToolWindowBackgroundBrushKey);chrome.SetResourceReference(Control.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);
            var caption=new DockPanel {Height=30,Margin=new Thickness(10,0,0,0)};
            var close=Button("×",()=>window.Close());close.Width=36;close.Height=28;close.MinWidth=0;close.Padding=new Thickness(0);close.Margin=new Thickness(0);close.Background=Brushes.Transparent;System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(close,true);DockPanel.SetDock(close,Dock.Right);caption.Children.Add(close);
            var title=new StackPanel {Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};var icon=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(ProductTheme).Assembly.Location),"Resources","CodeAssist16.png");if(System.IO.File.Exists(icon))title.Children.Add(new Image {Source=new System.Windows.Media.Imaging.BitmapImage(new Uri(icon)),Width=16,Height=16,Margin=new Thickness(0,0,6,0)});title.Children.Add(Text(window.Title,12));caption.Children.Add(title);DockPanel.SetDock(caption,Dock.Top);chrome.Children.Add(caption);chrome.Children.Add(content);window.Content=new Border {BorderThickness=new Thickness(1),BorderBrush=Brushes.DimGray,Child=chrome};
        }
        internal static void Apply(FrameworkElement root)
        {
            root.Loaded+=(sender,args)=>AttachChrome(Window.GetWindow(root));
            if(root is Panel)root.SetResourceReference(Panel.BackgroundProperty,EnvironmentColors.ToolWindowBackgroundBrushKey);
            if(root is Border)root.SetResourceReference(Border.BackgroundProperty,EnvironmentColors.ToolWindowBackgroundBrushKey);
            root.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.ToolWindowTextBrushKey);
            root.SetResourceReference(Control.BackgroundProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);
            foreach (var type in new[] { typeof(CheckBox), typeof(RadioButton), typeof(TreeViewItem), typeof(TreeView), typeof(TextBox), typeof(ComboBox), typeof(ComboBoxItem), typeof(ScrollViewer), typeof(ListView), typeof(ListViewItem), typeof(GridViewColumnHeader) })
            {
                var style = new Style(type);
                style.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension(EnvironmentColors.ToolWindowTextBrushKey)));
                if (type == typeof(ListView) || type == typeof(GridViewColumnHeader) || type == typeof(ScrollViewer) || type == typeof(TreeView) || type == typeof(TextBox) || type == typeof(ComboBox) || type == typeof(ComboBoxItem))
                    style.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(EnvironmentColors.ToolWindowBackgroundBrushKey)));
                if(type == typeof(ComboBox))
                    style.Setters.Add(new Setter(Control.TemplateProperty, comboTemplate ?? (comboTemplate = (ControlTemplate)XamlReader.Parse(@"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='{x:Type ComboBox}' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
 <Grid MinHeight='26'>
  <Border Background='{TemplateBinding Background}' BorderBrush='#666666' BorderThickness='1' CornerRadius='2'/>
  <ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}'>
   <ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border Background='Transparent'><Path Data='M0,0 L4,4 L8,0' Stroke='{Binding Foreground,RelativeSource={RelativeSource AncestorType=ComboBox}}' StrokeThickness='1.3' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,8,0'/></Border></ControlTemplate></ToggleButton.Template>
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
                if(type==typeof(CheckBox)||type==typeof(RadioButton)) {
                    var shape=type==typeof(CheckBox)?"<Border Width='13' Height='13' BorderThickness='1' BorderBrush='{TemplateBinding Foreground}' Background='Transparent'/>":"<Ellipse Width='13' Height='13' Stroke='{TemplateBinding Foreground}' Fill='Transparent'/>";
                    var mark=type==typeof(CheckBox)?"<Path x:Name='Mark' Data='M 2,6 L 5,9 L 11,2' Stroke='{TemplateBinding Foreground}' StrokeThickness='1.7' Width='13' Height='13' Visibility='Collapsed'/>":"<Ellipse x:Name='Mark' Width='7' Height='7' Fill='{TemplateBinding Foreground}' Visibility='Collapsed'/>";
                    style.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='"+type.Name+"'><Grid Background='Transparent'><Grid.ColumnDefinitions><ColumnDefinition Width='13'/><ColumnDefinition Width='*'/></Grid.ColumnDefinitions><Grid VerticalAlignment='Center'>"+shape+mark+"</Grid><ContentPresenter Grid.Column='1' ContentSource='Content' RecognizesAccessKey='True' Margin='6,0,0,0' VerticalAlignment='Center'/></Grid><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Mark' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate>")));
                }
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
        // Normalized vector paths: independent of font, DPI and installed glyph sets.
        internal static FrameworkElement Icon(string name, double size = 16,string role=null)
        {
            string data;
            switch(name) {
                case "Project": data="M2,4 L6,4 L8,6 L14,6 L14,13 L2,13 Z M3,2 L10,2 L10,4";break;
                case "File": data="M3,1 L10,1 L13,4 L13,15 L3,15 Z M10,1 L10,4 L13,4 M5,7 L11,7 M5,10 L10,10";break;
                case "Refresh": data="M13,6 A5,5 0 1 0 13,11 M13,2 L13,6 L9,6";break;
                case "Clone": data="M1,2 L10,2 L10,10 L1,10 Z M6,6 L15,6 L15,14 L6,14 Z";break;
                case "Reference": data="M6,3 A5,5 0 1 0 6,13 A5,5 0 1 0 6,3 M8,8 L15,8 M12,5 L15,8 L12,11";break;
                case "NextReference": data="M8,2 L8,13 M4,9 L8,13 L12,9";break;
                case "PreviousReference": data="M8,14 L8,3 M4,7 L8,3 L12,7";break;
                case "Close": data="M4,4 L12,12 M12,4 L4,12";break;
                case "Search": data="M10.8,10.8 L14,14 M11.5,6.7 A4.8,4.8 0 1 1 1.9,6.7 A4.8,4.8 0 1 1 11.5,6.7";break;
                case "Up": data="M4,10 L8,6 L12,10";break;
                case "Down": data="M4,6 L8,10 L12,6";break;
                case "Corresponding": data="M2,5 L13,5 M10,2 L13,5 L10,8 M14,11 L3,11 M6,8 L3,11 L6,14";break;
                default: data="M2,8 L13,8 M9,4 L13,8 L9,12";break;
            }
            var path=new System.Windows.Shapes.Path {Data=Geometry.Parse(data),StrokeThickness=1.4,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round};
            if(role==null)path.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,EnvironmentColors.ToolWindowTextBrushKey);else BrushFor(path,System.Windows.Shapes.Shape.StrokeProperty,role);
            var canvas=new Canvas {Width=16,Height=16};canvas.Children.Add(path);
            return new Viewbox {Width=size,Height=size,Child=canvas,IsHitTestVisible=false};
        }
        internal static Button Button(string label, Action click, bool primary = false)
        {
            var button = new Button { Content = UiText.Get(label, StartupUi.Preferences.UiLanguage), Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(0, 0, 10, 0), MinWidth = 88,
                Foreground = primary ? Brushes.Black : Brushes.White,
                Background = primary ? Accent : new SolidColorBrush(Color.FromRgb(62,62,66)), BorderThickness = new Thickness(0) };
            button.BorderThickness=new Thickness(1);
            button.BorderBrush=Brushes.Transparent;
            button.SnapsToDevicePixels=true;
            button.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>
<Grid><Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='2'/>
<Border x:Name='State' Background='Transparent' CornerRadius='2'/>
<ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='Center' VerticalAlignment='Center' RecognizesAccessKey='True'/>
<Border x:Name='Focus' BorderBrush='{TemplateBinding Foreground}' BorderThickness='1' Margin='3' CornerRadius='1' Visibility='Collapsed' IsHitTestVisible='False'/></Grid>
<ControlTemplate.Triggers>
<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='State' Property='Background' Value='#18808080'/><Setter Property='BorderBrush' Value='#888888'/></Trigger>
<Trigger Property='IsPressed' Value='True'><Setter TargetName='State' Property='Background' Value='#44808080'/></Trigger>
<Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Focus' Property='Visibility' Value='Visible'/></Trigger>
<Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger>
</ControlTemplate.Triggers></ControlTemplate>");
            if(primary) { button.Foreground=Brushes.White; BrushFor(button,Control.BackgroundProperty,"Primary"); }
            else { button.SetResourceReference(Control.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey); BrushFor(button,Control.BackgroundProperty,"Button"); }
            if(label=="×") {button.Content=Icon("Close");System.Windows.Automation.AutomationProperties.SetName(button,UiText.Get("닫기",StartupUi.Preferences.UiLanguage));}
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
            var root = new DockPanel { Margin = new Thickness(20, 8, 26, 20) };
            ProductTheme.Apply(root);
            var window = new Window { Title = "CodeAssist Options", Width = 890, Height = 675, MinWidth = 780, MinHeight = 580,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
            var icon = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(ProductOptions).Assembly.Location), "Resources", "CodeAssist.png");
            if(System.IO.File.Exists(icon)) window.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(icon));
            window.WindowStyle = WindowStyle.None;
            System.Windows.Shell.WindowChrome.SetWindowChrome(window, new System.Windows.Shell.WindowChrome {
                CaptionHeight=32, ResizeBorderThickness=new Thickness(6), GlassFrameThickness=new Thickness(0), CornerRadius=new CornerRadius(0) });
            var chrome = new DockPanel();
            ProductTheme.Apply(chrome);
            var caption = new DockPanel { Height=32, LastChildFill=true, Margin=new Thickness(10,0,2,0) };
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
            window.Content=new Border { BorderBrush=Brushes.DimGray,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(0),Child=chrome };
            window.SetResourceReference(Control.BackgroundProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);
            window.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.ToolWindowTextBrushKey);
            var footer = new DockPanel { Margin = new Thickness(236, 16, 0, 4) };
            DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
            var status = ProductTheme.Text(UiText.Get("설정은 저장할 때 적용됩니다.", StartupUi.Preferences.UiLanguage), 12);
            var footerButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            DockPanel.SetDock(footerButtons, Dock.Right); footer.Children.Add(footerButtons); footer.Children.Add(status);
            status.Visibility=firstRun?Visibility.Visible:Visibility.Collapsed;
            var errors = new HashSet<string>();
            var save = ProductTheme.Button(UiText.Get("저장", StartupUi.Preferences.UiLanguage), () => { ThreadHelper.ThrowIfNotOnUIThread(); draft.SetupCompleted = true; if(StartupUi.ApplyPreferences(draft)) window.DialogResult = true; }, true);
            save.IsDefault=true;
            var cancel=ProductTheme.Button(UiText.Get("취소", StartupUi.Preferences.UiLanguage), () => window.DialogResult = false);cancel.IsCancel=true;
            save.Width=90;save.Height=30;save.MinWidth=90;save.Padding=new Thickness(0);save.Margin=new Thickness(0,0,20,0);
            cancel.Width=90;cancel.Height=30;cancel.MinWidth=90;cancel.Padding=new Thickness(0);cancel.Margin=new Thickness(0);
            footerButtons.Children.Add(save); footerButtons.Children.Add(cancel);
            var sidebar = new DockPanel { Width = 196, Margin = new Thickness(0, 0, 40, 0) };
            DockPanel.SetDock(sidebar, Dock.Left); root.Children.Add(sidebar);
            var search = new TextBox { Height = 30, Padding = new Thickness(7,4,7,4), Margin = new Thickness(0, 0, 0, 8), ToolTip = UiText.Get("설정 검색", StartupUi.Preferences.UiLanguage) };
            System.Windows.Automation.AutomationProperties.SetName(search, UiText.Get("설정 검색", StartupUi.Preferences.UiLanguage));
            var searchBox = new Grid();
            searchBox.Children.Add(search);
            var placeholder = new TextBlock { Text=UiText.Get("설정 검색", StartupUi.Preferences.UiLanguage), Foreground=Brushes.Gray, Margin=new Thickness(8,6,6,12), IsHitTestVisible=false };
            searchBox.Children.Add(placeholder);
            var searchIcon=ProductTheme.Icon("Search",14);searchIcon.HorizontalAlignment=HorizontalAlignment.Right;searchIcon.VerticalAlignment=VerticalAlignment.Top;searchIcon.Margin=new Thickness(0,7,8,0);searchBox.Children.Add(searchIcon);
            search.Padding=new Thickness(4,2,26,2);
            search.TextChanged += (sender,args) => placeholder.Visibility=search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;
            DockPanel.SetDock(searchBox, Dock.Top); sidebar.Children.Add(searchBox);
            var tree = new TreeView { FontSize=12, BorderThickness = new Thickness(0), Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch };
            tree.Resources[SystemColors.HighlightBrushKey]=new SolidColorBrush(Color.FromRgb(61,61,61));
            tree.Resources[SystemColors.InactiveSelectionHighlightBrushKey]=new SolidColorBrush(Color.FromRgb(61,61,61));
            tree.Resources[SystemColors.HighlightTextBrushKey]=Brushes.White;
            tree.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey]=Brushes.White;
            sidebar.Children.Add(tree);
            var content = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            root.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            var pages = new Dictionary<string, Action>();
            CheckBox Toggle(string title, bool value, Action<bool> set)
            {
                var control = new CheckBox { Content = UiText.Get(title,draft.UiLanguage), IsChecked = value, Margin = new Thickness(0, 0, 0, 7) };
                control.Click += (s,e) => set(control.IsChecked == true); content.Children.Add(control); return control;
            }
            void ValidateReference(string key,bool valid) { if(valid) errors.Remove(key);else errors.Add(key);save.IsEnabled=errors.Count==0; }
            void Heading(string section, string page)
            {
                content.Children.Clear();
                var title = new TextBlock { FontSize=20, FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,0,0,17) };
                ProductTheme.BrushFor(title, TextBlock.ForegroundProperty, "Muted");
                title.Inlines.Add(new System.Windows.Documents.Run("CodeAssist  |  "+UiText.Get(section,draft.UiLanguage)));
                var active=new System.Windows.Documents.Run(page==section?"": "  |  "+UiText.Get(page,draft.UiLanguage));
                if(page==section){title.Inlines.Clear();title.Inlines.Add(new System.Windows.Documents.Run("CodeAssist  |  "));active.Text=UiText.Get(section,draft.UiLanguage);}
                active.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);title.Inlines.Add(active);
                content.Children.Add(title);
            }
            void Number(string label, int value, int minimum, int maximum, Action<int> set)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,14) };
                row.Children.Add(new TextBlock { Text = UiText.Get(label,draft.UiLanguage), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,12,0) });
                var number = new ComboBox { Width = 92, ItemsSource = Enumerable.Range(minimum, maximum-minimum+1).ToArray(), SelectedItem = value, MaxDropDownHeight = 240 };
                number.SelectionChanged += (s,e) => { if(number.SelectedItem is int n) set(n); }; row.Children.Add(number); content.Children.Add(row);
            }
            FrameworkElement InlineNumber(string name,int value,int minimum,int maximum,Action<int> set)
            {
                var grid=new Grid {Width=46,Height=25,Margin=new Thickness(4,-3,4,0)};
                grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(14)});
                var input=new TextBox {Text=value.ToString(),Padding=new Thickness(3,2,0,0),VerticalContentAlignment=VerticalAlignment.Center};
                System.Windows.Automation.AutomationProperties.SetName(input,UiText.Get(name,draft.UiLanguage));grid.Children.Add(input);
                input.TextChanged+=(sender,args)=>{if(int.TryParse(input.Text,out var n)&&n>=minimum&&n<=maximum){errors.Remove(name);set(n);}else errors.Add(name);save.IsEnabled=errors.Count==0;};
                var arrows=new Grid();arrows.RowDefinitions.Add(new RowDefinition());arrows.RowDefinitions.Add(new RowDefinition());Grid.SetColumn(arrows,1);grid.Children.Add(arrows);
                for(var direction=0;direction<2;direction++){
                    var delta=direction==0?1:-1;
                    var button=new System.Windows.Controls.Primitives.RepeatButton {Content=ProductTheme.Icon(direction==0?"Up":"Down",9),FontSize=10,Padding=new Thickness(0),Focusable=false};
                    System.Windows.Automation.AutomationProperties.SetName(button,UiText.Get(name,draft.UiLanguage)+(delta>0?" +":" −"));
                    button.Click+=(sender,args)=>{var current=int.TryParse(input.Text,out var n)?n:value;input.Text=Math.Max(minimum,Math.Min(maximum,current+delta)).ToString();};Grid.SetRow(button,direction);arrows.Children.Add(button);
                }
                return grid;
            }
            void ColorChoice(string label, string value, Action<string> set)
            {
                var row = new StackPanel { Orientation=Orientation.Horizontal, Margin=new Thickness(20,0,0,10), Height=26 };
                var swatch = ProductTheme.Button("",()=>{});swatch.Width=44;swatch.MinWidth=44;swatch.Height=24;swatch.Padding=new Thickness(4);swatch.Margin=new Thickness(0,0,6,0);swatch.BorderThickness=new Thickness(2);swatch.BorderBrush=Brushes.Gray;
                var fill=new Border {Background=(Brush)new BrushConverter().ConvertFromString(value),Width=36,Height=18};swatch.Content=fill;
                System.Windows.Automation.AutomationProperties.SetName(swatch,label);
                swatch.ToolTip=value;swatch.Tag=label;
                swatch.Click+=(sender,args)=>ColorSelectionPopup.Show(swatch,label,swatch.ToolTip as string,draft,color=> {
                    set(color);fill.Background=(Brush)new BrushConverter().ConvertFromString(color);swatch.ToolTip=color;
                });
                row.Children.Add(swatch);row.Children.Add(new TextBlock {Text=label,VerticalAlignment=VerticalAlignment.Center});content.Children.Add(row);
            }
            void Unavailable(string description)
            {
                content.Children.Add(ProductTheme.Text(description));
                content.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(30,255,211,70)), Padding = new Thickness(12),
                    Child = ProductTheme.Text(UiText.Get("이 범주는 현재 지원하지 않습니다. 제공되는 기능은 관련 설정에서 사용할 수 있습니다.", StartupUi.Preferences.UiLanguage)) });
            }
            void Page(TreeViewItem parent, string section, string title, Action body)
            {
                var item = new TreeViewItem { Header = UiText.Get(title, StartupUi.Preferences.UiLanguage), Tag = section + " / " + title, Padding = new Thickness(0,3,0,3) };
                parent.Items.Add(item); pages[(string)item.Tag] = () => { Heading(section,title); body(); };
            }
            TreeViewItem Group(string name, Action body)
            {
                var item = new TreeViewItem { Header = UiText.Get(name, StartupUi.Preferences.UiLanguage), Tag = name, IsExpanded = true, Padding = new Thickness(0,3,0,3) };
                tree.Items.Add(item); pages[name] = () => { Heading(name,name); body(); }; return item;
            }
            var editor = Group("Editor", () => {
                Toggle(UiText.Get("Repair type name case",draft.UiLanguage),draft.RepairTypeCase,v=>draft.RepairTypeCase=v);
                var pasteRow=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,7)};
                var pasteToggle=new CheckBox {Content=UiText.Get("Display paste menu with",draft.UiLanguage),IsChecked=draft.ClipboardMenuEnabled,Margin=new Thickness(0),VerticalAlignment=VerticalAlignment.Center};pasteRow.Children.Add(pasteToggle);
                var pasteCount=InlineNumber("클립보드 기록 항목 수",draft.ClipboardLimit,1,50,v=>draft.ClipboardLimit=v);pasteCount.IsEnabled=draft.ClipboardMenuEnabled;pasteRow.Children.Add(pasteCount);
                pasteToggle.Click+=(sender,args)=>{draft.ClipboardMenuEnabled=pasteToggle.IsChecked==true;pasteCount.IsEnabled=draft.ClipboardMenuEnabled;};
                pasteRow.Children.Add(ProductTheme.Text(UiText.Get("items",draft.UiLanguage),12));foreach(var label in pasteRow.Children.OfType<TextBlock>()){label.Margin=new Thickness(0);label.VerticalAlignment=VerticalAlignment.Center;}content.Children.Add(pasteRow);
                Toggle(UiText.Get("Format after paste",StartupUi.Preferences.UiLanguage),draft.FormatAfterPasteEnabled,v=>draft.FormatAfterPasteEnabled=v);
                Toggle(UiText.Get("Extend multi-line comments",StartupUi.Preferences.UiLanguage),draft.ExtendCommentsEnabled,v=>draft.ExtendCommentsEnabled=v);
                Toggle(UiText.Get("Insert () and closing brackets and quotes",StartupUi.Preferences.UiLanguage),draft.PairCompletionEnabled,v=>draft.PairCompletionEnabled=v);
                var spacesRow=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(20,5,0,16)};
                spacesRow.Children.Add(ProductTheme.Text(UiText.Get("and put",draft.UiLanguage),12));spacesRow.Children.Add(InlineNumber("Spaces between method name and ()",draft.MethodParenthesesSpaces,0,10,v=>draft.MethodParenthesesSpaces=v));spacesRow.Children.Add(ProductTheme.Text(UiText.Get("spaces between method name and ()",draft.UiLanguage),12));foreach(var label in spacesRow.Children.OfType<TextBlock>()){label.Margin=new Thickness(0);label.VerticalAlignment=VerticalAlignment.Center;}content.Children.Add(spacesRow);
                var braceTitle=ProductTheme.Text(UiText.Get("Insert closing brace:",draft.UiLanguage),12);braceTitle.Margin=new Thickness(20,0,0,10);content.Children.Add(braceTitle);
                foreach(var placement in new[]{ClosingBracePlacement.BlankLine,ClosingBracePlacement.NextLine,ClosingBracePlacement.SameLine})
                {
                    var selectedPlacement=placement;
                    var label=placement==ClosingBracePlacement.BlankLine?"after a blank line":placement==ClosingBracePlacement.NextLine?"on the next line":"on the same line";
                    var radio=new RadioButton {Content=UiText.Get(label,StartupUi.Preferences.UiLanguage),GroupName="ClosingBracePlacement",IsChecked=draft.ClosingBracePlacement==placement,Margin=new Thickness(40,0,0,10)};
                    radio.Click+=(s,e)=>draft.ClosingBracePlacement=selectedPlacement;content.Children.Add(radio);
                }
                var surround=Toggle(UiText.Get("Surround Selection via character toggles",StartupUi.Preferences.UiLanguage),draft.SurroundSelectionEnabled,v=>draft.SurroundSelectionEnabled=v);
                var row=new WrapPanel {Margin=new Thickness(20,0,0,14)};
                foreach(var value in new[]{"/","*","(","{","#"})
                {
                    var key=value;var checkbox=new CheckBox {Content=value,Margin=new Thickness(0,0,18,0),IsEnabled=draft.SurroundSelectionEnabled,
                        IsChecked=value=="/"?draft.SurroundSlash:value=="*"?draft.SurroundAsterisk:value=="("?draft.SurroundParentheses:value=="#"?draft.SurroundHash:draft.SurroundBraces};
                    checkbox.Click+=(s,e)=>{var enabled=checkbox.IsChecked==true;if(key=="/")draft.SurroundSlash=enabled;else if(key=="*")draft.SurroundAsterisk=enabled;else if(key=="(")draft.SurroundParentheses=enabled;else if(key=="#")draft.SurroundHash=enabled;else draft.SurroundBraces=enabled;};
                    surround.Click+=(s,e)=>checkbox.IsEnabled=draft.SurroundSelectionEnabled;row.Children.Add(checkbox);
                }
                var snippets=new CheckBox {Content=UiText.Get("With CodeAssist Snippets",draft.UiLanguage),IsChecked=draft.SurroundWithSnippets,IsEnabled=draft.SurroundSelectionEnabled};
                snippets.Click+=(s,e)=>draft.SurroundWithSnippets=snippets.IsChecked==true;surround.Click+=(s,e)=>snippets.IsEnabled=draft.SurroundSelectionEnabled;row.Children.Add(snippets);
                content.Children.Add(row);
                Toggle(UiText.Get("Insert a space when toggling a line into a comment",StartupUi.Preferences.UiLanguage),draft.CommentSpaceEnabled,v=>draft.CommentSpaceEnabled=v);
                var arrow=Toggle(UiText.Get("Convert pointer dot to arrow",draft.UiLanguage),draft.ConvertDotToArrow,v=>draft.ConvertDotToArrow=v);
                var overload=Toggle(UiText.Get("Convert dot to -> if operator -> is overloaded",draft.UiLanguage),draft.ConvertOverloadedArrow,v=>draft.ConvertOverloadedArrow=v);overload.Margin=new Thickness(20,0,0,7);overload.IsEnabled=draft.ConvertDotToArrow;arrow.Click+=(s,e)=>overload.IsEnabled=draft.ConvertDotToArrow;
                Toggle(UiText.Get("Insert underscore after m and Shift",draft.UiLanguage),draft.InsertUnderscoreAfterM,v=>draft.InsertUnderscoreAfterM=v);
                Toggle(UiText.Get("Double effective rate of key repeat",draft.UiLanguage),draft.DoubleKeyRepeat,v=>draft.DoubleKeyRepeat=v);
            });
            Page(editor,"Editor","Coloring",() => {
                Toggle(UiText.Get("향상된 심볼 색상 사용",draft.UiLanguage),draft.ColoringEnabled,v=>draft.ColoringEnabled=v).Margin=new Thickness(0,0,0,14);
                ColorChoice(UiText.Get("클래스와 타입",draft.UiLanguage),draft.TypeColor,v=>draft.TypeColor=v);
                ColorChoice(UiText.Get("변수",draft.UiLanguage),draft.VariableColor,v=>draft.VariableColor=v);
                ColorChoice(UiText.Get("Preprocessor macros",draft.UiLanguage),draft.MacroColor,v=>draft.MacroColor=v);
                ColorChoice(UiText.Get("Enum members",draft.UiLanguage),draft.EnumMemberColor,v=>draft.EnumMemberColor=v);
                ColorChoice(UiText.Get("Functions / methods",draft.UiLanguage),draft.FunctionColor,v=>draft.FunctionColor=v);
                ColorChoice(UiText.Get("Namespaces",draft.UiLanguage),draft.NamespaceColor,v=>draft.NamespaceColor=v);
                var applyTitle=ProductTheme.Text(UiText.Get("Apply coloring to",draft.UiLanguage),12);applyTitle.Margin=new Thickness(0,28,0,10);content.Children.Add(applyTitle);
                content.Children.Add(new Border {Height=1,Background=Brushes.DimGray,Margin=new Thickness(0,0,0,10)});
                var targets=new Grid {HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(20,0,0,0)};
                targets.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(132)});targets.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(132)});targets.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});targets.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});targets.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
                void Target(string name,bool value,Action<bool> set,bool enabled=true){var check=new CheckBox {Content=UiText.Get(name,draft.UiLanguage),IsChecked=value,IsEnabled=enabled,Margin=new Thickness(0,0,12,10)};check.Click+=(s,e)=>set(check.IsChecked==true);Grid.SetRow(check,targets.Children.Count/3);Grid.SetColumn(check,targets.Children.Count%3);targets.Children.Add(check);}
                Target("Text Editors",draft.ColorTextEditors,v=>draft.ColorTextEditors=v);Target("Tooltips",draft.ColorTooltips,v=>draft.ColorTooltips=v);Target("Listboxes",draft.ColorListboxes,v=>draft.ColorListboxes=v);
                Target("Object Browser",draft.ColorObjectBrowser,v=>draft.ColorObjectBrowser=v);Target("Views",draft.ColorViews,v=>draft.ColorViews=v);Target("CodeAssist Navigation Bar",draft.ColorNavigationBar,v=>draft.ColorNavigationBar=v);content.Children.Add(targets);
                var attributes=ProductTheme.Text(UiText.Get("Text editor font attributes",draft.UiLanguage),12);attributes.Margin=new Thickness(0,28,0,10);content.Children.Add(attributes);
                content.Children.Add(new Border {Height=1,Background=Brushes.DimGray,Margin=new Thickness(0,0,0,10)});
                Toggle(UiText.Get("Local symbols in bold",draft.UiLanguage),draft.LocalSymbolsBold,v=>draft.LocalSymbolsBold=v);
                Toggle(UiText.Get("System symbols in italics",draft.UiLanguage),draft.SystemSymbolsItalic,v=>draft.SystemSymbolsItalic=v);
                var completion=Toggle(UiText.Get("Visual Studio 기본 추천 색상 사용",draft.UiLanguage),draft.CompletionUseVisualStudioColors,v=>draft.CompletionUseVisualStudioColors=v);completion.Margin=new Thickness(0,22,0,0);
            });
            Page(editor,"Editor","Underlining",()=>{ ReferenceOptions.Render("Underlining",content,draft,ValidateReference); });
            Page(editor,"Editor","Highlighting",()=>{ ReferenceOptions.Render("Highlighting",content,draft,ValidateReference); });
            Page(editor,"Editor","Floating Elements",()=>Unavailable(UiText.Get("빠른 작업은 Alt+Shift+Q로 엽니다. 부동 빠른 작업 툴바는 현재 지원하지 않습니다.", StartupUi.Preferences.UiLanguage)));
            Page(editor,"Editor","Smart Select",()=> {
                Toggle(UiText.Get("Start and grow selections by relatively small increments",StartupUi.Preferences.UiLanguage),draft.SmartSelectSmallIncrements,v=>draft.SmartSelectSmallIncrements=v);
                var word=Toggle(UiText.Get("Start non-block selections with the current word",StartupUi.Preferences.UiLanguage),draft.SmartSelectStartWithWord,v=>draft.SmartSelectStartWithWord=v);
                var casing=Toggle(UiText.Get("Changes in case delimit current word",StartupUi.Preferences.UiLanguage),draft.SmartSelectSplitCase,v=>draft.SmartSelectSplitCase=v);
                var underscore=Toggle(UiText.Get("Underscores delimit current word",StartupUi.Preferences.UiLanguage),draft.SmartSelectSplitUnderscores,v=>draft.SmartSelectSplitUnderscores=v);
                casing.Margin=underscore.Margin=new Thickness(20,0,0,14);
                casing.IsEnabled=underscore.IsEnabled=draft.SmartSelectStartWithWord;
                word.Click+=(s,e)=>casing.IsEnabled=underscore.IsEnabled=draft.SmartSelectStartWithWord;
                content.Children.Add(new Border {Height=1,Background=Brushes.DimGray,Margin=new Thickness(0,4,0,16)});
                Toggle(UiText.Get("Smart Select 사용",StartupUi.Preferences.UiLanguage),draft.SmartSelectEnabled,v=>draft.SmartSelectEnabled=v);
                Toggle(UiText.Get("줄 선택 전에 앞뒤 공백을 제외한 범위 선택",StartupUi.Preferences.UiLanguage),draft.SmartSelectTrimWhitespace,v=>draft.SmartSelectTrimWhitespace=v);
                content.Children.Add(ProductTheme.Text(UiText.Get("선택 확장 Alt+Shift+] · 축소 Alt+Shift+[ · 블록 확장 Alt+] · 축소 Alt+[.", StartupUi.Preferences.UiLanguage)));
            });
            Page(editor,"Editor","Display",()=>{ ReferenceOptions.Render("Display",content,draft,ValidateReference); });
            Page(editor,"Editor","Guides",()=>{
                Toggle(UiText.Get("Local symbols in bold",draft.UiLanguage),draft.LocalSymbolsBold,v=>draft.LocalSymbolsBold=v);
                Toggle(UiText.Get("System symbols in italics",draft.UiLanguage),draft.SystemSymbolsItalic,v=>draft.SystemSymbolsItalic=v);
                Toggle(UiText.Get("Highlight references under caret",draft.UiLanguage),draft.AutomaticReferenceHighlights,v=>draft.AutomaticReferenceHighlights=v);
                Toggle(UiText.Get("Highlight output filenames",draft.UiLanguage),draft.OutputFileHighlighting,v=>draft.OutputFileHighlighting=v);
                Toggle(UiText.Get("Highlight find results",draft.UiLanguage),draft.FindResultHighlighting,v=>draft.FindResultHighlighting=v);
                Toggle(UiText.Get("Underline comment and string spelling",draft.UiLanguage),draft.SpellUnderliningEnabled,v=>draft.SpellUnderliningEnabled=v);
                var spellingLanguage=new TextBox {Text=draft.SpellingLanguage,Width=150,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};spellingLanguage.TextChanged+=(s,e)=>draft.SpellingLanguage=spellingLanguage.Text;content.Children.Add(spellingLanguage);
                Toggle(UiText.Get("Highlight current line",draft.UiLanguage),draft.HighlightCurrentLine,v=>draft.HighlightCurrentLine=v);
                Toggle(UiText.Get("Highlight matching braces",draft.UiLanguage),draft.HighlightMatchingBraces,v=>draft.HighlightMatchingBraces=v);
                Toggle(UiText.Get("Highlight mismatching braces",draft.UiLanguage),draft.HighlightMismatchingBraces,v=>draft.HighlightMismatchingBraces=v);
                Toggle(UiText.Get("Show column indicator",draft.UiLanguage),draft.ColumnIndicatorEnabled,v=>draft.ColumnIndicatorEnabled=v);
                Number(UiText.Get("Column",draft.UiLanguage),draft.ColumnIndicator,20,500,v=>draft.ColumnIndicator=v);
                Toggle(UiText.Get("Expand snippet shortcuts with Tab",draft.UiLanguage),draft.SnippetShortcutsEnabled,v=>draft.SnippetShortcutsEnabled=v);
            });
            Page(editor,"Editor","Mouse",()=>{ ReferenceOptions.Render("Mouse",content,draft,ValidateReference); });
            var navigation = Group("Navigation",()=>content.Children.Add(ProductTheme.Text(UiText.Get("Alt+G 정의/구현 · Alt+O 대응 파일 · Alt+Shift+S 심볼 · Alt+Shift+O 파일.", StartupUi.Preferences.UiLanguage))));
            Page(navigation,"Navigation","Goto",()=>{ ReferenceOptions.Render("Goto",content,draft,ValidateReference); });
            Page(navigation,"Navigation","Source Links",()=>SourceLinkOptions.Render(content,draft,ValidateReference));
            Page(navigation,"Navigation","CodeAssist Hashtags",()=>{ ReferenceOptions.Render("Hashtags",content,draft,ValidateReference); });
            var suggestions = Group("Suggestions",()=>{
                var enabled=Toggle("Enable Suggestion Lists",draft.SuggestionListsEnabled,v=>draft.SuggestionListsEnabled=v);
                var smart=Toggle("Include Smart Suggestions",draft.SmartSuggestionsEnabled,v=>draft.SmartSuggestionsEnabled=v);smart.Margin=new Thickness(20,0,0,24);smart.IsEnabled=draft.SuggestionListsEnabled;
                enabled.Click+=(sender,args)=>smart.IsEnabled=draft.SuggestionListsEnabled;
                var title=ProductTheme.Text(UiText.Get("Suggestion and Completion lists",draft.UiLanguage),12);title.Margin=new Thickness(0,0,0,10);content.Children.Add(title);
                var cpp=Toggle("Enable in C++",draft.CompletionEnabled,v=>draft.CompletionEnabled=v);cpp.Margin=new Thickness(20,0,0,7);
                var snippets=Toggle("Include CodeAssist Snippets",draft.SnippetSuggestionsEnabled,v=>draft.SnippetSuggestionsEnabled=v);snippets.Margin=new Thickness(20,0,0,7);
            });
            Page(suggestions,"Suggestions","Enhanced Listboxes",()=>{ ReferenceOptions.Render("Enhanced Listboxes",content,draft,ValidateReference); });
            var projects = Group("Projects",()=>content.Children.Add(ProductTheme.Text(UiText.Get("범용 C/C++ 프로젝트를 지원하며 Unreal은 추가 지원 대상입니다.", StartupUi.Preferences.UiLanguage))));
            Page(projects,"Projects","Projects and Files",()=>{ ReferenceOptions.Render("Projects and Files",content,draft,ValidateReference); });
            Page(projects,"Projects","C/C++ Directories",()=>{ ReferenceOptions.Render("C/C++ Directories",content,draft,ValidateReference); });
            Page(projects,"Projects","File Extensions",()=>{
                content.Children.Add(ProductTheme.Text("Additional C/C++ file extensions"));var input=new TextBox {Text=draft.CustomFileExtensions};content.Children.Add(input);
                input.TextChanged+=(s,e)=>{try{FileExtensionSettings.Parse(input.Text);draft.CustomFileExtensions=input.Text;errors.Remove("extensions");status.Text=UiText.Get("설정은 저장할 때 적용됩니다.",draft.UiLanguage);}catch(ArgumentException error){errors.Add("extensions");status.Text=error.Message;}save.IsEnabled=errors.Count==0;};
                content.Children.Add(ProductTheme.Text("Example: .ipp;.tpp;.cuh. Changes are applied to the next index build and cache fingerprint. Language-service associations remain Visual Studio settings."));
            });
            Page(projects,"Projects","Compilation Database",()=>{
                content.Children.Add(ProductTheme.Text("Compilation database directory"));var input=new TextBox {Text=draft.CompilationDatabaseDirectory};content.Children.Add(input);
                input.TextChanged+=(s,e)=>{var value=input.Text.Trim();if(value.Length==0||System.IO.Directory.Exists(value)&&System.IO.File.Exists(System.IO.Path.Combine(value,"compile_commands.json"))){draft.CompilationDatabaseDirectory=value;errors.Remove("database");status.Text=UiText.Get("설정은 저장할 때 적용됩니다.",draft.UiLanguage);}else{errors.Add("database");status.Text="Select a directory containing compile_commands.json.";}save.IsEnabled=errors.Count==0;};
                content.Children.Add(ProductTheme.Text("CMake, Ninja, Make and other build systems can export compile_commands.json. Select a target-specific directory to use its compiler, flags, target architecture and include paths. CodeAssist reads the database; it does not execute build commands."));
            });
            Group("Code Generation",()=>{ ReferenceOptions.Render("Code Generation",content,draft,ValidateReference); });
            Page(projects,"Projects","Generation Templates",()=>{
                content.Children.Add(ProductTheme.Text("Method implementation body template"));var body=new TextBox {Text=draft.MethodBodyTemplate,AcceptsReturn=true,Height=130,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};body.TextChanged+=(s,e)=>draft.MethodBodyTemplate=body.Text;content.Children.Add(body);content.Children.Add(ProductTheme.Text("Placeholders: $Body$, $MethodName$, $ClassName$, $ReturnType$, $Parameters$. Generated implementations use this template; business logic is supplied by you."));
            });
            var inspection = Group("Code Inspection",()=> {
                foreach(var mode in new[] { InspectionMode.Ask,InspectionMode.Project,InspectionMode.PerFile,InspectionMode.Disabled }) {
                    var captured=mode;
                    var radio=new RadioButton { Content=mode==InspectionMode.Project?UiText.Get("프로젝트 전체 기본 검사", StartupUi.Preferences.UiLanguage):mode==InspectionMode.PerFile?UiText.Get("파일별 검사", StartupUi.Preferences.UiLanguage):mode==InspectionMode.Disabled?UiText.Get("검사 사용 안 함", StartupUi.Preferences.UiLanguage):UiText.Get("상단 안내에서 선택", StartupUi.Preferences.UiLanguage), IsChecked=draft.InspectionMode==mode, Margin=new Thickness(0,0,0,12) };
                    radio.Click+=(s,e)=>draft.InspectionMode=captured;content.Children.Add(radio);
                }
                content.Children.Add(ProductTheme.Text(UiText.Get("프로젝트 전체 검사는 로컬 프로젝트 C/C++ 파일의 확정 구문 오류 후보를 검사해 오류 목록에 표시합니다. 컴파일러의 전체 진단을 대체하지 않습니다.", StartupUi.Preferences.UiLanguage)));
            });
            Page(inspection,"Code Inspection","Project-Wide",()=> { ReferenceOptions.Render("Project-Wide",content,draft,ValidateReference); });
            Page(inspection,"Code Inspection","Per-File",()=>{ ReferenceOptions.Render("Per-File",content,draft,ValidateReference); });
            Page(inspection,"Code Inspection","Checker Sets",()=>content.Children.Add(ProductTheme.Text(UiText.Get("프로젝트 전체: 괄호·제어문 구문 검사. 파일별: 타입 대소문자·식별자·구문·스코프 후보 검사. 추가 검사기는 미구현입니다.", StartupUi.Preferences.UiLanguage))));
            Group("CodeAssist Intelligence",()=>Unavailable(UiText.Get("로컬 AI·에이전트 연결은 현재 구현하지 않았습니다.", StartupUi.Preferences.UiLanguage)));
            Group("Debug Assistance",()=>{ ReferenceOptions.Render("Debug Assistance",content,draft,ValidateReference); });
            Group("Games & GPUs",()=>content.Children.Add(ProductTheme.Text(UiText.Get("Unreal 반사 매크로와 메타 지정자·엔진 심볼을 지원합니다. GPU 전용 분석기는 미구현입니다.", StartupUi.Preferences.UiLanguage))));
            var advanced = Group("Advanced",()=> {
                Toggle(UiText.Get("시작할 때 Welcome 문서 열기", StartupUi.Preferences.UiLanguage),draft.ShowWelcomeOnStartup,v=>draft.ShowWelcomeOnStartup=v);
                Toggle(UiText.Get("상단 안내바 표시", StartupUi.Preferences.UiLanguage),!draft.HideWelcomeBar,v=>draft.HideWelcomeBar=!v);
                content.Children.Add(ProductTheme.Button(UiText.Get("CodeAssist 문서", StartupUi.Preferences.UiLanguage),()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp("startup"); }));
            });
            Page(advanced,"Advanced","Logging and Recovery",()=> { ReferenceOptions.Render("Logging and Recovery",content,draft,ValidateReference); });
            Page(advanced,"Advanced","Startup",()=> { ReferenceOptions.Render("Startup",content,draft,ValidateReference); });
            Page(advanced,"Advanced","System Info",()=> { ReferenceOptions.Render("System Info",content,draft,ValidateReference); });
            Page(advanced,"Advanced","Language",()=> {
                content.Children.Add(ProductTheme.Text("UI 언어"));
                var choices = new[] { new LanguageChoice("auto",UiText.Get("시스템 언어",StartupUi.Preferences.UiLanguage)) }.Concat(UiText.LanguageCodes.Select(code=>new LanguageChoice(code,UiText.LanguageName(code)))).ToArray();
                var language = new ComboBox { ItemsSource=choices, DisplayMemberPath="Label", SelectedValuePath="Code", SelectedValue=draft.UiLanguage, Width=220, HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,0,0,14) };
                language.SelectionChanged+=(s,e)=>{if(language.SelectedValue is string code)draft.UiLanguage=code;};
                content.Children.Add(language);
                content.Children.Add(ProductTheme.Text("언어는 저장 후 새로 여는 화면에 적용됩니다."));
            });
            Page(advanced,"Advanced","Performance",()=>{ ReferenceOptions.Render("Performance",content,draft,ValidateReference); });
            var performancePage=advanced.Items.OfType<TreeViewItem>().First(i=>(string)i.Tag=="Advanced / Performance");advanced.Items.Remove(performancePage);advanced.Items.Insert(0,performancePage);
            foreach(var parent in new[]{editor,projects}) {
                foreach(var extra in parent.Items.OfType<TreeViewItem>().Where(i=>new[]{"Editor / Guides","Projects / File Extensions","Projects / Compilation Database","Projects / Generation Templates"}.Contains(i.Tag as string)).ToArray()) {parent.Items.Remove(extra);advanced.Items.Add(extra);}
            }
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