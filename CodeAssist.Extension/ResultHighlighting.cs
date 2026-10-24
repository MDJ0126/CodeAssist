using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Windows.Media;
using CodeAssist.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.Extension
{
    internal static class ResultStyles
    {
        [Export(typeof(ClassificationTypeDefinition))][Name("CodeAssist Output File")]internal static ClassificationTypeDefinition File=null;
        [Export(typeof(ClassificationTypeDefinition))][Name("CodeAssist Find Match")]internal static ClassificationTypeDefinition Match=null;
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames="CodeAssist Output File")][Name("CodeAssist Output File")][UserVisible(true)]
        internal sealed class FileFormat:ClassificationFormatDefinition {public FileFormat(){DisplayName="CodeAssist Output File";ForegroundColor=Color.FromRgb(80,170,255);}}
        [Export(typeof(EditorFormatDefinition))][ClassificationType(ClassificationTypeNames="CodeAssist Find Match")][Name("CodeAssist Find Match")][UserVisible(true)]
        internal sealed class MatchFormat:ClassificationFormatDefinition {public MatchFormat(){DisplayName="CodeAssist Find Match";BackgroundColor=Color.FromRgb(105,85,30);}}
    }
    [Export(typeof(IClassifierProvider))][ContentType("text")]
    internal sealed class ResultHighlightProvider:IClassifierProvider
    {
        [Import]internal IClassificationTypeRegistryService Registry=null;
        public IClassifier GetClassifier(ITextBuffer buffer)
        {var type=buffer.ContentType.TypeName;if(type.IndexOf("output",StringComparison.OrdinalIgnoreCase)<0&&type.IndexOf("find",StringComparison.OrdinalIgnoreCase)<0)return null;return buffer.Properties.GetOrCreateSingletonProperty(()=>new ResultHighlighting(buffer,Registry));}
    }
    internal sealed class ResultHighlighting:IClassifier
    {
        private readonly ITextBuffer buffer;private readonly IClassificationTypeRegistryService registry;
        private static readonly Regex FilePath=new Regex(@"(?:[A-Za-z]:[\\/]|(?:\.\.?[\\/])?)[^\r\n:<>""|]*?\.(?:cpp|cxx|cc|c|hpp|hxx|hh|h|inl)(?=[:(\s]|$)",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(30));
        internal ResultHighlighting(ITextBuffer buffer,IClassificationTypeRegistryService registry){this.buffer=buffer;this.registry=registry;buffer.Changed+=(s,e)=>ClassificationChanged?.Invoke(this,new ClassificationChangedEventArgs(new SnapshotSpan(e.After,0,e.After.Length)));}
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
        public System.Collections.Generic.IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result=new List<ClassificationSpan>();var text=span.GetText();if(text.Length>128*1024)return result;
            if(StartupUi.Preferences.OutputFileHighlighting)try{foreach(Match match in FilePath.Matches(text))result.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot,span.Start+match.Index,match.Length),registry.GetClassificationType("CodeAssist Output File")));}catch(RegexMatchTimeoutException){}
            if(StartupUi.Preferences.FindResultHighlighting&&buffer.ContentType.TypeName.IndexOf("find",StringComparison.OrdinalIgnoreCase)>=0&&ThreadHelper.CheckAccess())
            {
                ThreadHelper.ThrowIfNotOnUIThread();var dte=Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;var query=dte?.Find?.FindWhat;if(!string.IsNullOrEmpty(query)&&query.Length<=1024)for(var start=0;start<text.Length;){var at=text.IndexOf(query,start,StringComparison.OrdinalIgnoreCase);if(at<0)break;result.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot,span.Start+at,query.Length),registry.GetClassificationType("CodeAssist Find Match")));start=at+query.Length;}
            }
            return result;
        }
    }
}
