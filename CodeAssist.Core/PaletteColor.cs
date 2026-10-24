using System;
using System.Globalization;
namespace CodeAssist.Core
{
    public static class PaletteColor
    {
        public static bool TryParse(string text, out byte[] rgb)
        {
            rgb = null; var value = (text ?? "").Trim().TrimStart('#');
            if (value.Length != 6 || !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex)) return false;
            rgb = new[] { (byte)(hex >> 16), (byte)(hex >> 8), (byte)hex }; return true;
        }
        public static double[] ToHsv(byte r, byte g, byte b)
        {
            var red=r/255.0; var green=g/255.0; var blue=b/255.0;
            var max=Math.Max(red,Math.Max(green,blue)); var min=Math.Min(red,Math.Min(green,blue)); var delta=max-min;
            var hue=delta==0?0:max==red?60*((green-blue)/delta%6):max==green?60*((blue-red)/delta+2):60*((red-green)/delta+4);
            if(hue<0)hue+=360; return new[] { hue,max==0?0:delta/max,max };
        }
        public static byte[] FromHsv(double hue,double saturation,double value)
        {
            if(double.IsNaN(hue)||double.IsInfinity(hue)||double.IsNaN(saturation)||double.IsInfinity(saturation)||double.IsNaN(value)||double.IsInfinity(value))throw new ArgumentOutOfRangeException();
            hue=(hue%360+360)%360;saturation=Math.Max(0,Math.Min(1,saturation));value=Math.Max(0,Math.Min(1,value));
            var chroma=value*saturation;var x=chroma*(1-Math.Abs(hue/60%2-1));var m=value-chroma;double r=0,g=0,b=0;
            if(hue<60){r=chroma;g=x;}else if(hue<120){r=x;g=chroma;}else if(hue<180){g=chroma;b=x;}else if(hue<240){g=x;b=chroma;}else if(hue<300){r=x;b=chroma;}else{r=chroma;b=x;}
            return new[]{(byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255)};
        }
    }
}
