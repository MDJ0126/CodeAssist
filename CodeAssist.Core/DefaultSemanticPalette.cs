using System;
namespace CodeAssist.Core {
 public static class DefaultSemanticPalette {
  public static string Resolve(string configured,string defaultColor,bool light) {
   if(!light||!string.Equals(Normalize(configured),Normalize(defaultColor),StringComparison.OrdinalIgnoreCase))return configured;
   switch(Normalize(defaultColor).ToUpperInvariant()) {
    case "#569CD6":return "#0000FF";
    case "#FFD700":return "#2B6C6E";
    case "#BDB76B":return "#000000";
    case "#FF8000":return "#A31515";
    case "#BD63C5":return "#6F008A";
    case "#B88020":return "#795000";
    case "#B9D7A2":return "#355D18";
    default:return configured;
   }
  }
  private static string Normalize(string value){if(value!=null&&value.Length==9&&value.StartsWith("#FF",StringComparison.OrdinalIgnoreCase))return "#"+value.Substring(3);return value??"";}
 }
}