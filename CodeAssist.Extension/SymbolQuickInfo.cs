using CodeAssist.Core;
using CodeAssist.Indexing;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace CodeAssist.Extension
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("CodeAssist Symbol Quick Info")]
    [ContentType("C/C++")]
    [Order(After=Priority.Default)]
    internal sealed class SymbolQuickInfoProvider : IAsyncQuickInfoSourceProvider
    {
        [Import] internal ProjectIndexService Indexes=null;
        [Import] internal ITextDocumentFactoryService Documents=null;
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer buffer) { WelcomeDocumentWindow.RecordEvent("QuickInfoSourceCreated contentType="+buffer.ContentType.TypeName); Documents.TryGetTextDocument(buffer,out var document); return new SymbolQuickInfoSource(buffer,Indexes,document?.FilePath); }
    }
    internal sealed class SymbolQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer buffer;
        private readonly ProjectIndexService indexes;
        private readonly string filePath;
        private bool disposed;
        private int traced;
        internal SymbolQuickInfoSource(ITextBuffer buffer,ProjectIndexService indexes,string filePath){this.buffer=buffer;this.indexes=indexes;this.filePath=filePath;}
        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session,CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(disposed||!StartupUi.Preferences.QuickInfoEnabled||indexes==null)return Task.FromResult<QuickInfoItem>(null);
            var snapshot=buffer.CurrentSnapshot;var point=session.GetTriggerPoint(snapshot);
            if(!point.HasValue)return Task.FromResult<QuickInfoItem>(null);
            var line=point.Value.GetContainingLine();var column=point.Value.Position-line.Start.Position;
            var word=IdentifierParser.At(line.GetText(),column);
            if(word.Length==0)return Task.FromResult<QuickInfoItem>(null);
            var start=Math.Min(point.Value.Position,snapshot.Length-1);
            if(start>=0 && !(char.IsLetterOrDigit(snapshot[start])||snapshot[start]=='_'))start--;
            while(start>line.Start && (char.IsLetterOrDigit(snapshot[start-1])||snapshot[start-1]=='_'))start--;
            if(start<0||start+word.Length>snapshot.Length)return Task.FromResult<QuickInfoItem>(null);
            var text=snapshot.GetText();
            var candidates=QuickInfoSymbolResolver.Resolve(
                PersistentSymbolIndex.ReadDocumentSymbols(filePath??"",text).Concat(indexes.FindLiveSymbolDetails(word)),
                word,filePath,line.LineNumber+1,CppExpressionContext.FindEnclosingFunctionOwner(text,start));
            if(traced++<8)WelcomeDocumentWindow.RecordEvent("QuickInfoQuery word="+word+" candidates="+candidates.Length+" coloring="+StartupUi.Preferences.ColorTooltips);
            var extraRows=new List<object>();
            var macro=DisplayQuickInfo.MacroPreview(text,start,word,StartupUi.Preferences);
            foreach(var expansion in macro)extraRows.Add(new ClassifiedTextElement(new ClassifiedTextRun("text",expansion)));
            if(StartupUi.Preferences.QuickInfoComments)foreach(var symbol in candidates.Where(s=>string.Equals(s.FilePath,filePath,StringComparison.OrdinalIgnoreCase))){var comment=DisplayQuickInfo.CommentAbove(text,symbol.Line,StartupUi.Preferences.QuickInfoDoxygen);if(comment.Length>0)extraRows.Add(new ClassifiedTextElement(new ClassifiedTextRun("text",comment)));}
            if(candidates.Length==0&&StartupUi.Preferences.QuickInfoStructural){var structural=DisplayQuickInfo.StructuralDescription(word);if(structural!=null)extraRows.Add(new ClassifiedTextElement(new ClassifiedTextRun("keyword",structural)));}
            if(candidates.Length==0&&extraRows.Count==0)return Task.FromResult<QuickInfoItem>(null);

            TraceNativeContent(session);
            session.TextView.Properties[typeof(QuickInfoFallback)]=session;
            var rows=candidates.Select(symbol=>
            {
                var signature=QuickInfoSymbolResolver.DisplaySignature(symbol);
                var runs=new List<ClassifiedTextRun>();var cursor=0;
                if(StartupUi.Preferences.ColoringEnabled&&StartupUi.Preferences.ColorTooltips){
                    foreach(var token in SymbolDisplayTokens.Parse(signature,symbol.Kind)){
                        if(token.Start>cursor)runs.Add(new ClassifiedTextRun("text",signature.Substring(cursor,token.Start-cursor)));
                        runs.Add(new ClassifiedTextRun(RecommendedColors.DisplayClassification(token.Ink),signature.Substring(token.Start,token.Length)));cursor=token.Start+token.Length;
                    }
                }
                if(cursor<signature.Length)runs.Add(new ClassifiedTextRun("text",signature.Substring(cursor)));
                return (object)new ClassifiedTextElement(runs);
            }).ToArray();
            var content=new ContainerElement(ContainerElementStyle.Stacked,rows.Concat(extraRows).ToArray());
            cancellationToken.ThrowIfCancellationRequested();
            if(disposed||!ReferenceEquals(snapshot,buffer.CurrentSnapshot))return Task.FromResult<QuickInfoItem>(null);
            return Task.FromResult(new QuickInfoItem(snapshot.CreateTrackingSpan(start,word.Length,SpanTrackingMode.EdgeInclusive),new QuickInfoFallback {Session=session,Content=content,Word=word,KeepAlongsideNative=extraRows.Count>0}));
        }
        private void TraceNativeContent(IAsyncQuickInfoSession session)
        {
            var root=Environment.GetEnvironmentVariable("CODEASSIST_PARITY_ROOT");
            if(string.IsNullOrEmpty(root)||string.IsNullOrEmpty(filePath)||!filePath.StartsWith(root,StringComparison.OrdinalIgnoreCase))return;
            EventHandler<QuickInfoSessionStateChangedEventArgs> handler=null;
            handler=(s,e)=>{
                if(e.NewState==QuickInfoSessionState.Visible)
                {
                    WelcomeDocumentWindow.RecordEvent("QuickInfoContentTypes "+string.Join(" | ",session.Content.Select(c=>c?.GetType().FullName??"null")));
                    foreach(var item in session.Content)TraceContent(item,0);
                    session.StateChanged-=handler;
                }
                else if(e.NewState==QuickInfoSessionState.Dismissed)session.StateChanged-=handler;
            };
            session.StateChanged+=handler;
        }
        private static void TraceContent(object item,int depth)
        {
            if(item==null||depth>6)return;
            var detail=item is ClassifiedTextElement text?string.Join("|",text.Runs.Select(r=>r.ClassificationTypeName+":"+r.Text)):item is string literal?literal:"";
            WelcomeDocumentWindow.RecordEvent("QuickInfoNode depth="+depth+" type="+item.GetType().FullName+" text="+detail.Replace("\r"," ").Replace("\n"," ").Substring(0,Math.Min(400,detail.Length)));
            if(item is ContainerElement container)foreach(var child in container.Elements)TraceContent(child,depth+1);
        }
        public void Dispose(){disposed=true;}
    }
}