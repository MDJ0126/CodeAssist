using CodeAssist.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace CodeAssist.Extension
{
    internal static class ProductWelcome
    {
        private static readonly Brush Ink = Brushes.White;
        private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(185,191,204));
        private static readonly Brush Canvas = new SolidColorBrush(Color.FromRgb(5,7,11));
        private static readonly Brush Card = new SolidColorBrush(Color.FromRgb(17,20,31));
        private static TextBlock Text(string value, double size=14) => new TextBlock { Text=UiText.Get(value,StartupUi.Preferences.UiLanguage), FontSize=size, Foreground=Ink, TextWrapping=TextWrapping.Wrap, Margin=new Thickness(0,0,0,14) };
        private static StackPanel Body() => new StackPanel { Margin=new Thickness(28) };
        private static Border CardView(StackPanel content) => new Border { Background=Card, CornerRadius=new CornerRadius(16), Child=content, Margin=new Thickness(0,0,0,24) };
        internal static FrameworkElement Create()
        {
            var content = new StackPanel { MaxWidth=1248, Margin=new Thickness(48,36,48,42), HorizontalAlignment=HorizontalAlignment.Stretch };
            var hero = new StackPanel { Margin=new Thickness(0,12,0,56), HorizontalAlignment=HorizontalAlignment.Center };
            var brand = new StackPanel { Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=new Thickness(0,0,0,22) };
            var path=Path.Combine(Path.GetDirectoryName(typeof(ProductWelcome).Assembly.Location),"Resources","CodeAssist.png");
            if(File.Exists(path)) brand.Children.Add(new Image { Source=new BitmapImage(new Uri(path)),Width=24,Height=24,Margin=new Thickness(0,0,8,0) });
            brand.Children.Add(new TextBlock { Text="CodeAssist",Foreground=Ink,FontSize=22,FontWeight=FontWeights.SemiBold });hero.Children.Add(brand);
            var title = new TextBlock { FontSize=52, FontWeight=FontWeights.Bold, TextWrapping=TextWrapping.Wrap, TextAlignment=TextAlignment.Center, Margin=new Thickness(0,0,0,20) };
            title.Inlines.Add(new Run("Welcome to ") { Foreground=Ink }); title.Inlines.Add(new Run("CodeAssist") { Foreground=ProductTheme.Accent });hero.Children.Add(title);
            var intro=Text(UiText.Get("코드를 이해하고, 원하는 위치로 이동하고, 더 빠르게 편집하세요.", StartupUi.Preferences.UiLanguage),18);intro.Foreground=Muted;intro.TextAlignment=TextAlignment.Center;hero.Children.Add(intro);
            var links=new WrapPanel { HorizontalAlignment=HorizontalAlignment.Center, Margin=new Thickness(0,18,0,0) };
            links.Children.Add(ProductTheme.Button("Documentation",()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp("startup"); }));
            links.Children.Add(ProductTheme.Button(UiText.Get("주요 기능", StartupUi.Preferences.UiLanguage),()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp("features"); }));
            links.Children.Add(ProductTheme.Button(UiText.Get("단축키", StartupUi.Preferences.UiLanguage),()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp("shortcuts"); }));
            links.Children.Add(ProductTheme.Button("Options",()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.Configure(false); },true));
            hero.Children.Add(links);content.Children.Add(hero);
            var news=Body();var heading=Text("What's New with CodeAssist",30);heading.FontWeight=FontWeights.SemiBold;news.Children.Add(heading);
            var features=new Grid { Margin=new Thickness(0,12,0,0) };
            features.ColumnDefinitions.Add(new ColumnDefinition());features.ColumnDefinitions.Add(new ColumnDefinition());
            features.RowDefinitions.Add(new RowDefinition());features.RowDefinitions.Add(new RowDefinition());
            var values=new[] {
                new[] {"PROJECT CODE INSPECTION",UiText.Get("프로젝트의 C/C++ 기본 구문 검사 결과를 오류 목록에서 확인하세요.", StartupUi.Preferences.UiLanguage),"inspection"},
                new[] {"EDITOR & COLORING",UiText.Get("심볼 팔레트와 자동 완성·클립보드 설정을 한곳에서 조정하세요.", StartupUi.Preferences.UiLanguage),"colors"},
                new[] {"FAST SYMBOL NAVIGATION",UiText.Get("파일·심볼·메서드·대응 파일을 단축키와 Context Bar로 찾으세요.", StartupUi.Preferences.UiLanguage),"navigation"},
                new[] {"UNREAL & C/C++",UiText.Get("범용 C/C++ 작업 흐름에 Unreal 매크로와 엔진 탐색 지원을 더합니다.", StartupUi.Preferences.UiLanguage),"features"} };
            for(int i=0;i<values.Length;i++) {
                var entry=values[i];var body=new StackPanel { Margin=new Thickness(0,0,24,24) };
                var label=Text("•  "+entry[0],14);label.Foreground=ProductTheme.Accent;label.FontWeight=FontWeights.SemiBold;body.Children.Add(label);
                var description=Text(entry[1]);description.Foreground=Muted;body.Children.Add(description);
                var more=ProductTheme.Button("Learn More  ›",()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp(entry[2]); });
                more.Background=Brushes.Transparent;more.Foreground=ProductTheme.Accent;more.HorizontalAlignment=HorizontalAlignment.Left;more.Padding=new Thickness(0);more.MinWidth=0;
                body.Children.Add(more);
                Grid.SetColumn(body,i%2);Grid.SetRow(body,i/2);features.Children.Add(body);
            }
            news.Children.Add(features);content.Children.Add(CardView(news));
            var lower=new Grid();lower.ColumnDefinitions.Add(new ColumnDefinition());lower.ColumnDefinitions.Add(new ColumnDefinition());
            var learn=Body();var learnTitle=Text("Learn More about CodeAssist",26);learnTitle.FontWeight=FontWeights.SemiBold;learn.Children.Add(learnTitle);
            learn.Children.Add(Text(UiText.Get("설치·권장 색상·탐색·검사 기능의 사용 방법과 검증 범위를 확인하세요.", StartupUi.Preferences.UiLanguage)));
            learn.Children.Add(ProductTheme.Button(UiText.Get("오프라인 사용 안내", StartupUi.Preferences.UiLanguage),()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.OpenHelp("startup"); }));
            learn.Children.Add(new Border { Height=20 });
            learn.Children.Add(ProductTheme.Button("심볼 찾기  Alt+Shift+S",()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.Execute(0x104); }));
            learn.Children.Add(new Border { Height=10 });
            learn.Children.Add(ProductTheme.Button("파일 열기  Alt+Shift+O",()=> { ThreadHelper.ThrowIfNotOnUIThread(); StartupUi.Execute(0x103); }));
            var left=CardView(learn);left.Margin=new Thickness(0,0,12,24);lower.Children.Add(left);
            var feedback=Body();var feedbackTitle=Text("Share your Feedback",26);feedbackTitle.FontWeight=FontWeights.SemiBold;feedback.Children.Add(feedbackTitle);
            feedback.Children.Add(Text(UiText.Get("개선할 점이나 재현 방법을 적어 복사하세요. 내용은 자동 전송하지 않습니다.", StartupUi.Preferences.UiLanguage)));
            var note=new TextBox { AcceptsReturn=true, TextWrapping=TextWrapping.Wrap, Height=120, Padding=new Thickness(10), Background=Canvas, Foreground=Ink, BorderBrush=Brushes.DimGray, Margin=new Thickness(0,0,0,16), VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
            System.Windows.Automation.AutomationProperties.SetName(note,UiText.Get("피드백 내용", StartupUi.Preferences.UiLanguage));
            feedback.Children.Add(note);
            var copied=Text("",12);copied.Foreground=ProductTheme.Accent;
            feedback.Children.Add(ProductTheme.Button(UiText.Get("피드백 내용 복사", StartupUi.Preferences.UiLanguage),()=> { try { Clipboard.SetText("CodeAssist 0.0.55\n"+note.Text);copied.Text="복사했습니다. 원하는 이슈나 대화에 붙여넣으세요."; }
                catch(System.Runtime.InteropServices.ExternalException) {copied.Text=UiText.Get("클립보드에 복사하지 못했습니다. 잠시 뒤 다시 시도하세요.", StartupUi.Preferences.UiLanguage);} }));
            feedback.Children.Add(copied);
            var right=CardView(feedback);right.Margin=new Thickness(12,0,0,24);Grid.SetColumn(right,1);lower.Children.Add(right);content.Children.Add(lower);
            var keys=Body();keys.Children.Add(Text("CodeAssist Shortcuts",28));
            var shortcutGrid=new Grid();shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition());shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(210) });
            var rows=new[] {new[]{"Find References","Alt+Shift+F"},new[]{"Find Selected","Alt+K"},new[]{"Find Symbol","Alt+Shift+S"},new[]{"Go to Definition / Implementation","Alt+G"},new[]{"Go to Related","Alt+Shift+G"},new[]{"List Methods","Alt+M"},new[]{"Corresponding File","Alt+O"},new[]{"Open File","Alt+Shift+O"},new[]{"Multiple Clipboards","Ctrl+Shift+V"},new[]{"Quick Actions","Alt+Shift+Q"},new[]{"Rename","Alt+Shift+R"},new[]{"Hashtags","Alt+Shift+H"}};
            for(int i=0;i<rows.Length;i++) {shortcutGrid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });for(int j=0;j<2;j++){var cell=Text(rows[i][j]);if(j==1)cell.Foreground=ProductTheme.Accent;Grid.SetRow(cell,i);Grid.SetColumn(cell,j);shortcutGrid.Children.Add(cell);}}
            keys.Children.Add(shortcutGrid);content.Children.Add(CardView(keys));
            var startup=new CheckBox { Content=UiText.Get("Visual Studio 시작 시 Welcome 열기", StartupUi.Preferences.UiLanguage), IsChecked=StartupUi.Preferences.ShowWelcomeOnStartup, Foreground=Muted, Margin=new Thickness(4,0,0,20) };
            startup.Click+=(s,e)=> {StartupUi.Preferences.ShowWelcomeOnStartup=startup.IsChecked==true;StartupUi.Save();};content.Children.Add(startup);
            var footer=Text(UiText.Get("CodeAssist · 범용 C/C++ 개발 보조 · Unreal Engine 지원", StartupUi.Preferences.UiLanguage),12);footer.Foreground=Muted;footer.TextAlignment=TextAlignment.Center;content.Children.Add(footer);
            var scroll = new ScrollViewer { Content=content, VerticalScrollBarVisibility=ScrollBarVisibility.Auto, HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };
            ProductTheme.Apply(scroll);scroll.Background=Canvas;scroll.Foreground=Ink;
            return scroll;
        }
    }
}