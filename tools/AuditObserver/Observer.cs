using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.ValidationObserver
{
    // Passive instrumentation: never executes commands, edits buffers or moves carets.
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("C/C++")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public sealed class Observer : IWpfTextViewCreationListener
    {
        [Import] internal ITextDocumentFactoryService Documents = null;
        private static readonly Dictionary<string,object> States=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
        private static readonly string Folder=@"C:\Projects\CodeAssist\.local\full-runtime";
        public void TextViewCreated(IWpfTextView view)
        {
            ITextDocument document;if(!Documents.TryGetTextDocument(view.TextBuffer,out document))return;
            var file=Path.GetFullPath(document.FilePath);
            if(!file.StartsWith(@"C:\Projects\CodeAssist\tests\",StringComparison.OrdinalIgnoreCase)&&!file.StartsWith(@"C:\Projects\CodeAssist\.local\",StringComparison.OrdinalIgnoreCase))return;
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(150)};
            Action schedule=()=>{if(!view.IsClosed){timer.Stop();timer.Start();}};
            timer.Tick+=(s,e)=>{
                timer.Stop();if(view.IsClosed)return;
                try{
                    var snapshot=view.TextSnapshot;var span=view.Selection.StreamSelectionSpan.SnapshotSpan;if(snapshot.Length>256*1024)return;
                    States[file]=new {file,text=snapshot.GetText(),version=snapshot.Version.VersionNumber,caret=view.Caret.Position.BufferPosition.Position,line=view.Caret.Position.BufferPosition.GetContainingLine().LineNumber+1,selectionStart=span.Start.Position,selectionLength=span.Length,selectedText=span.GetText(),zoom=view.ZoomLevel,observedAt=DateTimeOffset.Now.ToString("o")};
                    var json=new JavaScriptSerializer {MaxJsonLength=8*1024*1024}.Serialize(new {processId=Process.GetCurrentProcess().Id,assembly=typeof(Observer).Assembly.FullName,scope="Passive actual VS snapshots; no commands or input injected.",states=States.Values.ToArray()});
                    Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"observer-"+Process.GetCurrentProcess().Id+".json"),json);
                }catch(Exception ex){try{File.WriteAllText(Path.Combine(Folder,"observer-error-"+Process.GetCurrentProcess().Id+".txt"),ex.ToString());}catch{}}
            };
            EventHandler changed=(s,e)=>schedule();
            view.Caret.PositionChanged+=(s,e)=>schedule();view.Selection.SelectionChanged+=changed;view.TextBuffer.Changed+=(s,e)=>schedule();view.GotAggregateFocus+=changed;view.Closed+=(s,e)=>timer.Stop();schedule();
        }
    }
}
