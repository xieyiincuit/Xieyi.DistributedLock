namespace Xieyi.DistributedLock.Exceptions
{
    internal class LockInInvalidStateException : ApplicationException
    {
        public LockInInvalidStateException(string message) : base(message)
        {
        }
    }
}