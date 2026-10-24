using System;
using Microsoft.VisualStudio.Shell;
namespace CodeAssist.Extension
{
    // Observe the existing global commands; retain user key bindings unchanged.
    internal static class ReferenceResultKeys
    {
        private static EnvDTE80.DTE2 dte;
        private static EnvDTE.CommandEvents next,previous;
        internal static void Attach(EnvDTE80.DTE2 environment)
        {
            ThreadHelper.ThrowIfNotOnUIThread();Detach();dte=environment;if(dte==null)return;
            next=dte.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}",279];
            previous=dte.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}",280];
            next.BeforeExecute+=BeforeExecute;previous.BeforeExecute+=BeforeExecute;
        }
        internal static void Detach()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(next!=null)next.BeforeExecute-=BeforeExecute;
            if(previous!=null)previous.BeforeExecute-=BeforeExecute;
            next=null;previous=null;dte=null;
        }
        private static void BeforeExecute(string guid,int id,object input,object output,ref bool cancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(cancelDefault||!ReferenceResults.CanMove)return;
            var document=dte?.ActiveDocument;var selection=document?.Selection as EnvDTE.TextSelection;
            if(selection==null)return;
            cancelDefault=ReferenceResults.TryMoveAt(document.FullName,selection.ActivePoint.Line,selection.ActivePoint.LineCharOffset,id==280?-1:1);
        }
    }
}
