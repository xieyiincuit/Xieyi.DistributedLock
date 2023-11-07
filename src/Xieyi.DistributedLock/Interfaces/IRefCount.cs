namespace Xieyi.DistributedLock.Interfaces
{
    /// <summary>
    /// refCount to sure LockEntry is thread-safe
    /// </summary>
    internal interface IRefCount
    {
        void IncRef();

        bool TryIncRef();

        bool DecRef();
    }
}