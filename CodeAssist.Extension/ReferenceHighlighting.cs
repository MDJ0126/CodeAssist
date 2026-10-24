using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CodeAssist.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.Extension
{
    [Export(typeof(IViewTaggerProvider))][ContentType("C/C++")][TagType(typeof(TextMarkerTag))]
    internal sealed class ReferenceHighlightProvider:IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView view,ITextBuffer buffer) where T:ITag=>view.TextBuffer==buffer?view.Properties.GetOrCreateSingletonProperty(()=>new ReferenceHighlighting(view)) as ITagger<T>:null;
    }
    internal sealed class ReferenceHighlighting:ITagger<TextMarkerTag>
    {
        private readonly ITextView view;private readonly DispatcherTimer timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(220)};
        private ITextSnapshot parsed,published;private CppReferenceContext context;private int[] offsets=Array.Empty<int>();private int length;private bool closed,running;
        internal ReferenceHighlighting(ITextView view){this.view=view;view.Caret.PositionChanged+=Caret;view.TextBuffer.Changed+=Changed;view.Closed+=Closed;timer.Tick+=Tick;StartupUi.SettingsApplied+=Schedule;if(view is IWpfTextView wpf)wpf.VisualElement.PreviewMouseLeftButtonUp+=Mouse;Schedule();}
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        private void Caret(object sender,CaretPositionChangedEventArgs e)=>Schedule();private void Changed(object sender,TextContentChangedEventArgs e)=>Schedule();
        private void Schedule(){timer.Stop();if(!closed)timer.Start();}
        private void Tick(object sender,EventArgs e){timer.Stop();if(closed)return;if(running){Schedule();return;}ExtensionPackage.Current?.JoinableTaskFactory.RunAsync(UpdateAsync).FileAndForget("CodeAssist/ReferenceHighlighting");}
        private async System.Threading.Tasks.Task UpdateAsync()
        {
            if(closed||view.IsClosed)return;var snapshot=view.TextSnapshot;var position=view.Caret.Position.BufferPosition;var line=position.GetContainingLine();var word=IdentifierParser.At(line.GetText(),position.Position-line.Start.Position);
            if(!StartupUi.Preferences.AutomaticReferenceHighlights||word.Length==0||snapshot.Length>512*1024){offsets=Array.Empty<int>();published=snapshot;Raise();return;}
            running=true;
            try
            {
                var text=ReferenceEquals(parsed,snapshot)?null:snapshot.GetText();var old=context;var found=await System.Threading.Tasks.Task.Run(()=>{var current=text==null?old:new CppReferenceContext(text);var declaration=current.DeclarationAt(word,position);return Tuple.Create(current,current.References(word,declaration).Where(offset=>declaration.HasValue||current.DeclarationAt(word,offset)==null).ToArray());});
                if(closed||view.IsClosed||!ReferenceEquals(snapshot,view.TextSnapshot)||view.Caret.Position.BufferPosition!=position)return;
                context=found.Item1;parsed=snapshot;published=snapshot;offsets=found.Item2;length=word.Length;Raise();
            }
            finally{running=false;}
        }
        private void Mouse(object sender,MouseButtonEventArgs e){ThreadHelper.ThrowIfNotOnUIThread();if(!closed&&StartupUi.Preferences.AltClickGoToDefinition&&Keyboard.Modifiers==ModifierKeys.Alt){e.Handled=true;StartupUi.Execute(0x100);}}
        private void Raise(){if(!closed)TagsChanged?.Invoke(this,new SnapshotSpanEventArgs(new SnapshotSpan(view.TextSnapshot,0,view.TextSnapshot.Length)));}
        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {if(closed||spans.Count==0||spans[0].Snapshot!=published)yield break;foreach(var offset in offsets){var span=new SnapshotSpan(published,offset,length);if(spans.Any(requested=>requested.IntersectsWith(span)))yield return new TagSpan<TextMarkerTag>(span,new TextMarkerTag(WordHighlights.Marker));}}
        private void Closed(object sender,EventArgs e){closed=true;timer.Stop();timer.Tick-=Tick;view.Caret.PositionChanged-=Caret;view.TextBuffer.Changed-=Changed;view.Closed-=Closed;StartupUi.SettingsApplied-=Schedule;if(view is IWpfTextView wpf)wpf.VisualElement.PreviewMouseLeftButtonUp-=Mouse;context=null;offsets=Array.Empty<int>();}
    }
}
