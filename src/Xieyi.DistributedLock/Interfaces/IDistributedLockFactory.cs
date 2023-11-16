namespace Xieyi.DistributedLock.Interfaces
{
    /// <summary>
    /// OperationLock with Redis Connection
    /// </summary>
    public interface IDistributedLockFactory : IDisposable
    {
        internal long CreateLock(string key, string field, TimeSpan expiryTime);

        internal bool Unlock(string key, string field);

        internal long RenewLock(string key, string field, TimeSpan leaseTime);
    }
}

