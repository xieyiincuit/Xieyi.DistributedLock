namespace Xieyi.DistributedLock.Exceptions
{
    internal class UnlockFailException : ApplicationException
    {
        public UnlockFailException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
