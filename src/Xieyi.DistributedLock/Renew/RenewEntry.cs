using Xieyi.DistributedLock.Interfaces;
using Xieyi.DistributedLock.LockLimit;

namespace Xieyi.DistributedLock.Renew
{
    internal class RenewEntry : RefCounted, IComparable
    {
        private readonly LockBase _lockBase;
        private readonly int _threadId;
        private readonly TimeSpan _leaseTime;
        private DateTime _nextRenewTime;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public RenewEntry(LockBase lockBase, TimeSpan leaseTime, long pttl) : base(lockBase.EntryName)
        {
            _lockBase = lockBase;

            _leaseTime = leaseTime;
            UpdateRenewTime(pttl);

            _threadId = Thread.CurrentThread.ManagedThreadId;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        internal IDistributedLockFactory LockFactory => _lockBase.LockFactory;
        
        internal string LockName => _lockBase.LockName;

        internal string RenewId => $"{_lockBase.Id}:{_threadId}";

        internal TimeSpan LeaseTime => _leaseTime;

        internal bool IsUnlocked => RefCount <= 0;

        internal CancellationToken RenewFailedToken => _cancellationTokenSource.Token;

        internal bool IsRenewFailed => _cancellationTokenSource.IsCancellationRequested;

        public int TimeToRenew()
        {
            return (int)(_nextRenewTime - DateTime.UtcNow).TotalMilliseconds;
        }

        public void UpdateRenewTime(long pttl)
        {
            _nextRenewTime = pttl > 0
                ? DateTime.UtcNow.AddMilliseconds(pttl / 3)
                : DateTime.UtcNow.AddMilliseconds(_leaseTime.TotalMilliseconds / 3);
        }

        public void NotifyRenewFailed()
        {
            _cancellationTokenSource.Cancel();
        }

        protected override void CloseInternal()
        {
            _lockBase.RemoveThreadId(_threadId);
            _cancellationTokenSource.Dispose();
        }

        public int CompareTo(object obj)
        {
            var other = obj as RenewEntry;
            if (other != null) return _nextRenewTime.CompareTo(other._nextRenewTime);
            return -1;
        }

        public override string ToString()
        {
            return _lockBase.ToString();
        }
    }
}