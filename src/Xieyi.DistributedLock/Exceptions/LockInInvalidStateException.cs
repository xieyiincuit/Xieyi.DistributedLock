namespace Xieyi.DistributedLock.Exceptions
{
    public class LockInInvalidStateException : ApplicationException
    {
        public LockInInvalidStateException(string message) : base(message)
        {
        }
    }
}