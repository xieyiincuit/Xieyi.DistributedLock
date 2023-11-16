using System.Collections.Concurrent;
using Xieyi.DistributedLock.Exceptions;

namespace Xieyi.DistributedLock.LockLimit
{
    internal class LocalLockManager
    {
        private readonly ConcurrentDictionary<string, LockEntry> _lockEntries;

        private LocalLockManager()
        {
            _lockEntries = new ConcurrentDictionary<string, LockEntry>();
        }

        public static LocalLockManager Instance { get; } = new LocalLockManager();

        public void Lock(string name)
        {
            GetLockEntry(name).Enter();
        }

        public bool TryLock(string name, int waitTime)
        {
            if (waitTime <= 0)
            {
                return false;
            }

            var entry = GetLockEntry(name);
            if (!entry.TryEnter(waitTime))
            {
                entry.DecRef();
                return false;
            }

            return true;
        }

        private LockEntry GetLockEntry(string name)
        {
            while (true)
            {
                if (_lockEntries.TryGetValue(name, out var entry))
                {
                    if (entry.TryIncRef())
                    {
                        return entry;
                    }
                }

                var newEntry = new LockEntry(name);
                if (_lockEntries.TryAdd(name, newEntry))
                {
                    return newEntry;
                }
            }
        }

        public void Unlock(string name)
        {
            if (!(_lockEntries.TryGetValue(name, out var entry) && entry.IsEntered()))
            {
                throw new LockInInvalidStateException("The local lock is missing or the local lock is not held by the current thread. " + $"LockName: [{name}]");
            }

            entry.Exit();
            entry.DecRef();
        }

        internal void Remove(string name)
        {
            _lockEntries.TryRemove(name, out _);
        }
    }
}