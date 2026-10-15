using CodeAssist.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CodeAssist.Indexing
{
    public sealed class ProjectSyntaxFinding
    {
        public string File { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
    }
    public static class ProjectSyntaxInspection
    {
        public static IReadOnlyList<ProjectSyntaxFinding> Scan(string root, CancellationToken cancellation=default(CancellationToken))
        {
            cancellation.ThrowIfCancellationRequested();
            if(string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) throw new DirectoryNotFoundException(root);
            var results=new List<ProjectSyntaxFinding>();
            foreach(var file in PersistentSymbolIndex.InputFiles(root))
            {
                cancellation.ThrowIfCancellationRequested();
                string text;
                try { text=System.IO.File.ReadAllText(file); }
                catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
                { results.Add(new ProjectSyntaxFinding { File=file,Line=1,Column=1,Message="읽기 실패: "+ex.Message });continue; }
                var scanned=0;var line=1;var lineStart=0;
                foreach(var issue in CppDelimiterParser.FindDefiniteIssues(text))
                {
                    cancellation.ThrowIfCancellationRequested();
                    // Issue enumeration is source ordered by the parser.
                    if(issue.Start<scanned) {scanned=0;line=1;lineStart=0;}
                    while(scanned<issue.Start) {if(text[scanned++]=='\n'){line++;lineStart=scanned;}}
                    results.Add(new ProjectSyntaxFinding {File=file,Line=line,Column=issue.Start-lineStart+1,Message=issue.Message});
                }
            }
            return results;
        }
    }
}