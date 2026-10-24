using System.Globalization;
using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class MenuLocalizationTests {
 [Theory][InlineData("ko")][InlineData("ko-KR")][InlineData("en")][InlineData("en-US")]
 public void EveryRegisteredCommandHasTranslation(string language){Assert.Equal(74,UiCommandLabels.Sources.Count);foreach(var item in UiCommandLabels.Sources){var text=UiCommandLabels.Get(item.Key,language);Assert.False(string.IsNullOrWhiteSpace(text));if(language.StartsWith("ko"))Assert.NotEqual(item.Value,text);else Assert.Equal(item.Value,text);}}
 [Theory][InlineData("ko-KR","사용")][InlineData("en-US","Enable")]
 public void AutoUsesCurrentUiCulture(string culture,string expected){var previous=CultureInfo.CurrentUICulture;try{CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(culture);Assert.Equal(expected,UiText.Get("Enable","auto"));}finally{CultureInfo.CurrentUICulture=previous;}}
 [Fact] public void InfoBarAndContextLabelsHaveKorean(){foreach(var key in new[]{"Enable","Disable","Use per-file engine","Welcome","Options","Context: ","Definition: ","Global","Go","Inspection.Prompt","Inspection.Start"})Assert.NotEqual(key,UiText.Get(key,"ko-KR"));}
}
