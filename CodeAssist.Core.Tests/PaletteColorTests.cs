using CodeAssist.Core;
using Xunit;
public class PaletteColorTests
{
 [Theory][InlineData("E65AA0",230,90,160)][InlineData("#50C8E6",80,200,230)][InlineData("000000",0,0,0)][InlineData("ffffff",255,255,255)]
 public void HexAndHsvRoundTripExactBytes(string hex,byte r,byte g,byte b){Assert.True(PaletteColor.TryParse(hex,out var rgb));Assert.Equal(new[]{r,g,b},rgb);var hsv=PaletteColor.ToHsv(r,g,b);Assert.Equal(rgb,PaletteColor.FromHsv(hsv[0],hsv[1],hsv[2]));}
 [Theory][InlineData("123")][InlineData("gg0000")][InlineData("#FFFFFFFF")][InlineData(null)]
 public void InvalidHexDoesNotBecomeAColor(string input){Assert.False(PaletteColor.TryParse(input,out _));}
 [Theory][InlineData(0,255,0,0)][InlineData(120,0,255,0)][InlineData(240,0,0,255)][InlineData(360,255,0,0)]
 public void HueEndpointsAreStable(double hue,byte r,byte g,byte b){Assert.Equal(new[]{r,g,b},PaletteColor.FromHsv(hue,1,1));}
 [Fact]public void OptionsCancelDraftDoesNotMutateSavedCustomColors(){var original=new StartupPreferences();original.CustomPaletteColors.Add("#E65AA0");var draft=original.Copy();draft.CustomPaletteColors[0]="#50C8E6";Assert.Equal("#E65AA0",original.CustomPaletteColors[0]);Assert.Equal("#50C8E6",draft.Copy().CustomPaletteColors[0]);}
}
