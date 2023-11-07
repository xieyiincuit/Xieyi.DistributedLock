using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Xieyi.DistributedLock.Extensions;

namespace Xieyi.DistributedLock.Connection
{
    /// <summary>
    /// A connection provider that Create Redis Connection
    /// </summary>
    internal class NativeDistributedLockProvider : DistributedLockProvider
    {
        private readonly ILoggerFactory loggerFactory;

        public DistributedLockEndPoint LockEndPoint { get; init; }

        private DistributedLockConnection connection;

        private const int DefaultConnectionTimeout = 100;
        private const int DefaultSyncTimeout = 1000;
        private const int DefaultConfigCheckSeconds = 10;

        public NativeDistributedLockProvider(ILoggerFactory loggerFactory = null)
        {
            this.loggerFactory = loggerFactory ?? new LoggerFactory();
            this.LockEndPoint = new DistributedLockEndPoint();
        }

        internal override DistributedLockConnection CreateRedisConnection()
        {
            if (this.LockEndPoint == null)
            {
                throw new ArgumentException("No endpoints specified");
            }

            var logger = loggerFactory.CreateLogger<NativeDistributedLockProvider>();
            
            var redisConfig = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                ConnectTimeout = LockEndPoint.ConnectionTimeout ?? DefaultConnectionTimeout,
                SyncTimeout = LockEndPoint.SyncTimeout ?? DefaultSyncTimeout,
                Ssl = LockEndPoint.Ssl,
                SslProtocols = LockEndPoint.SslProtocols,
                Password = LockEndPoint.Password,
                ConfigCheckSeconds = LockEndPoint.ConfigCheckSeconds ?? DefaultConfigCheckSeconds
            };

            redisConfig.EndPoints.Add(LockEndPoint.EndPoint);

            connection = new DistributedLockConnection
            {
                ConnectionMultiplexer = ConnectionMultiplexer.Connect(redisConfig),
                RedisDatabase = LockEndPoint.RedisDatabase ?? DefaultRedisDatabase,
                RedisKeyFormat = string.IsNullOrEmpty(LockEndPoint.RedisKeyFormat) ? DefaultRedisKeyFormat : LockEndPoint.RedisKeyFormat
            };

            connection.ConnectionMultiplexer.ConnectionFailed += (_, args) => { logger.LogWarning($"ConnectionFailed: {args.EndPoint.GetFriendlyName()} ConnectionType: {args.ConnectionType} FailureType: {args.FailureType}"); };

            connection.ConnectionMultiplexer.ConnectionRestored += (_, args) => { logger.LogWarning($"ConnectionRestored: {args.EndPoint.GetFriendlyName()} ConnectionType: {args.ConnectionType} FailureType: {args.FailureType}"); };

            connection.ConnectionMultiplexer.ConfigurationChanged += (_, args) => { logger.LogDebug($"ConfigurationChanged: {args.EndPoint.GetFriendlyName()}"); };

            connection.ConnectionMultiplexer.ConfigurationChangedBroadcast += (_, args) => { logger.LogDebug($"ConfigurationChangedBroadcast: {args.EndPoint.GetFriendlyName()}"); };

            connection.ConnectionMultiplexer.ErrorMessage += (_, args) => { logger.LogWarning($"ErrorMessage: {args.EndPoint.GetFriendlyName()} Message: {args.Message}"); };

            return connection;
        }

        internal override void DisposeConnection()
        {
            connection.ConnectionMultiplexer.Dispose();
        }
    }
}