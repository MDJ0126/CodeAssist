using CodeAssist.Core;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
namespace CodeAssist.Extension
{
    [Guid("c25bc16f-d84a-45b2-932b-4fa52280a64a")]
    public sealed class HelpToolWindow : ToolWindowPane
    {
        public HelpToolWindow() : base(null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Caption="CodeAssist — "+UiText.Get("Documentation",StartupUi.Preferences.UiLanguage);
            Content=new HelpView();
        }
    }
    internal sealed class HelpView : UserControl
    {
        private string language=StartupUi.Preferences.UiLanguage;
        private readonly ListBox topics=new ListBox {DisplayMemberPath="Title",BorderThickness=new Thickness(0),Margin=new Thickness(0,12,12,0)};
        private readonly StackPanel article=new StackPanel {Margin=new Thickness(20)};
        private readonly TextBox search=new TextBox {Margin=new Thickness(0,0,12,0),Padding=new Thickness(6)};
        internal HelpView()
        {
            var root=new Grid {Margin=new Thickness(14)};ProductTheme.Apply(root);
            root.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(190)});root.ColumnDefinitions.Add(new ColumnDefinition());
            var sidebar=new DockPanel();DockPanel.SetDock(search,Dock.Top);sidebar.Children.Add(search);sidebar.Children.Add(topics);root.Children.Add(sidebar);
            topics.SetResourceReference(Control.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);topics.SetResourceReference(Control.BackgroundProperty,EnvironmentColors.ToolWindowBackgroundBrushKey);
            System.Windows.Automation.AutomationProperties.SetName(search,UiText.Get("Search help",language));
            var reader=new ScrollViewer {Content=article,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(reader,1);root.Children.Add(reader);Content=root;
            topics.SelectionChanged+=(s,e)=>Render(topics.SelectedItem as HelpSection);
            search.TextChanged+=(s,e)=>{topics.ItemsSource=HelpCatalog.Search(language,search.Text);topics.SelectedIndex=topics.Items.Count>0?0:-1;if(topics.Items.Count==0)Render(null);};
            topics.ItemsSource=HelpCatalog.Sections(language);Navigate("startup");
        }
        internal void Navigate(string topic)
        {
            language=StartupUi.Preferences.UiLanguage;topics.ItemsSource=HelpCatalog.Sections(language);
            search.Text="";
            // Select the instance already owned by ItemsSource.
            topics.SelectedItem=topics.Items.Cast<HelpSection>().FirstOrDefault(s=>s.Id==topic)??topics.Items.Cast<HelpSection>().First();
            topics.ScrollIntoView(topics.SelectedItem);
        }
        private void Render(HelpSection section)
        {
            article.Children.Clear();
            var heading=ProductTheme.Text(section?.Title??UiText.Get("No matching topics",language),26);heading.FontWeight=FontWeights.SemiBold;
            heading.SetResourceReference(TextBlock.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);article.Children.Add(heading);
            if(section==null)return;
            heading.Margin=new Thickness(0,0,0,20);
            foreach(var block in section.Blocks)
            {
                if(block.Kind=="shortcut")
                {
                    var row=new Grid {Margin=new Thickness(0,0,0,8)};
                    row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(190)});row.ColumnDefinitions.Add(new ColumnDefinition());
                    var key=HelpText(block.Label,13);key.FontFamily=new System.Windows.Media.FontFamily("Consolas");key.FontWeight=FontWeights.SemiBold;key.TextWrapping=TextWrapping.NoWrap;key.Margin=new Thickness(0);
                    var badge=new Border {Child=key,Padding=new Thickness(10,7,10,7),Margin=new Thickness(0,0,16,0),CornerRadius=new CornerRadius(4),BorderThickness=new Thickness(1),BorderBrush=System.Windows.Media.Brushes.Gray,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
                    row.Children.Add(badge);var description=HelpText(block.Text,14);description.Margin=new Thickness(0,6,0,6);Grid.SetColumn(description,1);row.Children.Add(description);article.Children.Add(row);
                }
                else if(block.Kind=="heading")
                {
                    var title=HelpText(block.Text,18);title.FontWeight=FontWeights.SemiBold;title.Margin=new Thickness(0,18,0,12);article.Children.Add(title);
                }
                else if(block.Kind=="item"||block.Kind=="step")
                {
                    var row=new Grid {Margin=new Thickness(0,0,0,10)};row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(block.Kind=="step"?30:20)});row.ColumnDefinitions.Add(new ColumnDefinition());
                    row.Children.Add(HelpText(block.Kind=="step"?block.Label+".":"•",14));var text=HelpText(block.Text,14);Grid.SetColumn(text,1);row.Children.Add(text);article.Children.Add(row);
                }
                else {var text=HelpText(block.Text,14);text.Margin=new Thickness(0,0,0,14);article.Children.Add(text);}
            }
        }
        private static TextBlock HelpText(string value,double size)
        {
            var text=ProductTheme.Text(value,size);text.TextWrapping=TextWrapping.Wrap;text.LineHeight=23;text.Margin=new Thickness(0);
            text.SetResourceReference(TextBlock.ForegroundProperty,EnvironmentColors.ToolWindowTextBrushKey);return text;
        }
    }
}
