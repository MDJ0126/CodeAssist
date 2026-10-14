using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeAssist.Core
{
    /// <summary>Rejects stale results, including results from a previously closed document.</summary>
    public sealed class DocumentRevisionStore<T>
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, long> revisions = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, T> values = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        private long next;
        public long Begin(string path)
        {
            lock (gate) { var revision = ++next; revisions[path] = revision; return revision; }
        }
        public bool Publish(string path, long revision, T value)
        {
            lock (gate)
            {
                if (!revisions.TryGetValue(path, out var current) || current != revision) return false;
                values[path] = value;
                return true;
            }
        }
        public void Remove(string path) { lock (gate) { revisions.Remove(path); values.Remove(path); } }
        public T[] Snapshot() { lock (gate) return values.Values.ToArray(); }
    }
}
