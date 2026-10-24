using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    public sealed class SourceLinkRule
    {
        public string Name {get;set;} = "";
        public bool Enabled {get;set;} = true;
        public bool RegularExpression {get;set;}
        public string Pattern {get;set;} = "";
        public string Target {get;set;} = "";
    }
    public sealed class SourceLinkMatch
    {
        public string Name {get;internal set;}
        public int Start {get;internal set;}
        public int Length {get;internal set;}
        public Uri Uri {get;internal set;}
        public override string ToString()=>Name+" — "+Uri.AbsoluteUri;
    }
    public static class SourceLinks
    {
        private static readonly TimeSpan Timeout=TimeSpan.FromMilliseconds(30);
        private static Regex Compile(SourceLinkRule rule)
        {
            if(rule==null||string.IsNullOrWhiteSpace(rule.Pattern)||rule.Pattern.Length>2048)throw new ArgumentException("A source link pattern must contain 1–2048 characters.");
            var pattern=rule.Pattern;
            if(!rule.RegularExpression)
            {
                var position=pattern.IndexOf("{id}",StringComparison.Ordinal);
                if(position<0||pattern.IndexOf("{id}",position+4,StringComparison.Ordinal)>=0)throw new ArgumentException("A simple pattern must contain exactly one {id} placeholder.");
                pattern=Regex.Escape(pattern.Substring(0,position))+@"(?<id>[0-9]+)"+Regex.Escape(pattern.Substring(position+4));
            }
            return new Regex(pattern,RegexOptions.CultureInvariant,Timeout);
        }
        public static void Validate(SourceLinkRule rule)
        {
            var regex=Compile(rule);
            if(string.IsNullOrWhiteSpace(rule.Target)||rule.Target.Length>8192)throw new ArgumentException("A source link requires an http or https target.");
            var target=Regex.Replace(rule.Target,@"\{(?<key>[A-Za-z_][A-Za-z_0-9]*)\}",m=>{
                if(!regex.GetGroupNames().Contains(m.Groups["key"].Value))throw new ArgumentException("Unknown capture in link target: "+m.Value);
                return "1";
            },RegexOptions.CultureInvariant,Timeout);
            if(!Uri.TryCreate(target,UriKind.Absolute,out var uri)||(uri.Scheme!="http"&&uri.Scheme!="https"))throw new ArgumentException("Only absolute http and https targets are supported.");
        }
        public static IReadOnlyList<SourceLinkMatch> Find(string line,IEnumerable<SourceLinkRule> rules)
        {
            var result=new List<SourceLinkMatch>();if(line==null||line.Length>32768)return result;
            foreach(var rule in (rules??Enumerable.Empty<SourceLinkRule>()).Where(r=>r!=null&&r.Enabled).Take(100))
            {
                try
                {
                    Validate(rule);var regex=Compile(rule);
                    foreach(Match match in regex.Matches(line))
                    {
                        if(match.Length==0)continue;
                        var target=Regex.Replace(rule.Target,@"\{(?<key>[A-Za-z_][A-Za-z_0-9]*)\}",m=>Uri.EscapeDataString(match.Groups[m.Groups["key"].Value].Value),RegexOptions.CultureInvariant,Timeout);
                        if(Uri.TryCreate(target,UriKind.Absolute,out var uri)&&(uri.Scheme=="http"||uri.Scheme=="https"))result.Add(new SourceLinkMatch {Name=rule.Name,Start=match.Index,Length=match.Length,Uri=uri});
                        if(result.Count>=100)return result;
                    }
                }
                catch(ArgumentException){}catch(RegexMatchTimeoutException){}
            }
            return result;
        }
    }
}
