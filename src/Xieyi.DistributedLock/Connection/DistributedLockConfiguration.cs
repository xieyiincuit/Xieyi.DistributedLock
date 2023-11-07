using Microsoft.Extensions.Logging;

namespace Xieyi.DistributedLock.Connection
{
    public class DistributedLockConfiguration
    {
        public DistributedLockProvider Provider { get; set; }
        public ILoggerFactory LoggerFactory { get; }
        public DistributedLockRetryConfiguration RetryConfiguration { get; init; }

        public DistributedLockConfiguration(DistributedLockEndPoint lockEndPoint, ILoggerFactory loggerFactory = null)
        {
            this.Provider = new NativeDistributedLockProvider(loggerFactory) { LockEndPoint = lockEndPoint };
            this.LoggerFactory = loggerFactory;
        }

        public DistributedLockConfiguration(DistributedLockProvider connectionProvider, ILoggerFactory loggerFactory = null)
        {
            this.Provider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider), "Connection provider must not be null");
            this.LoggerFactory = loggerFactory;
        }
    }
}