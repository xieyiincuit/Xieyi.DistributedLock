namespace Xieyi.DistributedLock.Connection
{
    public abstract class DistributedLockProvider
    {
        internal abstract DistributedLockConnection CreateRedisConnection();
        internal abstract void DisposeConnection();

        protected const int DefaultRedisDatabase = -1;
        protected const string DefaultRedisKeyFormat = "distributedLock:{0}";
    }
}