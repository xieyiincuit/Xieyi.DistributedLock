using Microsoft.Extensions.Logging;

namespace Xieyi.DistributedLock.Connection
{
    public class DistributedLockConfiguration
    {
        public AbstractDistributedLockConnectionProvider ConnectionProvider { get; set; }
        public ILoggerFactory LoggerFactory { get; }
        public DistributedLockRetryConfiguration RetryConfiguration { get; set; }

        public DistributedLockConfiguration(DistributedLockEndPoint lockEndPoint, ILoggerFactory loggerFactory = null)
        {
            this.ConnectionProvider = new NativeAbstractDistributedLockConnectionProvider(loggerFactory) { LockEndPoint = lockEndPoint };
            this.LoggerFactory = loggerFactory;
        }

        public DistributedLockConfiguration(AbstractDistributedLockConnectionProvider connectionConnectionProvider, ILoggerFactory loggerFactory = null)
        {
            this.ConnectionProvider = connectionConnectionProvider ?? throw new ArgumentNullException(nameof(connectionConnectionProvider), "Connection provider must not be null");
            this.LoggerFactory = loggerFactory;
        }
    }
}