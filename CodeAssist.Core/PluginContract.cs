using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeAssist.Core
{
    public sealed class PluginDocument
    {
        public PluginDocument(string path,string text,int caret,int selectionStart,int selectionLength){Path=path;Text=text;Caret=caret;SelectionStart=selectionStart;SelectionLength=selectionLength;}
        public string Path{get;}public string Text{get;}public int Caret{get;}public int SelectionStart{get;}public int SelectionLength{get;}
    }
    public sealed class PluginAction
    {
        public PluginAction(string title,Func<IReadOnlyList<EditorTextChange>> edits){Title=title;Edits=edits??throw new ArgumentNullException(nameof(edits));}
        public string Title{get;}public Func<IReadOnlyList<EditorTextChange>> Edits{get;}
        public override string ToString()=>Title;
    }
    // Export this interface from an installed VSIX MEF component. Workspace DLLs are never loaded.
    public interface ICodeAssistPlugin
    {
        string Name{get;}
        IEnumerable<PluginAction> GetActions(PluginDocument document);
    }
    public static class PluginEdits
    {
        public static string Apply(string original,IEnumerable<EditorTextChange> changes)
        {
            if(original==null)throw new ArgumentNullException(nameof(original));
            var sorted=(changes??throw new ArgumentNullException(nameof(changes))).OrderBy(c=>c.Start).ToArray();
            if(sorted.Length>10000)throw new ArgumentException("Too many plugin edits");
            for(var i=0;i<sorted.Length;i++){var c=sorted[i];if(c.Start<0||c.Length<0||c.Start>original.Length-c.Length||c.Text==null)throw new ArgumentException("Invalid plugin edit");if(i>0&&(c.Start<sorted[i-1].Start+sorted[i-1].Length||c.Start==sorted[i-1].Start))throw new ArgumentException("Overlapping plugin edits");}
            foreach(var c in sorted.Reverse())original=original.Remove(c.Start,c.Length).Insert(c.Start,c.Text);return original;
        }
    }
}
