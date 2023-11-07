namespace Xieyi.DistributedLock.Exceptions
{
    public class LockHasBeenClosedException : ApplicationException
    {
        public LockHasBeenClosedException(string message) : base(message)
        {
        }
    }
}
