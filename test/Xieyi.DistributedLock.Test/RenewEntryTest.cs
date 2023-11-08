using System.Collections.Concurrent;
using System.Reflection;
using Xieyi.DistributedLock.Exceptions;
using Xieyi.DistributedLock.Interfaces;
using Xieyi.DistributedLock.Renew;

namespace Xieyi.DistributedLock.Test;

public class RenewEntryTest : IDisposable
{
    private const string _lockKey = "distributedLockTest";

    private readonly IDistributedLockFactory _lockFactory;
    private readonly DistributedLock _distributedLock;
    private readonly ConcurrentDictionary<int, RenewEntry> _threadIds;

    private static readonly long _leaseTime = 30000;

    public RenewEntryTest()
    {
        _lockFactory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
        _distributedLock = new DistributedLock(_lockKey, _lockFactory);
        _threadIds = GetThreadIds(_distributedLock);
    }

    [Fact]
    public void TimeToRenew()
    {
        var renewEntry = new RenewEntry(_distributedLock, TimeSpan.FromMilliseconds(_leaseTime), _leaseTime);
        Assert.InRange(renewEntry.TimeToRenew(), 10000 - 100, 10000);

        renewEntry.UpdateRenewTime(60000);
        Assert.InRange(renewEntry.TimeToRenew(), 20000 - 100, 20000);

        renewEntry.UpdateRenewTime(0);
        Assert.InRange(renewEntry.TimeToRenew(), 10000 - 100, 10000);
    }

    [Fact]
    public void RefCount()
    {
        Assert.False(_threadIds.ContainsKey(Thread.CurrentThread.ManagedThreadId));

        _distributedLock.Lock();
        _threadIds.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var renewEntry);

        Assert.NotNull(renewEntry);
        Assert.Equal(1, renewEntry.RefCount);

        _distributedLock.Lock();
        Assert.Equal(2, renewEntry.RefCount);

        _distributedLock.Unlock();
        Assert.Equal(1, renewEntry.RefCount);

        _distributedLock.Unlock();
        Assert.Equal(0, renewEntry.RefCount);

        Assert.False(_threadIds.ContainsKey(Thread.CurrentThread.ManagedThreadId));
    }

    [Fact]
    public void RenewFailed()
    {
        _distributedLock.Lock();
        Assert.False(_distributedLock.RenewFailed());

        _threadIds.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var renewEntry);
        Assert.NotNull(renewEntry);

        renewEntry.NotifyRenewFailed();
        Assert.True(_distributedLock.RenewFailed());
        _distributedLock.Unlock();
    }

    [Fact]
    public void AlreadyClosedException()
    {
        _distributedLock.Lock();
        _threadIds.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var renewEntry);
        _distributedLock.Unlock();
        Assert.NotNull(renewEntry);

        Assert.Throws<LockHasBeenClosedException>(() =>
        {
            renewEntry.IncRef();
        });
    }
    
    public void Dispose()
    {
        _distributedLock.Dispose();
        _lockFactory.Dispose();
    }

    private ConcurrentDictionary<int, RenewEntry> GetThreadIds(DistributedLock distributedLock)
    {
        var type = distributedLock.GetType().BaseType;
        var fieldInfo = type?.GetField("_threadIds", BindingFlags.NonPublic | BindingFlags.Instance);
        return (ConcurrentDictionary<int, RenewEntry>)fieldInfo?.GetValue(distributedLock);
    }
}