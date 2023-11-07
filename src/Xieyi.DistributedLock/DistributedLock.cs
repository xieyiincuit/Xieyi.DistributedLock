using System.Diagnostics;
using Xieyi.DistributedLock.Exceptions;
using Xieyi.DistributedLock.Interfaces;
using Xieyi.DistributedLock.LockLimit;

namespace Xieyi.DistributedLock
{
    internal class DistributedLock : LockBase
    {
        public DistributedLock(string lockName, IDistributedLockFactory lockFactory) : base(lockName, lockFactory)
        {
        }

        public override void Lock()
        {
            Lock(TimeSpan.FromSeconds(30));
        }

        public override void Lock(TimeSpan leaseTime)
        {
            if (leaseTime.TotalMilliseconds < 0)
                throw new ArgumentException("The leaseTime must be a positive number.");
            
            if (TryLockInternal(leaseTime))
            {
                return;
            }

            LocalLockManager.Instance.Lock(_entryName);
            try
            {
                while (true)
                {
                    if (TryLockInternal(leaseTime))
                    {
                        return;
                    }

                    Thread.Sleep(_retryIntervalMilliseconds);
                }
            }
            finally
            {
                LocalLockManager.Instance.Unlock(_entryName);
            }
        }

        public override bool TryLock()
        {
            return TryLock(TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        public override bool TryLock(TimeSpan leaseTime)
        {
            return TryLock(TimeSpan.Zero, leaseTime);
        }

        public override bool TryLock(TimeSpan waitTime, TimeSpan leaseTime)
        {
            var stopwatch = Stopwatch.StartNew();

            if (TryLockInternal(leaseTime))
            {
                return true;
            }

            if (LocalLockManager.Instance.TryLock(_entryName, (int)(waitTime.TotalMilliseconds - stopwatch.ElapsedMilliseconds)))
            {
                try
                {
                    do
                    {
                        if (TryLockInternal(leaseTime))
                        {
                            return true;
                        }

                        Thread.Sleep(_retryIntervalMilliseconds);
                    }
                    while (stopwatch.ElapsedMilliseconds < waitTime.TotalMilliseconds);
                }
                finally
                {
                    LocalLockManager.Instance.Unlock(_entryName);
                }
            }

            return false;
        }

        public override void Unlock()
        {
            try
            {
                TryUnlockInternal();
            }
            catch (Exception ex)
            {
                throw new UnlockFailException($"An exception occurred while unlocking. {this}.", ex);
            }
        }
    }
}