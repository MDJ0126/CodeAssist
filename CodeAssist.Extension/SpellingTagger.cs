using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Markup;
using CodeAssist.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
namespace CodeAssist.Extension
{
    [Export(typeof(ITaggerProvider))][ContentType("C/C++")][TagType(typeof(IErrorTag))]
    internal sealed class SpellingProvider:ITaggerProvider
    {public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T:ITag=>buffer.Properties.GetOrCreateSingletonProperty(()=>new SpellingTagger(buffer)) as ITagger<T>;}
    internal sealed class SpellingTagger:ITagger<IErrorTag>
    {
        private readonly ITextBuffer buffer;private ITextSnapshot parsed;private string mask;private TextBox checker;private string language;
        private readonly Dictionary<string,string> cache=new Dictionary<string,string>(StringComparer.Ordinal);
        internal SpellingTagger(ITextBuffer buffer){this.buffer=buffer;buffer.Changed+=(s,e)=>{parsed=null;TagsChanged?.Invoke(this,new SnapshotSpanEventArgs(new SnapshotSpan(e.After,0,e.After.Length)));};}
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(!StartupUi.Preferences.SpellUnderliningEnabled||spans.Count==0||!ThreadHelper.CheckAccess()||spans[0].Snapshot.Length>256*1024)yield break;
            if(checker==null) {checker=new TextBox();SpellCheck.SetIsEnabled(checker,true);}
            var wanted=StartupUi.Preferences.SpellingLanguage??"en-US";if(language!=wanted){try{checker.Language=XmlLanguage.GetLanguage(wanted);}catch(ArgumentException){yield break;}language=wanted;cache.Clear();}
            var snapshot=spans[0].Snapshot;if(parsed!=snapshot){mask=CppCodeMask.Create(snapshot.GetText());parsed=snapshot;}
            var checkedWords=0;
            foreach(var requested in spans)
            foreach(Match match in Regex.Matches(requested.GetText(),@"\b[a-z]{3,30}\b",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20)))
            {
                if(++checkedWords>150)yield break;var position=requested.Start.Position+match.Index;if(mask.Substring(position,match.Length).Any(c=>!char.IsWhiteSpace(c)))continue;
                if(!cache.TryGetValue(match.Value,out var message)){checker.Text=match.Value;var error=checker.GetSpellingError(0);message=error==null?"":"Possible spelling: "+string.Join(", ",error.Suggestions.Take(3));if(cache.Count>2000)cache.Clear();cache[match.Value]=message;}
                if(message.Length>0)yield return new TagSpan<IErrorTag>(new SnapshotSpan(snapshot,position,match.Length),new ErrorTag(PredefinedErrorTypeNames.Warning,message));
            }
        }
    }
}
