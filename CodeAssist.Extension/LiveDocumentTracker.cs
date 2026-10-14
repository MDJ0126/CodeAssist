using Microsoft.VisualStudio.Text;
using System;
using System.Threading;
using Microsoft.VisualStudio.Text.Editor;
using CodeAssist.Core;

namespace CodeAssist.Extension
{
    internal sealed class LiveDocumentTracker : IDisposable
    {
        private readonly ITextBuffer buffer;
        private readonly string filePath;
        private readonly ProjectIndexService indexService;
        private readonly Timer updateTimer;
        private ITextSnapshot pendingSnapshot;
        private readonly object gate = new object();
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private long revision;
        private int views;
        private bool disposed;

        public LiveDocumentTracker(ITextBuffer buffer, string filePath, ProjectIndexService indexService)
        {
            this.buffer = buffer;
            this.filePath = filePath;
            this.indexService = indexService;
            updateTimer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
            buffer.Changed += OnChanged;
            Schedule(buffer.CurrentSnapshot, 0);
        }

        public void Attach(ITextView view)
        {
            lock (gate) views++;
            EventHandler closed = null;
            closed = (sender, args) =>
            {
                view.Closed -= closed;
                lock (gate) { if (--views == 0) Dispose(); }
            };
            view.Closed += closed;
        }

        private void OnChanged(object sender, TextContentChangedEventArgs args)
        {
            Schedule(args.After, 150);
        }

        public void Reparse()
        {
            Schedule(buffer.CurrentSnapshot, 0);
        }

        private void Schedule(ITextSnapshot snapshot, int delay)
        {
            lock (gate)
            {
                if (disposed) return;
                cancellation.Cancel(); cancellation.Dispose();
                cancellation = new CancellationTokenSource();
                revision = indexService.BeginLiveDocument(filePath);
                pendingSnapshot = snapshot;
                updateTimer.Change(delay, Timeout.Infinite);
            }
        }

        private void OnTimer(object state)
        {
            ITextSnapshot snapshot; long currentRevision; CancellationToken token;
            lock (gate)
            {
                if (disposed) return;
                snapshot = pendingSnapshot; currentRevision = revision; token = cancellation.Token;
                pendingSnapshot = null;
            }
            if (snapshot != null) indexService.UpdateLiveDocument(filePath, snapshot.GetText(), currentRevision, token);
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                buffer.Changed -= OnChanged;
                updateTimer.Dispose(); cancellation.Cancel(); cancellation.Dispose();
                pendingSnapshot = null;
                indexService.RemoveLiveDocument(filePath);
                buffer.Properties.RemoveProperty(typeof(LiveDocumentTracker));
            }
        }
    }
}
