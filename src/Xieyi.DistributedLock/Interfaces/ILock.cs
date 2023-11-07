namespace Xieyi.DistributedLock.Interfaces
{
    /// <summary>
    /// Lock Instance For Client
    /// </summary>
    public interface ILock : IDisposable
    {
        void Lock();
        void Lock(TimeSpan leaseTime);

        bool TryLock();
        bool TryLock(TimeSpan leaseTime);
        bool TryLock(TimeSpan waitTime, TimeSpan leaseTime);
        
        void Unlock();
    }
}