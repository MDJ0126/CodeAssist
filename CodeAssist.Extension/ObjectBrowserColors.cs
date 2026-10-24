using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
namespace CodeAssist.Extension
{
    // Paint only the Object Browser's own WPF subtree; restore every original value.
    internal sealed class ObjectBrowserColors:IDisposable
    {
        private readonly ProjectIndexService index;private readonly NativeBrowserColors native;private IntPtr browserFrame;
        private readonly EnvDTE80.DTE2 dte;
        private readonly EnvDTE.WindowEvents windows;
        private readonly DispatcherTimer timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(500)};
        private sealed class Original {internal DependencyObject Node;internal object Value;internal BindingBase Binding;}
        private readonly List<Original> originals=new List<Original>();
        internal ObjectBrowserColors(EnvDTE80.DTE2 dte,ProjectIndexService index)
        {
            this.dte=dte;this.index=index;native=new NativeBrowserColors(index);windows=dte.Events.WindowEvents;windows.WindowActivated+=Activated;timer.Tick+=Tick;StartupUi.SettingsApplied+=Changed;
        }
        private void Activated(EnvDTE.Window active,EnvDTE.Window previous)
        {
            var caption=active?.Caption??"";
            if(caption.IndexOf("Object Browser",StringComparison.OrdinalIgnoreCase)>=0||caption.Contains("개체 브라우저")){browserFrame=(IntPtr)active.HWnd;if(browserFrame==(IntPtr)dte.MainWindow.HWnd)browserFrame=IntPtr.Zero;native.Observe(browserFrame,StartupUi.Preferences.ColorObjectBrowser);timer.Start();}else timer.Stop();
        }
        private void Changed(){Restore();native.Observe(browserFrame,StartupUi.Preferences.ColorObjectBrowser);if(StartupUi.Preferences.ColorObjectBrowser)timer.Start();else timer.Stop();}
        private void Restore()
        {
            foreach(var item in originals){if(item.Binding!=null)BindingOperations.SetBinding(item.Node,TextBlock.ForegroundProperty,item.Binding);else if(item.Value==DependencyProperty.UnsetValue)item.Node.ClearValue(TextBlock.ForegroundProperty);else item.Node.SetValue(TextBlock.ForegroundProperty,item.Value);}
            originals.Clear();
        }
        private void Tick(object sender,EventArgs args)
        {
            if(!StartupUi.Preferences.ColoringEnabled||!StartupUi.Preferences.ColorObjectBrowser){Restore();return;}
            if(index==null)return;
            var pending=new Stack<Tuple<DependencyObject,bool>>();
            foreach(PresentationSource source in PresentationSource.CurrentSources)if(source.RootVisual!=null)pending.Push(Tuple.Create((DependencyObject)source.RootVisual,false));
            var visited=0;
            while(pending.Count>0&&visited++<8192)
            {
                var item=pending.Pop();var node=item.Item1;
                var browser=item.Item2||node.GetType().FullName.IndexOf("ObjectBrowser",StringComparison.OrdinalIgnoreCase)>=0;
                if(browser&&node is TextBlock text&&!string.IsNullOrWhiteSpace(text.Text))
                {
                    var name=text.Text.Split('(',' ','\t',':').FirstOrDefault();var symbols=index.Index.FindDefinitions(name??"");
                    if(symbols.Count>0){var brush=RecommendedColors.SymbolBrush(symbols[0].Kind);if(brush!=null){if(!originals.Any(o=>ReferenceEquals(o.Node,text)))originals.Add(new Original {Node=text,Value=text.ReadLocalValue(TextBlock.ForegroundProperty),Binding=BindingOperations.GetBindingBase(text,TextBlock.ForegroundProperty)});text.SetCurrentValue(TextBlock.ForegroundProperty,brush);}}
                }
                for(var i=VisualTreeHelper.GetChildrenCount(node)-1;i>=0;i--)pending.Push(Tuple.Create(VisualTreeHelper.GetChild(node,i),browser));
            }
        }
        public void Dispose(){native.Dispose();timer.Stop();timer.Tick-=Tick;windows.WindowActivated-=Activated;StartupUi.SettingsApplied-=Changed;Restore();}
    }
}
