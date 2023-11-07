namespace Xieyi.DistributedLock.LockLimit
{
    internal class LockEntry : RefCounted
    {
        private readonly object _locker = new object();

        internal LockEntry(string name) : base(name)
        {
        }

        protected override void CloseInternal()
        {
            LocalLockManager.Instance.Remove(Name);
        }

        internal void Enter()
        {
            Monitor.Enter(_locker);
        }

        internal bool TryEnter(int waitTime)
        {
            return Monitor.TryEnter(_locker, waitTime);
        }

        internal bool IsEntered()
        {
            return Monitor.IsEntered(_locker);
        }

        internal void Exit()
        {
            Monitor.Exit(_locker);
        }
    }
}