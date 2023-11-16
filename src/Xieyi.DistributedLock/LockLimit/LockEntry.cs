namespace Xieyi.DistributedLock.LockLimit
{
    internal class LockEntry : RefCounted
    {
        private readonly object _locker = new object();

        public LockEntry(string name) : base(name)
        {
        }

        protected override void CloseInternal()
        {
            LocalLockManager.Instance.Remove(Name);
        }

        public void Enter()
        {
            Monitor.Enter(_locker);
        }

        public bool TryEnter(int waitTime)
        {
            return Monitor.TryEnter(_locker, waitTime);
        }

        public bool IsEntered()
        {
            return Monitor.IsEntered(_locker);
        }

        public void Exit()
        {
            Monitor.Exit(_locker);
        }
    }
}