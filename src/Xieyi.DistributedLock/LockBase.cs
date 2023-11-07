using System.Collections.Concurrent;
using Xieyi.DistributedLock.Exceptions;
using Xieyi.DistributedLock.Interfaces;
using Xieyi.DistributedLock.Renew;

namespace Xieyi.DistributedLock
{
    internal abstract class LockBase : ILock
    {
        protected readonly IDistributedLockFactory _lockFactory;
        protected readonly string _lockName;
        protected readonly string _entryName;
        protected readonly string _id;
        protected readonly int _retryIntervalMilliseconds;

        // 理想情况下，有且仅有一个线程会获取分布式锁，只用记录单个线程ID。
        // 但存在以下情况：
        // 线程 A 持有分布式锁 -> Redis 服务端锁失效 -> 线程 B 在线程 A 解锁前持有分布式锁
        // 导致一个分布式锁对象存在被两个线程同时 “持有” 的可能。
        // 因此需要使用集合记录持有分布式锁的线程。
        private readonly ConcurrentDictionary<int, RenewEntry> _threadIds;

        protected LockBase(string lockName, IDistributedLockFactory lockFactory)
        {
            _lockName = lockName;
            _entryName = lockName;
            _id = Guid.NewGuid().ToString();
            _retryIntervalMilliseconds = 200;

            _threadIds = new ConcurrentDictionary<int, RenewEntry>();

            _lockFactory = lockFactory;
        }

        internal string LockName => _lockName;
        internal string EntryName => _entryName;
        internal string Id => _id;
        internal IDistributedLockFactory LockFactory => _lockFactory;

        public abstract void Lock();

        public abstract void Lock(TimeSpan leaseTime);

        public abstract bool TryLock();

        public abstract bool TryLock(TimeSpan leaseTime);

        public abstract bool TryLock(TimeSpan waitTime, TimeSpan leaseTime);

        public abstract void Unlock();

        protected bool TryLockInternal(TimeSpan leaseTime)
        {
            var pttl = _lockFactory.CreateLock(_lockName, GetClientId(), leaseTime);

            //set renew entry in thread processing
            if (pttl > 0)
            {
                if (TryGetRenewEntry(out var oldEntry))
                {
                    oldEntry.IncRef();
                }
                else
                {
                    var renewEntry = new RenewEntry(this, leaseTime, pttl);
                    _threadIds.TryAdd(Thread.CurrentThread.ManagedThreadId, renewEntry);

                    RenewManager.Instance.AddEntry(renewEntry);
                }

                return true;
            }

            return false;
        }

        protected bool TryUnlockInternal()
        {
            if (TryGetRenewEntry(out var renewEntry))
            {
                renewEntry.DecRef();
            }

            var success = _lockFactory.Unlock(_lockName, GetClientId());
            return success;
        }


        public bool RenewFailed()
        {
            if (TryGetRenewEntry(out var renewEntry))
            {
                return renewEntry.IsRenewFailed;
            }

            throw new LockInInvalidStateException("Can only get RenewFailed status while holding distributed lock.");
        }

        public CancellationToken GetRenewFailedToken()
        {
            if (TryGetRenewEntry(out var renewEntry))
            {
                return renewEntry.RenewFailedToken;
            }

            throw new LockInInvalidStateException("Can only get RenewFailedToken while holding distributed lock.");
        }

        private bool TryGetRenewEntry(out RenewEntry renewEntry)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return _threadIds.TryGetValue(threadId, out renewEntry);
        }

        private string GetClientId()
        {
            return $"{_id}:{Thread.CurrentThread.ManagedThreadId}";
        }

        public void RemoveThreadId(int threadId)
        {
            _threadIds.TryRemove(threadId, out _);
        }

        public override string ToString()
        {
            return $"LockName: [{_lockName}] ClientID:[{GetClientId()}]";
        }

        public void Dispose() => Unlock();
    }
}