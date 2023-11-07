namespace Xieyi.DistributedLock.Connection
{
    public class DistributedLockRetryConfiguration
    {
        public DistributedLockRetryConfiguration(int? retryCount = null, TimeSpan? retryDelay = null)
        {
            if (retryCount.HasValue && retryCount < 1)
            {
                throw new ArgumentException("Retry count must be at least 1", nameof(retryCount));
            }

            if (retryDelay.HasValue && retryDelay < TimeSpan.FromMilliseconds(10))
            {
                throw new ArgumentException("Retry delay must be at least 10 ms", nameof(retryDelay));
            }

            RetryCount = retryCount;
            RetryDelay = retryDelay;
        }

        public int? RetryCount { get; }
        public TimeSpan? RetryDelay { get; }
    }
}