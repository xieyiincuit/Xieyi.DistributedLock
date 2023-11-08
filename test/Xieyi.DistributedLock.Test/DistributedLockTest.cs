using System.Diagnostics;
using StackExchange.Redis;
using Xieyi.DistributedLock.Exceptions;

namespace Xieyi.DistributedLock.Test;

public class DistributedLockTest : IDisposable
{
    private const string _lockKey = "distributedLockTest";

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _redis;

    private static readonly long _leaseTime = 10000L;
    private static readonly long _expectedPttl = _leaseTime - 100;

    public DistributedLockTest()
    {
        _connectionMultiplexer = TestHelper.GetRedisClient();
        _redis = _connectionMultiplexer.GetDatabase();
    }

    [Fact]
    public void Lock()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);

        distributedLock.Lock();

        var pttl = _redis.KeyTimeToLive(_lockKey).GetValueOrDefault();
        Assert.True(pttl.TotalMilliseconds > TimeSpan.FromSeconds(30).TotalMilliseconds - 100);

        var clientId = $"{distributedLock.Id}:{Thread.CurrentThread.ManagedThreadId}";
        var redisValue = _redis.HashGet(_lockKey, clientId);
        redisValue.TryParse(out int refCount);

        Assert.Equal(1, refCount);
        distributedLock.Unlock();
    }

    [Fact]
    public void Lock_Blocking()
    {
        LockOccupiedInMilliTime(10 * 1000);
        Thread.Sleep(100);

        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);

        var stopWatch = Stopwatch.StartNew();
        distributedLock.Lock();

        var exceptWaitTime = 9900;
        Assert.True(stopWatch.ElapsedMilliseconds > exceptWaitTime);

        distributedLock.Unlock();
    }

    [Fact]
    public void Lock_WithLeaseTime()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);

        distributedLock.Lock(TimeSpan.FromMilliseconds(_leaseTime));

        var pttl = _redis.KeyTimeToLive(_lockKey).GetValueOrDefault();
        Assert.True(pttl.TotalMilliseconds > _expectedPttl - 100);

        var clientId = $"{distributedLock.Id}:{Thread.CurrentThread.ManagedThreadId}";
        var redisValue = _redis.HashGet(_lockKey, clientId);
        redisValue.TryParse(out int refCount);

        Assert.Equal(1, refCount);
        distributedLock.Unlock();
    }

    [Fact]
    public void Lock_Reentrant()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);
        var clientId = $"{distributedLock.Id}:{Thread.CurrentThread.ManagedThreadId}";

        distributedLock.Lock();
        distributedLock.Lock();
        distributedLock.Lock();

        var redisValue = _redis.HashGet(_lockKey, clientId);
        redisValue.TryParse(out int refCount);

        Assert.Equal(3, refCount);

        distributedLock.Unlock();
        distributedLock.Unlock();

        _redis.HashGet(_lockKey, clientId).TryParse(out refCount);
        Assert.Equal(1, refCount);

        distributedLock.Unlock();

        _redis.HashGet(_lockKey, clientId).TryParse(out refCount);
        Assert.Equal(0, refCount);
    }

    [Fact]
    public void TryLock()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);

        Assert.True(distributedLock.TryLock());
        var pttl = _redis.KeyTimeToLive(_lockKey).GetValueOrDefault();
        Assert.True(pttl.TotalMilliseconds > TimeSpan.FromSeconds(30).TotalMilliseconds - 100);

        var clientId = $"{distributedLock.Id}:{Thread.CurrentThread.ManagedThreadId}";
        var redisValue = _redis.HashGet(_lockKey, clientId);
        redisValue.TryParse(out int refCount);

        Assert.Equal(1, refCount);
        distributedLock.Unlock();
    }

    [Fact]
    public void TryLock_LeaseTime()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);

        Assert.True(distributedLock.TryLock(TimeSpan.FromSeconds(10)));
        var pttl = _redis.KeyTimeToLive(_lockKey).GetValueOrDefault();
        Assert.True(pttl.TotalMilliseconds > TimeSpan.FromSeconds(10).TotalMilliseconds - 100);

        var clientId = $"{distributedLock.Id}:{Thread.CurrentThread.ManagedThreadId}";
        var redisValue = _redis.HashGet(_lockKey, clientId);
        redisValue.TryParse(out int refCount);

        Assert.Equal(1, refCount);
        distributedLock.Unlock();
    }

    [Fact]
    public void TryLock_WaitTime_Succeed()
    {
        LockOccupiedInMilliTime(10 * 1000);
        Thread.Sleep(100);

        var waitTime = TimeSpan.FromMilliseconds(15 * 1000);
        var leaseTime = TimeSpan.FromMilliseconds(10 * 1000);

        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);
        Assert.True(distributedLock.TryLock(waitTime, leaseTime));

        distributedLock.Unlock();
    }

    [Fact]
    public void TryLock_WaitTime_Failed()
    {
        LockOccupiedInMilliTime(10 * 1000);
        Thread.Sleep(100);

        var waitTime = TimeSpan.FromMilliseconds(8 * 1000);
        var leaseTime = TimeSpan.FromMilliseconds(10 * 1000);

        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);
        Assert.False(distributedLock.TryLock(waitTime, leaseTime));

        distributedLock.Unlock();
    }

    [Fact]
    public void RenewFailed()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);
        distributedLock.Lock(TimeSpan.FromSeconds(10));
        Assert.False(distributedLock.RenewFailed());

        _redis.KeyDelete(_lockKey);

        //wait renew thread to handle this
        Thread.Sleep(5000);

        Assert.True(distributedLock.RenewFailed());
        distributedLock.Unlock();
    }

    [Fact]
    public void RenewFailed_Exception_BeforeLock()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);

        Assert.Throws<LockInInvalidStateException>(() => { distributedLock.RenewFailed(); });

        distributedLock.Lock();
        distributedLock.Unlock();
    }

    [Fact]
    public void RenewFailed_Exception_AfterUnLock()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);
        distributedLock.Lock();

        distributedLock.Unlock();
        Assert.Throws<LockInInvalidStateException>(() => { distributedLock.RenewFailed(); });
    }
    
    [Fact]
    public void GetRenewToken_Exception_BeforeLock()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);
        distributedLock.Lock();

        distributedLock.Unlock();
        Assert.Throws<LockInInvalidStateException>(() => { distributedLock.GetRenewFailedToken(); });
    }

    [Fact]
    public void GetRenewToken_Exception_AfterUnLock()
    {
        var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        var distributedLock = new DistributedLock(_lockKey, factory);
        distributedLock.Lock();

        distributedLock.Unlock();
        Assert.Throws<LockInInvalidStateException>(() => { distributedLock.GetRenewFailedToken(); });
    }

    private void LockOccupiedInMilliTime(int sleepTime)
    {
        new Thread(() =>
        {
            var factory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
            var distributedLock = new DistributedLock(_lockKey, factory);

            distributedLock.Lock();
            Thread.Sleep(sleepTime);

            distributedLock.Unlock();
        }).Start();
    }

    public void Dispose()
    {
        _redis.KeyDelete(_lockKey);
        _connectionMultiplexer.Dispose();
    }
}