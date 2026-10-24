using System;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    public static class FileExtensionSettings
    {
        public static string[] Parse(string value)
        {
            var extensions=(value??"").Split(new[]{';',',',' ','\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(e=>e.StartsWith(".",StringComparison.Ordinal)?e:"."+e).Select(e=>e.ToLowerInvariant()).Distinct().OrderBy(e=>e,StringComparer.Ordinal).ToArray();
            if(extensions.Length>32||extensions.Any(e=>!Regex.IsMatch(e,@"^\.[a-z0-9_+-]{1,15}$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20))))throw new ArgumentException("Use up to 32 extensions such as .ipp;.tpp;.cuh");return extensions;
        }
    }
}
