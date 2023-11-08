namespace Xieyi.DistributedLock.Interfaces
{
    /// <summary>
    /// OperationLock with Redis Connection
    /// </summary>
    public interface IDistributedLockFactory : IDisposable
    {
        long CreateLock(string key, string field, TimeSpan expiryTime);

        bool Unlock(string key, string field);

        long RenewLock(string key, string field, TimeSpan leaseTime);
    }
}

