using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using CodeAssist.Core;
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
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost host,IWpfTextViewMargin parent)
        {
            Documents.TryGetTextDocument(host.TextView.TextDataModel.DocumentBuffer,out var document);
            return new ContextBarMargin(host.TextView,document?.FilePath);
        }
    }
    internal sealed class ContextBarMargin : IWpfTextViewMargin
    {
        private const string Name="CodeAssist Context Bar";
        private readonly IWpfTextView view;
        private readonly DockPanel panel=new DockPanel { Height=28,LastChildFill=true };
        private readonly ComboBox methods=CreateMethodSelector();
        internal static ComboBox CreateMethodSelector()
        {
            // An explicit item template also formats the closed selection in our themed ComboBox.
            var label = new FrameworkElementFactory(typeof(TextBlock));
            label.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(SemanticToken.Name)));
            return new ComboBox { Width=220, Margin=new Thickness(6,1,2,1), ItemTemplate=new DataTemplate { VisualTree=label } };
        }
        private readonly Action settingsChanged;
        private bool disposed;
        private ITextSnapshot methodSnapshot;
        internal ContextBarMargin(IWpfTextView view,string path)
        {
            this.view=view;ProductTheme.Apply(panel);
            methods.ItemsSource=new[] {new SemanticToken("메서드 선택",0,0,SemanticTokenKind.Function)};methods.SelectedIndex=0;
            var go=ProductTheme.Button("Go",()=> {ThreadHelper.ThrowIfNotOnUIThread();StartupUi.Execute(0x100);});go.MinWidth=36;go.Padding=new Thickness(6,2,6,2);DockPanel.SetDock(go,Dock.Right);panel.Children.Add(go);
            var corresponding=ProductTheme.Button("⇄",()=> {ThreadHelper.ThrowIfNotOnUIThread();StartupUi.Execute(0x105);});corresponding.MinWidth=30;corresponding.Padding=new Thickness(6,2,6,2);corresponding.ToolTip="대응 파일 (Alt+O)";DockPanel.SetDock(corresponding,Dock.Right);panel.Children.Add(corresponding);
            DockPanel.SetDock(methods,Dock.Right);panel.Children.Add(methods);
            panel.Children.Add(new TextBlock {Text="➜  "+(path??"CodeAssist"),TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,0,0),ToolTip=path});
            methods.DropDownOpened+=(s,e)=> {
                methodSnapshot=view.TextSnapshot;
                methods.ItemsSource=CppSemanticParser.Parse(methodSnapshot.GetText()).Where(item=>item.Kind==SemanticTokenKind.Function).GroupBy(item=>item.Name).Select(group=>group.First()).ToArray();
            };
            methods.SelectionChanged+=(s,e)=> {
                if(ReferenceEquals(methodSnapshot,view.TextSnapshot) && methods.SelectedItem is SemanticToken token && token.Length>0 && token.Start+token.Length<=view.TextSnapshot.Length) {
                    var span=new SnapshotSpan(view.TextSnapshot,token.Start,token.Length);
                    view.Selection.Select(span,false);view.Caret.MoveTo(span.Start);view.ViewScroller.EnsureSpanVisible(span);
                }
            };
            settingsChanged=()=>panel.Visibility=StartupUi.Preferences.ContextBarEnabled?Visibility.Visible:Visibility.Collapsed;
            StartupUi.SettingsApplied+=settingsChanged;settingsChanged();
        }
        public FrameworkElement VisualElement=>panel;
        public double MarginSize=>panel.Visibility==Visibility.Visible?28:0;
        public bool Enabled=>!disposed && StartupUi.Preferences.ContextBarEnabled;
        public ITextViewMargin GetTextViewMargin(string name)=>string.Equals(name,Name,StringComparison.OrdinalIgnoreCase)?this:null;
        public void Dispose(){if(disposed)return;disposed=true;StartupUi.SettingsApplied-=settingsChanged;}
    }
}