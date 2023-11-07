namespace Xieyi.DistributedLock.Exceptions
{
    internal class LockHasBeenClosedException : ApplicationException
    {
        public LockHasBeenClosedException(string message) : base(message)
        {
        }
    }
}
