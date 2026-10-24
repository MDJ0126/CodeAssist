using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeAssist.ParityRunner {
 internal static class NativeCommandNames {
  internal static bool IsVa(string name){return name!=null&&(name.StartsWith("VAssistX.",StringComparison.OrdinalIgnoreCase)||name.StartsWith("VisualAssist.",StringComparison.OrdinalIgnoreCase));}
  internal static string ResolveRegistered(string requested,Func<string,string> lookup){
   if(string.IsNullOrEmpty(requested))throw new ArgumentException("Missing requested command");
   var exact=lookup(requested);
   if(exact!=null)return Resolve(requested,new[]{exact});
   if(!IsVa(requested))throw new InvalidOperationException("Requested command absent: "+requested);
   var alternate=(requested.StartsWith("VAssistX.",StringComparison.OrdinalIgnoreCase)?"VisualAssist.":"VAssistX.")+requested.Substring(requested.IndexOf('.')+1);
   var alias=lookup(alternate);
   if(alias==null||!string.Equals(alias,alternate,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("VA command alias absent or mismatched: "+requested);
   return alias;
  }
  internal static string Resolve(string requested,IEnumerable<string> names){
   if(string.IsNullOrEmpty(requested))throw new ArgumentException("Missing requested command");
   var available=names.Where(n=>!string.IsNullOrEmpty(n)).Distinct(StringComparer.Ordinal).ToArray();
   var exact=available.Where(n=>string.Equals(n,requested,StringComparison.OrdinalIgnoreCase)).ToArray();
   if(exact.Length==1)return exact[0];if(exact.Length>1)throw new InvalidOperationException("Ambiguous command name: "+requested);
   if(IsVa(requested)){
    var dot=requested.IndexOf('.');var alternate=(requested.StartsWith("VAssistX.",StringComparison.OrdinalIgnoreCase)?"VisualAssist.":"VAssistX.")+requested.Substring(dot+1);
    var aliases=available.Where(n=>string.Equals(n,alternate,StringComparison.OrdinalIgnoreCase)).ToArray();
    if(aliases.Length==1)return aliases[0];if(aliases.Length>1)throw new InvalidOperationException("Ambiguous VA alias: "+requested);
   }
   throw new InvalidOperationException("Requested command absent from live inventory: "+requested);
  }
 }
}
