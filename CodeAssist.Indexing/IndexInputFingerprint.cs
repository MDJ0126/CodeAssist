using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CodeAssist.Indexing
{
    public static class IndexInputFingerprint
    {
        public static string Create(IEnumerable<string> files)
        {
            var inputs = files.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => { var info = new FileInfo(path); return Path.GetFullPath(path).ToUpperInvariant() + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks; });
            using (var hash = SHA256.Create())
                return "INPUT1:" + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", inputs)))).Replace("-", "");
        }
    }
}
