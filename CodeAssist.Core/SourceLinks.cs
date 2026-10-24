using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
namespace CodeAssist.Core {
 public enum SourceLinkProvider { Url, FileViewer, ExternalTool }
 public sealed class SourceLinkRule {
  public string Name {get;set;}="";public bool Enabled {get;set;}=true;public bool RegularExpression {get;set;}
  public string Pattern {get;set;}="";public string Target {get;set;}="";
  public string PluginId {get;set;}="URL";
  public SourceLinkProvider Provider {get;set;}
  public string Scope {get;set;}="User";public string Keywords {get;set;}="";public string Suffix {get;set;}="";
  public string ValueKind {get;set;}="Number";public string Arguments {get;set;}="";
  public SourceLinkRule Copy()=>new SourceLinkRule {Name=Name,Enabled=Enabled,RegularExpression=RegularExpression,Pattern=Pattern,Target=Target,Provider=Provider,PluginId=PluginId,Scope=Scope,Keywords=Keywords,Suffix=Suffix,ValueKind=ValueKind,Arguments=Arguments};
 }
 public sealed class SourceLinkMatch {
  public string Name {get;internal set;}public int Start {get;internal set;}public int Length {get;internal set;}
  public Uri Uri {get;internal set;}public SourceLinkProvider Provider {get;internal set;}
  public string TargetPath {get;internal set;}public string Arguments {get;internal set;}public int Line {get;internal set;}=1;
  public override string ToString()=>Name+" — "+(Uri?.AbsoluteUri??TargetPath);
 }
 [XmlRoot("SourceLinks")] public sealed class SourceLinkRuleCatalog {[XmlElement("Rule")]public List<SourceLinkRule> Rules {get;set;}=new List<SourceLinkRule>();}
 public static class SourceLinks {
  private static readonly TimeSpan Timeout=TimeSpan.FromMilliseconds(30);
  private const string Capture=@"\{(?<key>[A-Za-z_][A-Za-z_0-9]*)\}";
  private static Regex Compile(SourceLinkRule rule){
   if(rule==null)throw new ArgumentException("A source link rule is required.");var pattern=rule.Pattern;
   if(!string.IsNullOrWhiteSpace(rule.Keywords)){
    var words=rule.Keywords.Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries).Select(word=>Regex.Escape(word.Trim())).Where(word=>word.Length>0).ToArray();
    if(words.Length==0)throw new ArgumentException("At least one keyword is required.");
    pattern="(?:"+string.Join("|",words)+")"+(rule.ValueKind=="Number"?@"(?<id>[0-9]+)":@"(?<id>[^\s]+?)")+Regex.Escape(rule.Suffix??"")+(rule.ValueKind=="Number"||!string.IsNullOrEmpty(rule.Suffix)?"":@"(?=\s|$)");
   } else if(!rule.RegularExpression){
    if(string.IsNullOrWhiteSpace(pattern))throw new ArgumentException("A source link pattern is required.");
    var position=pattern.IndexOf("{id}",StringComparison.Ordinal);if(position<0||pattern.IndexOf("{id}",position+4,StringComparison.Ordinal)>=0)throw new ArgumentException("A simple pattern must contain exactly one {id} placeholder.");
    pattern=Regex.Escape(pattern.Substring(0,position))+(rule.ValueKind=="Number"?@"(?<id>[0-9]+)":@"(?<id>[^\s]+)")+Regex.Escape(pattern.Substring(position+4));
   }
   if(pattern==null||pattern.Length>2048)throw new ArgumentException("A source link pattern must contain 1–2048 characters.");
   return new Regex(pattern,RegexOptions.CultureInvariant,Timeout);
  }
  private static string Expand(string template,Func<string,string> capture,bool url){return Regex.Replace(template??"",Capture,m=>url?Uri.EscapeDataString(capture(m.Groups["key"].Value)):capture(m.Groups["key"].Value),RegexOptions.CultureInvariant,Timeout);}
  public static void Validate(SourceLinkRule rule){
   var regex=Compile(rule);if(!Enum.IsDefined(typeof(SourceLinkProvider),rule.Provider))throw new ArgumentException("Unknown source link provider.");
   if(string.IsNullOrWhiteSpace(rule.Target)||rule.Target.Length>8192||(rule.Arguments??"").Length>8192)throw new ArgumentException("A source link requires a target.");
   Func<string,string> value=key=>{if(!regex.GetGroupNames().Contains(key))throw new ArgumentException("Unknown capture in link target: "+key);return key=="url"&&rule.Target=="{url}"?"https://example.com":"1";};
   var target=rule.Provider==SourceLinkProvider.Url&&rule.Target=="{url}"?value("url"):Expand(Environment.ExpandEnvironmentVariables(rule.Target),value,rule.Provider==SourceLinkProvider.Url);
   ArgumentTokens(Expand(rule.Arguments,value,false)).ToArray();
   if(rule.Provider==SourceLinkProvider.Url){if(!Uri.TryCreate(target,UriKind.Absolute,out var uri)||(uri.Scheme!="http"&&uri.Scheme!="https"))throw new ArgumentException("Only absolute http and https targets are supported by the URL provider.");}
   else {if(!Path.IsPathRooted(target)||target.IndexOf('\0')>=0)throw new ArgumentException("A file or tool requires an absolute path.");Path.GetFullPath(target);if(rule.Provider==SourceLinkProvider.ExternalTool&&!target.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("External tools must be explicit .exe paths.");}
  }
  // Quote captures as individual Windows argv values. No shell is used for external tools.
  public static string QuoteArgument(string value){var result=new StringBuilder("\"");var slashes=0;foreach(var c in value??""){if(c=='\\'){slashes++;continue;}if(c=='\"'){result.Append('\\',slashes*2+1);result.Append(c);}else {result.Append('\\',slashes);result.Append(c);}slashes=0;}result.Append('\\',slashes*2);return result.Append('"').ToString();}
  private static IEnumerable<string> ArgumentTokens(string value){var token=new StringBuilder();var quote=false;var active=false;foreach(var c in value??""){if(c=='"'){quote=!quote;active=true;}else if(char.IsWhiteSpace(c)&&!quote){if(active){yield return token.ToString();token.Clear();active=false;}}else {token.Append(c);active=true;}}if(quote)throw new ArgumentException("Unclosed quote in tool arguments.");if(active)yield return token.ToString();}
  public static IReadOnlyList<SourceLinkMatch> Find(string line,IEnumerable<SourceLinkRule> rules){
   var result=new List<SourceLinkMatch>();if(line==null||line.Length>32768)return result;
   foreach(var rule in (rules??Enumerable.Empty<SourceLinkRule>()).Where(r=>r!=null&&r.Enabled).Take(100))try{
    Validate(rule);var regex=Compile(rule);foreach(Match match in regex.Matches(line)){
     if(match.Length==0)continue;Func<string,string> capture=key=>match.Groups[key].Value;
     var target=rule.Provider==SourceLinkProvider.Url&&rule.Target=="{url}"?capture("url"):Expand(Environment.ExpandEnvironmentVariables(rule.Target),capture,rule.Provider==SourceLinkProvider.Url);
     var item=new SourceLinkMatch {Name=rule.Name,Start=match.Index,Length=match.Length,Provider=rule.Provider};
     if(rule.Provider==SourceLinkProvider.Url){if(!Uri.TryCreate(target,UriKind.Absolute,out var uri)||(uri.Scheme!="http"&&uri.Scheme!="https"))continue;item.Uri=uri;}
     else {if(!Path.IsPathRooted(target))continue;item.TargetPath=Path.GetFullPath(target);item.Uri=new Uri(item.TargetPath);item.Arguments=string.Join(" ",ArgumentTokens(rule.Arguments).Select(token=>QuoteArgument(Expand(token,capture,false))));if(int.TryParse(match.Groups["line"].Value,out var number))item.Line=Math.Max(1,number);}
     result.Add(item);if(result.Count>=100)return result;
    }
   }catch(ArgumentException){}catch(RegexMatchTimeoutException){}catch(NotSupportedException){}catch(PathTooLongException){}
   return result;
  }
  public static SourceLinkRule Preset(string name){
   switch(name){
    case "FogBugz":return new SourceLinkRule {PluginId="FogBugz",Name="FogBugz cases",Pattern="case {id}",Target="https://YOUR-SITE.fogbugz.com/f/cases/{id}"};
    case "GitHub":return new SourceLinkRule {PluginId="GitHub",Name="GitHub issues",Pattern="issue #{id}",Target="https://github.com/OWNER/REPOSITORY/issues/{id}"};
    case "Jira":return new SourceLinkRule {PluginId="Jira",Name="Jira issues",Pattern="TASK-{id}",Target="https://YOUR-SITE.atlassian.net/browse/TASK-{id}"};
    case "File Viewer":return new SourceLinkRule {PluginId="File Viewer",Name="File Viewer",Provider=SourceLinkProvider.FileViewer,Pattern="file {id}",Target=Path.Combine(Path.GetTempPath(),"source-{id}.cpp")};
    case "External Tool":return new SourceLinkRule {PluginId="External Tool",Name="External Tool",Provider=SourceLinkProvider.ExternalTool,Pattern="tool {id}",Target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","notepad.exe"),Arguments="{id}"};
    default:return new SourceLinkRule {Name="URL navigation",RegularExpression=true,Pattern=@"(?<url>https?://[^\s]+)",Target="{url}"};
   }
  }
  public static IReadOnlyList<SourceLinkRule> ImportDirectory(string directory){
   var rules=new List<SourceLinkRule>();if(!Directory.Exists(directory))return rules;
   foreach(var file in Directory.EnumerateFiles(directory,"*.xml").OrderBy(path=>path,StringComparer.OrdinalIgnoreCase).Take(64)){
    if(new FileInfo(file).Length>1024*1024)throw new ArgumentException("Source link manifest exceeds 1 MB.");
    using(var reader=XmlReader.Create(file,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024*1024})){
     var catalog=(SourceLinkRuleCatalog)new XmlSerializer(typeof(SourceLinkRuleCatalog)).Deserialize(reader);
     foreach(var rule in catalog.Rules){Validate(rule);rules.Add(rule.Copy());if(rules.Count>=100)return rules;}
    }
   }return rules;
  }
 }
}
