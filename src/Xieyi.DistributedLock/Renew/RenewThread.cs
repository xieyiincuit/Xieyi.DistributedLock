using Microsoft.Extensions.Logging;

namespace Xieyi.DistributedLock.Renew
{
    internal class RenewThread : ShutdownableThread
    {
        private readonly RenewEntryPriorityBlockingQueue<RenewEntry> _priorityQueue;
        private readonly ILoggerFactory _loggerFactory;

        internal RenewThread(RenewEntryPriorityBlockingQueue<RenewEntry> priorityQueue, ILoggerFactory loggerFactory = null)
        {
            _priorityQueue = priorityQueue;
            _loggerFactory = loggerFactory ?? new LoggerFactory();
        }

        protected override void Run()
        {
            var logger = _loggerFactory.CreateLogger<RenewThread>();
            
            while (!_cancellation.IsCancellationRequested)
            {
                var renewEntry = _priorityQueue.Poll();

                //lock is already exit by other thread
                if (renewEntry.IsUnlocked)
                    continue;

                var pttl = 0L;
                try
                {
                    pttl = TryRenew(renewEntry);
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"Failed to renew the lock, will try it again. {renewEntry}.", ex);

                    UpdateAndEnqueue(renewEntry, pttl);

                    continue;
                }

                //renew success, Enqueue for next renew
                if (pttl > 0)
                {
                    UpdateAndEnqueue(renewEntry, pttl);
                }
                else
                {
                    if (!renewEntry.IsUnlocked)
                    {
                        logger.LogError($"Failed to renew the lock because the lock was not unlocked logically but the lock was lost in fact. {renewEntry}.");

                        renewEntry.NotifyRenewFailed();
                    }
                }
            }
        }
        
        private long TryRenew(RenewEntry renewEntry)
        {
            var pttl = renewEntry.LockFactory.RenewLock(renewEntry.LockName, renewEntry.RenewId, renewEntry.LeaseTime);
            return pttl;
        }

        private void UpdateAndEnqueue(RenewEntry renewEntry, long pttl)
        {
            renewEntry.UpdateRenewTime(pttl);
            _priorityQueue.Offer(renewEntry);
        }
    }
}