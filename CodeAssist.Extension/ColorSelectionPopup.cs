using CodeAssist.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Automation;
namespace CodeAssist.Extension
{
    internal sealed class ColorSelectionPopup
    {
        private readonly Popup popup;
        private Border surface;
        private readonly StartupPreferences draft;
        private readonly Action<string> apply;
        private readonly string label;
        private Color selected;
        private bool updating;
        private TextBox hex;
        private Border preview;
        private readonly Slider[] hsv = new Slider[3], rgb = new Slider[3];
        private readonly TextBox[] channels = new TextBox[3];
        private readonly Border[] ramps = new Border[6];
        private static readonly string[] Palette = {
            "FFFFFF","FFC0C0","FFE0C0","FFFFC0","C0FFC0","C0FFFF","C0C0FF","FFC0FF",
            "E0E0E0","FF8080","FFC080","FFFF80","80FF80","80FFFF","8080FF","FF80FF",
            "C0C0C0","FF0000","FF8000","FFFF00","00FF00","00FFFF","0000FF","FF00FF",
            "808080","C00000","C06000","C0C000","00C000","00C0C0","0000C0","C000C0",
            "404040","800000","804000","808000","008000","008080","000080","800080",
            "000000","400000","603000","404000","004000","004040","000040","400040" };
        private static ControlTemplate sliderTemplate;
        private ColorSelectionPopup(FrameworkElement anchor,string label,string value,StartupPreferences draft,Action<string> apply)
        {
            this.draft=draft;this.apply=apply;this.label=label;
            selected=Read(value);
            popup=new Popup {PlacementTarget=anchor,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true};
            if(draft.CustomPaletteColors==null)draft.CustomPaletteColors=new List<string>();
            while(draft.CustomPaletteColors.Count<16)draft.CustomPaletteColors.Add("#FFFFFF");
            for(var i=0;i<16;i++)if(!PaletteColor.TryParse(draft.CustomPaletteColors[i],out _))draft.CustomPaletteColors[i]="#FFFFFF";
            ShowPalette(0);popup.IsOpen=true;
        }
        internal static void Show(FrameworkElement anchor,string label,string value,StartupPreferences draft,Action<string> apply)
        { new ColorSelectionPopup(anchor,label,value,draft,apply); }
        private string T(string text)=>UiText.Get(text,draft.UiLanguage);
        private static string Hex(Color c)=>"#"+c.R.ToString("X2")+c.G.ToString("X2")+c.B.ToString("X2");
        private static Color Read(string text){return PaletteColor.TryParse(text,out var bytes)?Color.FromRgb(bytes[0],bytes[1],bytes[2]):Colors.White;}
        private static Brush Solid(Color color){var b=new SolidColorBrush(color);b.Freeze();return b;}
        private void Choose(Color color){selected=color;apply(Hex(color));popup.IsOpen=false;}
        private void Surface(FrameworkElement content,double width)
        {
            // Keep the popup's presentation root stable while switching pages.
            if(surface==null)
            {
                surface=new Border {BorderBrush=Brushes.DimGray,BorderThickness=new Thickness(1),SnapsToDevicePixels=true};
                ProductTheme.Apply(surface);
                KeyboardNavigation.SetTabNavigation(surface,KeyboardNavigationMode.Cycle);
                surface.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){popup.IsOpen=false;e.Handled=true;}};
                popup.Child=surface;
            }
            surface.Width=width;surface.Child=content;
            ProductTheme.BrushFor(surface,Border.BackgroundProperty,content is Canvas?"ColorEditor":"Palette");
            AutomationProperties.SetName(surface,label+" "+T("Color palette"));
        }
        private Button SmallButton(string title,Action action,double width,double height)
        {
            var button=ProductTheme.Button(title,action);button.Width=width;button.Height=height;button.MinWidth=0;button.Padding=new Thickness(0);button.Margin=new Thickness(0);button.FontSize=12;
            return button;
        }
        private void ShowPalette(int page)
        {
            var body=new StackPanel();var tabs=new StackPanel {Orientation=Orientation.Horizontal,Height=24};
            var titles=new[]{"Palette","Web","System"};
            for(var i=0;i<3;i++){var n=i;var tab=SmallButton(T(titles[i]),()=>ShowPalette(n),i==0?59:54,23);if(i!=page)tab.Background=Brushes.Transparent;tabs.Children.Add(tab);}
            body.Children.Add(tabs);
            if(page==0)
            {
                var colors=new UniformGrid {Rows=8,Columns=8,Width=192,Height=192,Margin=new Thickness(3,5,3,5)};
                for(var i=0;i<64;i++)
                {
                    var slot=i-48;var color=Read(i<48?Palette[i]:draft.CustomPaletteColors[slot]);
                    var button=SmallButton("",()=>Choose(color),24,24);button.Background=Brushes.Transparent;
                    button.Content=new Border {Width=21,Height=21,Background=Solid(color),BorderBrush=Solid(Color.FromRgb(60,60,60)),BorderThickness=new Thickness(1)};
                    AutomationProperties.SetName(button,(slot>=0?T("Custom color")+" "+(slot+1)+" ":"")+Hex(color));button.ToolTip=Hex(color);
                    if(slot>=0){var customSlot=slot;button.PreviewMouseRightButtonUp+=(s,e)=>{ShowEditor(customSlot);e.Handled=true;};button.KeyDown+=(s,e)=>{if(e.Key==Key.Apps){ShowEditor(customSlot);e.Handled=true;}};}
                    colors.Children.Add(button);
                }
                body.Children.Add(colors);
            }
            else
            {
                var list=new StackPanel {Margin=new Thickness(0,4,0,0)};IEnumerable<KeyValuePair<string,Color>> values;
                if(page==1)values=typeof(Colors).GetProperties().Where(p=>p.PropertyType==typeof(Color)&&p.Name!="Transparent"&&p.Name!="Cyan").OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new KeyValuePair<string,Color>(p.Name=="Aqua"?"Cyan":p.Name,(Color)p.GetValue(null)));
                else
                {
                    var first=new[]{"ActiveBorder","ActiveCaption","WindowText","AppWorkspace","MenuBar","Window","ControlDark","ControlDarkDark"};
                    var properties=typeof(SystemColors).GetProperties().Where(p=>p.PropertyType==typeof(Color)).ToDictionary(p=>p.Name.Replace("Color",""));
                    values=first.Concat(properties.Keys.OrderBy(n=>n,StringComparer.Ordinal)).Distinct().Where(properties.ContainsKey).Select(n=>new KeyValuePair<string,Color>(n,(Color)properties[n].GetValue(null)));
                }
                foreach(var item in values)
                {
                    var color=item.Value;var name=Regex.Replace(item.Key,"([a-z])([A-Z])","$1 $2");var button=SmallButton("",()=>Choose(color),181,26);button.Background=Brushes.Transparent;button.HorizontalContentAlignment=HorizontalAlignment.Left;
                    var row=new StackPanel {Orientation=Orientation.Horizontal,Width=179,Height=26};row.Children.Add(new Border {Width=14,Height=14,Background=Solid(color),BorderBrush=Brushes.Black,BorderThickness=new Thickness(1),Margin=new Thickness(9,0,7,0)});var text=ProductTheme.Text(name,12);text.Margin=new Thickness(0);text.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(text);button.Content=row;AutomationProperties.SetName(button,name);list.Children.Add(button);
                }
                body.Children.Add(new ScrollViewer {Content=list,Height=202,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
            }
            Surface(body,200);
        }
        private void Put(Canvas canvas,FrameworkElement item,double x,double y){Canvas.SetLeft(item,x);Canvas.SetTop(item,y);canvas.Children.Add(item);}
        private TextBox Input(Canvas canvas,string name,double x,double y,double width)
        {
            var input=new TextBox {Width=width,Height=24,Padding=new Thickness(3,2,3,2),FontSize=12,BorderBrush=Brushes.DimGray};ProductTheme.BrushFor(input,Control.BackgroundProperty,"ColorInput");AutomationProperties.SetName(input,name);Put(canvas,input,x,y);return input;
        }
        private Slider Ramp(Canvas canvas,int number,string name,double x,double y,double width,double maximum)
        {
            var background=new Border {Width=width,Height=24};ramps[number]=background;Put(canvas,background,x,y);
            var slider=new Slider {Width=width+8,Height=28,Minimum=0,Maximum=maximum,IsMoveToPointEnabled=true,IsSnapToTickEnabled=maximum==255,TickFrequency=maximum==255?1:0.001,SmallChange=maximum==360?1:maximum==255?1:0.01,LargeChange=maximum==360?30:maximum==255?16:0.1,Background=Brushes.Transparent};
            if(sliderTemplate==null)sliderTemplate=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'><Grid Background='Transparent'><Track x:Name='PART_Track' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' Orientation='Horizontal'><Track.DecreaseRepeatButton><RepeatButton Command='Slider.DecreaseLarge' Opacity='0'/></Track.DecreaseRepeatButton><Track.IncreaseRepeatButton><RepeatButton Command='Slider.IncreaseLarge' Opacity='0'/></Track.IncreaseRepeatButton><Track.Thumb><Thumb Width='8' Height='28'><Thumb.Template><ControlTemplate TargetType='Thumb'><Grid Background='Transparent'><Path Data='M0,0 L8,0 L4,5 Z M4,5 L4,23 M0,28 L8,28 L4,23 Z' Fill='#DDDDDD' Stroke='#222222' StrokeThickness='1'/></Grid></ControlTemplate></Thumb.Template></Thumb></Track.Thumb></Track></Grid></ControlTemplate>");
            slider.Template=sliderTemplate;AutomationProperties.SetName(slider,T(name));Put(canvas,slider,x-4,y-2);return slider;
        }
        private void ShowEditor(int slot)
        {
            selected=Read(draft.CustomPaletteColors[slot]);var canvas=new Canvas {Width=248,Height=306};ProductTheme.Apply(canvas);ProductTheme.BrushFor(canvas,Panel.BackgroundProperty,"ColorEditor");
            Put(canvas,ProductTheme.Text(T("Hex"),12),10,14);hex=Input(canvas,T("Hex"),40,9,62);
            Put(canvas,ProductTheme.Text(T("Preview"),12),110,14);preview=new Border {Width=75,Height=22,CornerRadius=new CornerRadius(3)};Put(canvas,preview,162,10);
            Put(canvas,ProductTheme.Text(T("Hue"),12),10,47);hsv[0]=Ramp(canvas,0,"Hue",41,44,196,360);
            Put(canvas,ProductTheme.Text(T("Saturation"),12),10,83);hsv[1]=Ramp(canvas,1,"Saturation",77,80,160,1);
            Put(canvas,ProductTheme.Text(T("Brightness"),12),10,117);hsv[2]=Ramp(canvas,2,"Brightness",77,114,160,1);
            for(var i=0;i<3;i++)
            {
                var channel=i;var y=159+i*35;Put(canvas,ProductTheme.Text(new[]{"R","G","B"}[i],12),10,y+6);
                rgb[i]=Ramp(canvas,i+3,new[]{"Red","Green","Blue"}[i],25,y,169,255);channels[i]=Input(canvas,new[]{"Red","Green","Blue"}[i],198,y,39);
                channels[i].TextChanged+=(s,e)=>{if(updating||!byte.TryParse(channels[channel].Text,out var v))return;var c=selected;if(channel==0)c.R=v;else if(channel==1)c.G=v;else c.B=v;Update(c,channels[channel]);};
                rgb[i].ValueChanged+=(s,e)=>{if(updating)return;var c=selected;var v=(byte)Math.Round(rgb[channel].Value);if(channel==0)c.R=v;else if(channel==1)c.G=v;else c.B=v;Update(c,null);};
            }
            foreach(var slider in hsv)slider.ValueChanged+=(s,e)=>{if(updating)return;var c=PaletteColor.FromHsv(hsv[0].Value,hsv[1].Value,hsv[2].Value);Update(Color.FromRgb(c[0],c[1],c[2]),null);};
            hex.TextChanged+=(s,e)=>{if(!updating&&PaletteColor.TryParse(hex.Text,out var c))Update(Color.FromRgb(c[0],c[1],c[2]),hex);};
            var ok=SmallButton(T("OK"),()=>{if(!PaletteColor.TryParse(hex.Text,out var c)){hex.BorderBrush=Brushes.OrangeRed;hex.Focus();return;}var color=Color.FromRgb(c[0],c[1],c[2]);draft.CustomPaletteColors[slot]=Hex(color);Choose(color);},108,28);
            ProductTheme.BrushFor(ok,Control.BackgroundProperty,"Primary");var cancel=SmallButton(T("취소"),()=>popup.IsOpen=false,108,28);Put(canvas,ok,10,270);Put(canvas,cancel,129,270);
            // Canvas position must not determine keyboard traversal or let Tab escape to Options.
            var tabOrder=new Control[]{hex,hsv[0],hsv[1],hsv[2],rgb[0],channels[0],rgb[1],channels[1],rgb[2],channels[2],ok,cancel};
            for(var i=0;i<tabOrder.Length;i++)KeyboardNavigation.SetTabIndex(tabOrder[i],i);
            KeyboardNavigation.SetTabNavigation(canvas,KeyboardNavigationMode.Cycle);
            canvas.PreviewKeyDown+=(s,e)=>{
                if(e.Key!=Key.Tab)return;
                var index=Array.FindIndex(tabOrder,c=>c.IsKeyboardFocusWithin);
                var next=(index+((Keyboard.Modifiers&ModifierKeys.Shift)!=0?-1:1)+tabOrder.Length)%tabOrder.Length;
                if(tabOrder[next].Focus())e.Handled=true;
            };
            Update(selected,null);Surface(canvas,250);hex.Focus();hex.SelectAll();
        }
        private void Update(Color color,TextBox origin)
        {
            updating=true;
            try
            {
                selected=color;var values=PaletteColor.ToHsv(color.R,color.G,color.B);if((values[1]==0||values[0]==0&&hsv[0].Value==360)&&hsv[0]!=null)values[0]=hsv[0].Value;
                for(var i=0;i<3;i++)hsv[i].Value=values[i];
                if(origin!=hex)hex.Text=Hex(color).Substring(1);var bytes=new[]{color.R,color.G,color.B};
                for(var i=0;i<3;i++){rgb[i].Value=bytes[i];if(origin!=channels[i])channels[i].Text=bytes[i].ToString(CultureInfo.InvariantCulture);}
                preview.Background=Solid(color);
                var rainbow=new LinearGradientBrush();rainbow.StartPoint=new Point(0,0.5);rainbow.EndPoint=new Point(1,0.5);var stops=new[]{Colors.Red,Colors.Yellow,Colors.Lime,Colors.Cyan,Colors.Blue,Colors.Magenta,Colors.Red};for(var i=0;i<stops.Length;i++)rainbow.GradientStops.Add(new GradientStop(stops[i],i/6.0));ramps[0].Background=rainbow;
                var gray=PaletteColor.FromHsv(values[0],0,values[2]);var saturated=PaletteColor.FromHsv(values[0],1,values[2]);var brightest=PaletteColor.FromHsv(values[0],values[1],1);
                ramps[1].Background=new LinearGradientBrush(Color.FromRgb(gray[0],gray[1],gray[2]),Color.FromRgb(saturated[0],saturated[1],saturated[2]),0);ramps[2].Background=new LinearGradientBrush(Colors.Black,Color.FromRgb(brightest[0],brightest[1],brightest[2]),0);
                ramps[3].Background=new LinearGradientBrush(Colors.Black,Colors.Red,0);
                ramps[4].Background=new LinearGradientBrush(Colors.Black,Colors.Lime,0);
                ramps[5].Background=new LinearGradientBrush(Colors.Black,Colors.Blue,0);
            }
            finally {updating=false;}
        }
    }
}
