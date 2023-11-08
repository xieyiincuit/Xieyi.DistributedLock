using StackExchange.Redis;
using Xieyi.DistributedLock.Interfaces;

namespace Xieyi.DistributedLock.Test;

public class DistributedLockProviderTest : IDisposable
{
    private const string _lockKey = "distributedLockTest";

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _redis;
    private readonly IDistributedLockFactory _lockFactory;

    private static readonly long _leaseTime = 30000L;
    private static readonly long _expectedPttl = _leaseTime - 100;

    public DistributedLockProviderTest()
    {
        _connectionMultiplexer = TestHelper.GetRedisClient();
        _redis = _connectionMultiplexer.GetDatabase();
        _lockFactory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
    }

    [Fact]
    public void Lock()
    {
        using (var distributedLock = DistributedLockProvider.Lock(_lockFactory, _lockKey))
        {
            var pttl = _redis.KeyTimeToLive(_lockKey).GetValueOrDefault();
            Assert.True(pttl.TotalMilliseconds > _expectedPttl);
            Thread.Sleep(1000);
        }

        Assert.False(_redis.KeyExists(_lockKey));
    }

    [Fact]
    public void TryLock()
    {
        if (DistributedLockProvider.TryLock(_lockFactory, _lockKey, out var Lock))
        {
            using (Lock)
            {
                var pttl = _redis.KeyTimeToLive(_lockKey).GetValueOrDefault();
                Assert.True(pttl.TotalMilliseconds > _expectedPttl);
                Thread.Sleep(1000);
            }

            Assert.False(_redis.KeyExists(_lockKey));
        }
        else
        {
            Assert.False(_redis.KeyExists(_lockKey));
        }
    }


    public void Dispose()
    {
        _redis.KeyDelete(_lockKey);
        _connectionMultiplexer.Dispose();
    }
}