using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeAssist.Core;
using Microsoft.VisualStudio.Shell;
namespace CodeAssist.Extension
{
    internal sealed class ReferenceSearchState
    {
        private readonly NavigationEntry[] entries;
        private ReferenceSearchState(string description,string symbol,string project,NavigationEntry[] entries,ReferenceDisplaySnapshot display,Func<Task<NavigationEntry[]>> refresh)
        {Description=description;Symbol=symbol;Project=project;this.entries=entries;Display=display;Refresh=refresh;}
        internal string Description {get;} internal string Symbol {get;} internal string Project {get;}
        internal ReferenceDisplaySnapshot Display {get;} internal Func<Task<NavigationEntry[]>> Refresh {get;}
        internal int Count=>entries.Length;
        internal NavigationEntry Entry(int index)=>entries[index];
        internal ReferenceSearchState WithResults(NavigationEntry[] rows)=>Capture(Description,Symbol,rows,Refresh,Project);
        internal static ReferenceSearchState Capture(string description,string symbol,NavigationEntry[] rows,Func<Task<NavigationEntry[]>> refresh,string project=null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();var entries=rows.ToArray();
            var dte=Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if(project==null)try{project=dte?.ActiveDocument?.ProjectItem?.ContainingProject?.FullName??dte?.Solution?.FullName??"";}catch(COMException){project="";}
            var display=ReferenceDisplaySnapshot.Create(entries.Select(entry=>new ReferenceLocation(entry.File,entry.Line,entry.Column,entry.Name)),file=>{
                try {var open=dte?.Documents.Cast<EnvDTE.Document>().FirstOrDefault(document=>string.Equals(document.FullName,file,StringComparison.OrdinalIgnoreCase));var text=open?.Object("TextDocument") as EnvDTE.TextDocument;
                    return text==null?File.ReadAllText(file):text.StartPoint.CreateEditPoint().GetText(text.EndPoint);}
                catch(IOException){return "";}catch(UnauthorizedAccessException){return "";}catch(COMException){return "";}
            });
            return new ReferenceSearchState(description,symbol??"",project??"",entries,display,refresh);
        }
    }
}
