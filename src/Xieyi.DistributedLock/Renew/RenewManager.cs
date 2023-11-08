namespace Xieyi.DistributedLock.Renew
{
    public sealed class RenewManager
    {
        private readonly RenewEntryPriorityBlockingQueue<RenewEntry> _priorityQueue;
        private RenewThread[] _threads;

        public static RenewManager Instance { get; } = new RenewManager();

        private RenewManager()
        {
            _priorityQueue = new RenewEntryPriorityBlockingQueue<RenewEntry>();

            InitializeRenewThreads();
        }

        private void InitializeRenewThreads()
        {
            _threads = new RenewThread[2];
            for (int i = 0; i < _threads.Length; i++)
            {
                var thread = new RenewThread(_priorityQueue);
                thread.Start();

                _threads[i] = thread;
            }
        }

        internal void AddEntry(RenewEntry renewEntry)
        {
            _priorityQueue.Offer(renewEntry);
        }
    }
}