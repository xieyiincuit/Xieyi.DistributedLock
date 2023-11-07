namespace Xieyi.DistributedLock.Exceptions
{
    public class UnlockFailException : ApplicationException
    {
        public UnlockFailException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
