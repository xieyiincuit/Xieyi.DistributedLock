using Xieyi.DistributedLock.Exceptions;
using Xieyi.DistributedLock.Interfaces;

namespace Xieyi.DistributedLock.LockLimit
{
    public abstract class RefCounted : IRefCount
    {
        private long _refCount = 1;
        public long RefCount => Interlocked.Read(ref _refCount);
        public string Name { get; }

        internal RefCounted(string name)
        {
            Name = name;
        }

        public void IncRef()
        {
            if (!TryIncRef())
            {
                throw new LockHasBeenClosedException($"[{Name}] is already closed, can't increment refCount, current count [{RefCount}].");
            }
        }

        public bool TryIncRef()
        {
            //thread-safe refCount
            do
            {
                long i = Interlocked.Read(ref _refCount);
                if (i > 0)
                {
                    if (Interlocked.CompareExchange(ref _refCount, i + 1, i) == i)
                        return true;
                }
                else
                {
                    return false;
                }
            } while (true);
        }

        public bool DecRef()
        {
            long i = Interlocked.Decrement(ref _refCount);
            
            if (i == 0)
            {
                CloseInternal();
                return true;
            }

            return false;
        }

        protected abstract void CloseInternal();
    }
}