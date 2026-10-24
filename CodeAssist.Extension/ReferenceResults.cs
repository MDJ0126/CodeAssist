using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using System.Collections.Generic;
using CodeAssist.Core;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CodeAssist.Extension
{
    [Guid("e4c882bf-d127-4272-b93c-febd6a79f503")]
    public sealed class ReferenceResultsToolWindow : ToolWindowPane
    {
        public ReferenceResultsToolWindow() : base(null) { Caption = ReferenceResultText.WindowTitle(StartupUi.Preferences.UiLanguage); Content = new ReferencePane(); }
    }
    internal sealed class ReferencePane : UserControl
    {
        private readonly TreeView tree=new TreeView {BorderThickness=new Thickness(0),Padding=new Thickness(2)};
        private readonly TextBlock title=new TextBlock {Margin=new Thickness(5,3,5,3),TextTrimming=TextTrimming.CharacterEllipsis};
        private readonly TextBox filter=new TextBox {Width=246,Height=22,Margin=new Thickness(4,1,4,1),VerticalContentAlignment=VerticalAlignment.Center};
        private readonly List<TreeViewItem> leaves=new List<TreeViewItem>();
        private Button refreshButton;private readonly LatestOperationGate refreshGate=new LatestOperationGate();private ReferenceSearchState state;
        private AsyncPackage package;
        internal ReferencePane()
        {
            var panel=new DockPanel();ProductTheme.Apply(panel);ProductTheme.Apply(tree);
            title.SetResourceReference(TextBlock.ForegroundProperty,Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowTextBrushKey);
            var toolbar=new DockPanel {Height=27};DockPanel.SetDock(toolbar,Dock.Top);panel.Children.Add(toolbar);
            var buttons=new StackPanel {Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Left);toolbar.Children.Add(buttons);
            foreach(var previous in new[]{false,true}) {
                var back=previous;var label=UiText.Get(back?"Previous reference":"Next reference",StartupUi.Preferences.UiLanguage);
                var button=ProductTheme.Button("",()=>Move(back?-1:1));button.Content=ProductTheme.Icon(back?"PreviousReference":"NextReference",16,"Link");button.Width=24;button.MinWidth=24;button.Height=23;button.Padding=new Thickness(3);button.Margin=new Thickness(0);button.ToolTip=label;
                System.Windows.Automation.AutomationProperties.SetName(button,label);buttons.Children.Add(button);
            }
            refreshButton=ToolbarButton("Refresh references","Refresh",()=>Refresh());buttons.Children.Add(refreshButton);
            buttons.Children.Add(ToolbarButton("Clone Results","Clone",()=>{if(package!=null)package.JoinableTaskFactory.RunAsync(CloneResultsAsync).FileAndForget("CodeAssist/CloneReferences");}));
            System.Windows.Automation.AutomationProperties.SetName(filter,UiText.Get("Filter references",StartupUi.Preferences.UiLanguage));filter.ToolTip=UiText.Get("Filter references",StartupUi.Preferences.UiLanguage);
            DockPanel.SetDock(filter,Dock.Left);toolbar.Children.Add(filter);
            var clear=ProductTheme.Button("",()=>filter.Clear());clear.Content=ProductTheme.Icon("Close",12);clear.Width=20;clear.MinWidth=20;clear.Height=22;clear.Padding=new Thickness(2);clear.Margin=new Thickness(0,1,4,1);clear.ToolTip=UiText.Get("Clear filter",StartupUi.Preferences.UiLanguage);System.Windows.Automation.AutomationProperties.SetName(clear,(string)clear.ToolTip);DockPanel.SetDock(clear,Dock.Left);toolbar.Children.Add(clear);toolbar.Children.Add(title);panel.Children.Add(tree);Content=panel;
            filter.TextChanged+=(sender,args)=>Build();
            Loaded+=(sender,args)=>StartupUi.SettingsApplied+=Build;Unloaded+=(sender,args)=>StartupUi.SettingsApplied-=Build;
            GotKeyboardFocus+=(sender,args)=>ReferenceResults.ActivePane=this;PreviewMouseDown+=(sender,args)=>ReferenceResults.ActivePane=this;Unloaded+=(sender,args)=>{if(ReferenceEquals(ReferenceResults.ActivePane,this))ReferenceResults.ActivePane=null;};
            tree.MouseDoubleClick+=(sender,args)=>{if(tree.SelectedItem is TreeViewItem item&&item.Tag is NavigationEntry entry)Navigate(entry);};
            tree.PreviewKeyDown+=(sender,args)=>{if(args.Key==Key.Enter&&tree.SelectedItem is TreeViewItem item&&item.Tag is NavigationEntry entry){Navigate(entry);args.Handled=true;}else if(args.Key==Key.F8){Move((Keyboard.Modifiers&ModifierKeys.Shift)!=0?-1:1);args.Handled=true;}};
        }
        internal bool IsBound=>package!=null;
        internal System.Threading.Tasks.Task CloneResultsAsync()=>ReferenceResults.CloneAsync(package,state,filter.Text);
        private static Button ToolbarButton(string label,string icon,Action action)
        {
            var text=UiText.Get(label,StartupUi.Preferences.UiLanguage);var button=ProductTheme.Button("",action);
            button.Content=ProductTheme.Icon(icon,16,"Link");button.Width=24;button.MinWidth=24;button.Height=23;button.Padding=new Thickness(3);button.Margin=new Thickness(0);button.ToolTip=text;
            System.Windows.Automation.AutomationProperties.SetName(button,text);return button;
        }
        private void Refresh()
        {
            if(package==null||state?.Refresh==null)return;
            var operation=refreshGate.TryBegin();if(operation==null)return;
            var revision=state;
            refreshButton.IsEnabled=false;
            package.JoinableTaskFactory.RunAsync(async()=>{
                try {var result=await revision.Refresh();await package.JoinableTaskFactory.SwitchToMainThreadAsync();if(!refreshGate.IsCurrent(operation)||!ReferenceEquals(state,revision))return;if(result==null)throw new InvalidOperationException(UiText.Get("Reference refresh failed",StartupUi.Preferences.UiLanguage));state=revision.WithResults(result);Build();}
                catch(Exception error){await package.JoinableTaskFactory.SwitchToMainThreadAsync();if(refreshGate.IsCurrent(operation)&&ReferenceEquals(state,revision)){title.Text=UiText.Get("Reference refresh failed",StartupUi.Preferences.UiLanguage);title.ToolTip=error.Message;}}
                finally {await package.JoinableTaskFactory.SwitchToMainThreadAsync();if(refreshGate.TryComplete(operation))refreshButton.IsEnabled=state?.Refresh!=null;}
            }).FileAndForget("CodeAssist/RefreshReferences");
        }
        private static TreeViewItem Row(string caption,string icon,string role,string[] terms=null)
        {
            var label=new StackPanel {Orientation=Orientation.Horizontal};var glyph=ProductTheme.Icon(icon,16,role);glyph.Margin=new Thickness(0,0,4,0);label.Children.Add(glyph);var text=new TextBlock {VerticalAlignment=VerticalAlignment.Center};
            if(StartupUi.Preferences.ColoringEnabled&&StartupUi.Preferences.ColorViews&&Enum.TryParse<SymbolKind>(role,out var kind)){var brush=RecommendedColors.SymbolBrush(kind);if(brush!=null)text.Foreground=brush;}
            terms=terms??Array.Empty<string>();
            var highlighted=new bool[caption.Length];
            foreach(var term in terms)for(var start=0;start<=caption.Length-term.Length;){var found=caption.IndexOf(term,start,StringComparison.OrdinalIgnoreCase);if(found<0)break;for(var n=found;n<found+term.Length;n++)highlighted[n]=true;start=found+Math.Max(1,term.Length);}
            for(var start=0;start<caption.Length;){var end=start+1;while(end<caption.Length&&highlighted[end]==highlighted[start])end++;var run=new Run(caption.Substring(start,end-start));if(highlighted[start]){run.Background=ProductTheme.Accent;run.Foreground=Brushes.Black;}text.Inlines.Add(run);start=end;}
            label.Children.Add(text);
            var item=new TreeViewItem {Header=label,IsExpanded=true,Padding=new Thickness(0,2,0,2)};System.Windows.Automation.AutomationProperties.SetName(item,caption);return item;
        }
        private void Build()
        {
            var selected=(tree.SelectedItem as TreeViewItem)?.Tag as NavigationEntry;
            tree.Items.Clear();leaves.Clear();if(state==null)return;
            var query=ReferenceFilter.Compile(filter.Text);var terms=query.Terms.ToArray();
            var root=Row(string.IsNullOrEmpty(state.Project)?UiText.Get("References",StartupUi.Preferences.UiLanguage):state.Project,"Project","Unknown");tree.Items.Add(root);
            foreach(var group in state.Display.Rows.GroupBy(row=>row.File,StringComparer.OrdinalIgnoreCase))
            {
                var file=Row(group.Key,"File","Unknown");
                foreach(var entry in group)
                {
                    if(!query.Matches(entry.Caption))continue;
                    var row=Row(entry.Caption,"Reference",state.Entry(entry.Index).Kind,terms);row.Tag=state.Entry(entry.Index);file.Items.Add(row);leaves.Add(row);
                    if(ReferenceEquals(row.Tag,selected))row.IsSelected=true;
                }
                if(file.Items.Count>0)root.Items.Add(file);
            }
            title.Text=state.Description+" — "+leaves.Count+" / "+state.Count;title.ToolTip=title.Text;
        }
        private void Move(int direction)=>MoveAt(direction,null,0,0);
        internal bool MoveAt(int direction,string currentFile,int line,int column)
        {
            if(package==null||state==null||leaves.Count==0)return false;
            var rows=leaves.Select(item=>(NavigationEntry)item.Tag).Select(item=>new ReferenceLocation(item.File,item.Line,item.Column)).ToArray();
            var index=ReferenceResultNavigation.Next(rows,leaves.IndexOf(tree.SelectedItem as TreeViewItem),currentFile,line,column,state.Symbol.Length,direction);
            var selected=leaves[index];selected.IsSelected=true;selected.BringIntoView();Navigate((NavigationEntry)selected.Tag);return true;
        }
        private void Navigate(NavigationEntry entry)
        {
            if(package==null)return;
            var selectedSymbol=state.Symbol;
            package.JoinableTaskFactory.RunAsync(async()=>{
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;if(dte==null)return;
                dte.ItemOperations.OpenFile(entry.File,EnvDTE.Constants.vsViewKindTextView);var selection=dte.ActiveDocument.Selection as EnvDTE.TextSelection;if(selection==null)return;
                selection.MoveToLineAndOffset(entry.Line,entry.Column,false);
                if(selectedSymbol.Length>0&&selection.ActivePoint.CreateEditPoint().GetText(selectedSymbol.Length)==selectedSymbol)selection.CharRight(true,selectedSymbol.Length);
            }).FileAndForget("CodeAssist/ReferenceLocation");
        }
        internal void Bind(AsyncPackage package,ReferenceSearchState state,string query="")
        {
            refreshGate.Reset();this.package=package;this.state=state;refreshButton.IsEnabled=state.Refresh!=null;
            if(filter.Text==query)Build();else filter.Text=query??"";
            ReferenceResults.ActivePane=this;
        }
    }
    internal static class ReferenceResults
    {
        internal static ReferencePane ActivePane;
        internal static bool CanMove=>ActivePane?.IsVisible==true&&ActivePane.IsBound;
        internal static bool TryMoveAt(string file,int line,int column,int direction)=>ActivePane?.IsVisible==true&&ActivePane.MoveAt(direction,file,line,column);
        private static int clones;private static ReferenceSearchState last;
        internal static async System.Threading.Tasks.Task PublishAsync(AsyncPackage package,string symbol,NavigationEntry[] entries,bool compiler,Func<Task<NavigationEntry[]>> refresh=null,Action validate=null)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            validate?.Invoke();
            var published=ReferenceSearchState.Capture(ReferenceResultText.Description(symbol,compiler,StartupUi.Preferences.UiLanguage),symbol,entries,refresh);
            var pane=await package.ShowToolWindowAsync(typeof(ReferenceResultsToolWindow),0,true,package.DisposalToken);
            validate?.Invoke();last=published;
            if(pane?.Content is ReferencePane content)content.Bind(package,published);
        }
        internal static async System.Threading.Tasks.Task CloneAsync(AsyncPackage package,ReferenceSearchState state,string query)
        {
            if(state==null)return;await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            var pane=await package.ShowToolWindowAsync(typeof(ReferenceResultsToolWindow),Interlocked.Increment(ref clones),true,package.DisposalToken);
            if(pane?.Content is ReferencePane content){pane.Caption=ReferenceResultText.CloneTitle(state.Symbol,StartupUi.Preferences.UiLanguage);content.Bind(package,state,query);}
        }
        internal static async System.Threading.Tasks.Task ShowAsync(AsyncPackage package,bool clone)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if(clone){if(ActivePane?.IsBound==true)await ActivePane.CloneResultsAsync();else await CloneAsync(package,last,"");return;}
            var pane=await package.ShowToolWindowAsync(typeof(ReferenceResultsToolWindow),0,true,package.DisposalToken);
            if(pane?.Content is ReferencePane content&&!content.IsBound&&last!=null)content.Bind(package,last);
        }
    }
}
