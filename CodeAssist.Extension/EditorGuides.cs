using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using CodeAssist.Core;
namespace CodeAssist.Extension
{
    [Export(typeof(EditorFormatDefinition))][Name("CodeAssist Matched Brace")][UserVisible(true)]
    internal sealed class MatchedBraceFormat:MarkerFormatDefinition {public MatchedBraceFormat(){DisplayName="CodeAssist Matched Brace";BackgroundColor=Color.FromRgb(65,75,100);}}
    [Export(typeof(EditorFormatDefinition))][Name("CodeAssist Mismatched Brace")][UserVisible(true)]
    internal sealed class MismatchedBraceFormat:MarkerFormatDefinition {public MismatchedBraceFormat(){DisplayName="CodeAssist Mismatched Brace";BackgroundColor=Color.FromRgb(145,40,40);}}
    [Export(typeof(IViewTaggerProvider))][ContentType("C/C++")][TagType(typeof(TextMarkerTag))]
    internal sealed class BraceGuideProvider:IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView view,ITextBuffer buffer) where T:ITag=>view.TextBuffer==buffer?view.Properties.GetOrCreateSingletonProperty(()=>new BraceGuide(view)) as ITagger<T>:null;
    }
    internal sealed class BraceGuide:ITagger<TextMarkerTag>
    {
        private readonly ITextView view;private ITextSnapshot parsed;private BraceMap map;
        internal BraceGuide(ITextView view){this.view=view;view.Caret.PositionChanged+=CaretChanged;view.TextBuffer.Changed+=Changed;view.Closed+=Closed;StartupUi.SettingsApplied+=Settings;}
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        private void Settings(){if(!view.IsClosed)TagsChanged?.Invoke(this,new SnapshotSpanEventArgs(new SnapshotSpan(view.TextSnapshot,0,view.TextSnapshot.Length)));}
        private void CaretChanged(object sender,CaretPositionChangedEventArgs e)=>Settings();
        private void Changed(object sender,TextContentChangedEventArgs e){parsed=null;Settings();}
        private void Closed(object sender,EventArgs e){view.Caret.PositionChanged-=CaretChanged;view.TextBuffer.Changed-=Changed;view.Closed-=Closed;StartupUi.SettingsApplied-=Settings;}
        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(spans.Count==0||view.IsClosed||view.TextSnapshot.Length>2*1024*1024)yield break;
            var snapshot=spans[0].Snapshot;if(snapshot!=view.TextSnapshot)yield break;
            if(!ReferenceEquals(parsed,snapshot)){map=new BraceMap(snapshot.GetText());parsed=snapshot;}
            var caret=view.Caret.Position.BufferPosition.Position;var position=caret;int partner;
            if(!map.At(position,out partner)){position=caret-1;if(!map.At(position,out partner))yield break;}
            var p=StartupUi.Preferences;if(partner<0&&!p.HighlightMismatchingBraces||partner>=0&&!p.HighlightMatchingBraces)yield break;
            var tag=new TextMarkerTag(partner<0?"CodeAssist Mismatched Brace":"CodeAssist Matched Brace");
            foreach(var offset in partner<0?new[]{position}:new[]{position,partner}){var span=new SnapshotSpan(snapshot,offset,1);foreach(var requested in spans)if(requested.IntersectsWith(span)){yield return new TagSpan<TextMarkerTag>(span,tag);break;}}
        }
    }
    [Export(typeof(IWpfTextViewCreationListener))][ContentType("C/C++")][TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ColumnGuideProvider:IWpfTextViewCreationListener
    {
        [Export(typeof(AdornmentLayerDefinition))][Name("CodeAssist Column Guide")][Order(After=PredefinedAdornmentLayers.Selection,Before=PredefinedAdornmentLayers.Text)]
        internal AdornmentLayerDefinition Layer=null;
        public void TextViewCreated(IWpfTextView view)=>view.Properties.GetOrCreateSingletonProperty(()=>new ColumnGuide(view));
    }
    internal sealed class ColumnGuide
    {
        private readonly IWpfTextView view;private readonly IAdornmentLayer layer;
        internal ColumnGuide(IWpfTextView view){this.view=view;layer=view.GetAdornmentLayer("CodeAssist Column Guide");view.LayoutChanged+=Layout;view.Closed+=Closed;StartupUi.SettingsApplied+=Update;Update();}
        private void Layout(object sender,TextViewLayoutChangedEventArgs e)=>Update();
        private void Update()
        {
            if(view.IsClosed)return;layer.RemoveAllAdornments();var p=StartupUi.Preferences;
            if(view.Options.GetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId)!=p.HighlightCurrentLine)view.Options.SetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId,p.HighlightCurrentLine);
            if(!p.ColumnIndicatorEnabled||view.ViewportHeight<=0)return;
            var x=view.FormattedLineSource.BaseIndentation+view.FormattedLineSource.ColumnWidth*p.ColumnIndicator-view.ViewportLeft;
            if(x<0||x>view.ViewportWidth)return;
            var line=new Line {X1=x,X2=x,Y1=0,Y2=view.ViewportHeight,Stroke=Brushes.Gray,Opacity=.5,StrokeThickness=1,IsHitTestVisible=false};
            layer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative,null,null,line,null);
        }
        private void Closed(object sender,EventArgs e){view.LayoutChanged-=Layout;view.Closed-=Closed;StartupUi.SettingsApplied-=Update;}
    }
}
