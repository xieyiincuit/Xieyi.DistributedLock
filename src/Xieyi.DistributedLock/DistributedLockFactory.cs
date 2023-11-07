using System.Text;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Xieyi.DistributedLock.Connection;
using Xieyi.DistributedLock.Extensions;
using Xieyi.DistributedLock.Helper;
using Xieyi.DistributedLock.Interfaces;

namespace Xieyi.DistributedLock
{
    public class DistributedLockFactory : IDistributedLockFactory, IDisposable
    {
        private readonly DistributedLockConfiguration configuration;
        private readonly ILogger<DistributedLockFactory> logger;
        private readonly DistributedLockConnection redis;

        private int? retryCount => configuration.RetryConfiguration?.RetryCount ?? 3;
        private TimeSpan? retryDelay => configuration.RetryConfiguration?.RetryDelay ?? TimeSpan.FromMilliseconds(50);

        private static readonly string TryLockScript = EmbeddedResourceLoader.GetEmbeddedResource("Xieyi.DistributedLock.Lua.tryLock.lua");
        private static readonly string UnlockScript = EmbeddedResourceLoader.GetEmbeddedResource("Xieyi.DistributedLock.Lua.unLock.lua");
        private static readonly string RenewLockScript = EmbeddedResourceLoader.GetEmbeddedResource("Xieyi.DistributedLock.Lua.renewLock.lua");

        public static DistributedLockFactory Create(DistributedLockEndPoint endPoint, ILoggerFactory loggerFactory = null)
        {
            return Create(endPoint, null, loggerFactory);
        }

        public static DistributedLockFactory Create(DistributedLockEndPoint endPoint, DistributedLockRetryConfiguration retryConfiguration, ILoggerFactory loggerFactory = null)
        {
            var configuration = new DistributedLockConfiguration(endPoint, loggerFactory)
            {
                RetryConfiguration = retryConfiguration
            };
            return new DistributedLockFactory(configuration);
        }

        public static DistributedLockFactory Create(DistributedLockMultiplexer existingMultiplexer, ILoggerFactory loggerFactory = null)
        {
            return Create(existingMultiplexer, null, loggerFactory);
        }

        public static DistributedLockFactory Create(DistributedLockMultiplexer existingMultiplexer, DistributedLockRetryConfiguration retryConfiguration, ILoggerFactory loggerFactory = null)
        {
            var configuration = new DistributedLockConfiguration(
                new ExistDistributedLockProvider(existingMultiplexer),
                loggerFactory)
            {
                RetryConfiguration = retryConfiguration
            };

            return new DistributedLockFactory(configuration);
        }

        public DistributedLockFactory(DistributedLockConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration), "Configuration must not be null");
            this.redis = configuration.Provider.CreateRedisConnection();
            
            var loggerFactory = configuration.LoggerFactory ?? new LoggerFactory();
            this.logger = loggerFactory.CreateLogger<DistributedLockFactory>();
        }

        public long CreateLock(string key, string field, TimeSpan expiryTime)
        {
            return RetryHelper.Call(logger, () => LockInstance(redis, key, field, expiryTime), retryDelay.Value, retryCount.Value);
        }

        public bool Unlock(string key, string field)
        {
            return RetryHelper.Call(logger, () => UnLockInstance(redis, key, field), retryDelay.Value, retryCount.Value);
        }

        public long RenewLock(string key, string field, TimeSpan leaseTime)
        {
            return RetryHelper.Call(logger, () => RenewLockInstance(redis, key, field, leaseTime), retryDelay.Value, retryCount.Value);
        }

        private long LockInstance(DistributedLockConnection cache, string key, string field, TimeSpan expiryTime)
        {
            var redisKey = GetRedisKey(cache.RedisKeyFormat, key);
            var host = GetHost(cache.ConnectionMultiplexer);

            long pttl;
            try
            {
                logger.LogTrace($"LockInstance enter {host}: {redisKey}, {field}, {expiryTime}");
                pttl = (long)cache.ConnectionMultiplexer
                    .GetDatabase(cache.RedisDatabase)
                    .ScriptEvaluate(TryLockScript, new RedisKey[] { redisKey }, new RedisValue[] { field, (long)expiryTime.TotalMilliseconds });
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error locking lock instance {host}: {ex.Message}");
                return -2;
            }

            logger.LogTrace($"LockInstance exit {host}: {redisKey}, {field}, {pttl}");

            return pttl;
        }

        private bool UnLockInstance(DistributedLockConnection cache, string key, string field)
        {
            var redisKey = GetRedisKey(cache.RedisKeyFormat, key);
            var host = GetHost(cache.ConnectionMultiplexer);

            long unlockStatus;
            try
            {
                logger.LogTrace($"Instance Unlock {host}: {redisKey}, {field}");
                unlockStatus = (long)cache.ConnectionMultiplexer
                    .GetDatabase(cache.RedisDatabase)
                    .ScriptEvaluate(UnlockScript, new RedisKey[] { redisKey }, new RedisValue[] { field });
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error Unlock instance {host}: {ex.Message}");
                return false;
            }

            logger.LogTrace($"Instance Unlock exit {host}: {redisKey}, {field}");

            return unlockStatus > 0;
        }

        private long RenewLockInstance(DistributedLockConnection cache, string key, string field, TimeSpan expiryTime)
        {
            var redisKey = GetRedisKey(cache.RedisKeyFormat, key);
            var host = GetHost(cache.ConnectionMultiplexer);

            long pttl;
            try
            {
                logger.LogTrace($"LockInstance renew {host}: {redisKey}, {field}, {expiryTime}");
                pttl = (long)cache.ConnectionMultiplexer
                    .GetDatabase(cache.RedisDatabase)
                    .ScriptEvaluate(RenewLockScript, new RedisKey[] { redisKey }, new RedisValue[] { field, (long)expiryTime.TotalMilliseconds });
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error renew lock instance {host}: {ex.Message}");
                return -2;
            }

            logger.LogTrace($"LockInstance renew exit {host}: {redisKey}, {field}, {pttl}");

            return pttl;
        }

        private static string GetRedisKey(string redisKeyFormat, string resource)
        {
            return string.Format(redisKeyFormat, resource);
        }

        internal static string GetHost(IConnectionMultiplexer cache)
        {
            var result = new StringBuilder();

            foreach (var endPoint in cache.GetEndPoints())
            {
                var server = cache.GetServer(endPoint);

                result.Append(server.EndPoint.GetFriendlyName());
                result.Append(" (");
                result.Append(server.IsConnected ? "connected" : "disconnected");
                result.Append("), ");
            }

            if (result.Length >= 2)
            {
                result.Remove(result.Length - 2, 2);
            }

            return result.ToString();
        }


        public void Dispose()
        {
            this.configuration.Provider.DisposeConnection();
        }
    }
}