using System;
using System.Collections.Generic;
namespace CodeAssist.Core
{
    // Original values survive repeated application; only values still owned by us are restored.
    public sealed class ReversibleOverrides<T, TValue>
    {
        private sealed class State { public TValue Original; public TValue Applied; }
        private readonly Dictionary<T,State> states=new Dictionary<T,State>();
        public bool Active => states.Count>0;
        public void Apply(IEnumerable<T> items,Func<T,TValue> read,Action<T,TValue> write,Func<TValue,TValue> transform)
        {
            try {
                foreach(var item in items)
                {
                    if(states.ContainsKey(item))continue;
                    var original=read(item);var value=transform(original);
                    // Keep recovery data even if the setter modifies state before throwing.
                    states[item]=new State { Original=original,Applied=value };write(item,value);
                }
            }
            catch { Restore(read,write);throw; }
        }
        public void Restore(Func<T,TValue> read,Action<T,TValue> write)
        {
            var failures=new List<Exception>();
            foreach(var pair in new List<KeyValuePair<T,State>>(states))
            {
                try {
                    if(EqualityComparer<TValue>.Default.Equals(read(pair.Key),pair.Value.Applied))write(pair.Key,pair.Value.Original);
                    states.Remove(pair.Key);
                }
                catch(Exception error){failures.Add(error);}
            }
            if(failures.Count>0)throw new AggregateException("Some overrides could not be restored.",failures);
        }
    }
    public static class ContextReferenceNavigation
    {
        public static SelectionRange Move(string text,int caret,bool forward)
        {
            if(text==null||caret<0||caret>text.Length)return null;
            var word=IdentifierParser.At(text,caret);if(word.Length==0)return null;
            var start=caret;
            if(start==text.Length || !(char.IsLetterOrDigit(text[start])||text[start]=='_'))start--;
            while(start>0&&(char.IsLetterOrDigit(text[start-1])||text[start-1]=='_'))start--;
            var context=new CppReferenceContext(text);
            if(start<0||start>=context.Code.Length||context.Code.Substring(start,word.Length)!=word)return null;
            var matches=context.References(word,context.DeclarationAt(word,start));
            if(matches.Count==0)return null;
            if(forward){foreach(var offset in matches)if(offset>start)return new SelectionRange(offset,word.Length);return new SelectionRange(matches[0],word.Length);}
            for(var i=matches.Count-1;i>=0;i--)if(matches[i]<start)return new SelectionRange(matches[i],word.Length);
            return new SelectionRange(matches[matches.Count-1],word.Length);
        }
    }
}