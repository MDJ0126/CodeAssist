using System;using CodeAssist.Core;using Xunit;
public class DefaultSemanticPaletteTests {
 [Theory][InlineData("#FFD700")][InlineData("#BDB76B")][InlineData("#FF8000")][InlineData("#BD63C5")]
 public void LightDefaultsHaveReadableContrastAndDarkValuesArePreserved(string value){var result=DefaultSemanticPalette.Resolve("#FF"+value.Substring(1),value,true);double luminance=0;var weights=new[]{.2126,.7152,.0722};for(int i=0;i<3;i++){double channel=Convert.ToInt32(result.Substring(1+2*i,2),16)/255.0;channel=channel<=.04045?channel/12.92:Math.Pow((channel+.055)/1.055,2.4);luminance+=weights[i]*channel;}Assert.True(1.05/(luminance+.05)>=4.5);Assert.Equal(value,DefaultSemanticPalette.Resolve(value,value,false));}
 [Theory][InlineData("#123456")][InlineData("#FF8000")]
 public void ExplicitTypeCustomizationIsPreserved(string value){Assert.Equal(value,DefaultSemanticPalette.Resolve(value,"#FFD700",true));}
}