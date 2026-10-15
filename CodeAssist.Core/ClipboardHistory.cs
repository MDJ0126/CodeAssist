using System;
using System.Collections.Generic;

namespace CodeAssist.Core
{
    public sealed class ClipboardHistory
    {
        private readonly object gate = new object();
        private readonly List<string> values = new List<string>();
        private int capacity;
        public int Capacity { get { lock (gate) return capacity; } set { if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); lock (gate) { capacity = value; if (values.Count > capacity) values.RemoveRange(capacity, values.Count - capacity); } } }
        public ClipboardHistory(int capacity = 10) { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); this.capacity = capacity; }
        public void Record(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 1024 * 1024) return;
            lock (gate)
            {
                values.RemoveAll(item => string.Equals(item, value, StringComparison.Ordinal));
                values.Insert(0, value);
                if (values.Count > capacity) values.RemoveRange(capacity, values.Count - capacity);
            }
        }
        public string[] Items() { lock (gate) return values.ToArray(); }
    }
}
