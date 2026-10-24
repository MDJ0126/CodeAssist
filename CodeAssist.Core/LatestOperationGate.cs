namespace CodeAssist.Core
{
    /// <summary>Owner-thread gate for asynchronous work. Reset supersedes pending work without cancelling its task.</summary>
    public sealed class LatestOperationGate
    {
        public sealed class Ticket { internal Ticket() { } }
        private Ticket active;
        public bool IsRunning => active != null;
        public Ticket TryBegin()
        {
            if (active != null) return null;
            return active = new Ticket();
        }
        public bool IsCurrent(Ticket ticket) => ticket != null && ReferenceEquals(active, ticket);
        public bool TryComplete(Ticket ticket)
        {
            if (!IsCurrent(ticket)) return false;
            active = null;
            return true;
        }
        public void Reset() { active = null; }
    }
}
