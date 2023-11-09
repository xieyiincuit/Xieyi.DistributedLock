using Microsoft.Extensions.Logging;

namespace Xieyi.DistributedLock.Connection
{
    public class DistributedLockConfiguration
    {
        public DistributedLockConnectionProvider ConnectionProvider { get; set; }
        public ILoggerFactory LoggerFactory { get; }
        public DistributedLockRetryConfiguration RetryConfiguration { get; init; }

        public DistributedLockConfiguration(DistributedLockEndPoint lockEndPoint, ILoggerFactory loggerFactory = null)
        {
            this.ConnectionProvider = new NativeDistributedLockConnectionProvider(loggerFactory) { LockEndPoint = lockEndPoint };
            this.LoggerFactory = loggerFactory;
        }

        public DistributedLockConfiguration(DistributedLockConnectionProvider connectionConnectionProvider, ILoggerFactory loggerFactory = null)
        {
            this.ConnectionProvider = connectionConnectionProvider ?? throw new ArgumentNullException(nameof(connectionConnectionProvider), "Connection provider must not be null");
            this.LoggerFactory = loggerFactory;
        }
    }
}