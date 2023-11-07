using Xieyi.DistributedLock.Interfaces;

namespace Xieyi.DistributedLock
{
    public class DistributedLockProvider
    {
        public static ILock Lock(IDistributedLockFactory lockFactory, string lockName)
        {
            var distributedLock = new DistributedLock(lockName, lockFactory);
            distributedLock.Lock();
            return distributedLock;
        }

        public static bool TryLock(IDistributedLockFactory lockFactory, string lockName, out ILock distributedLock)
        {
            return (distributedLock = new DistributedLock(lockName, lockFactory)).TryLock();
        }
    }
}