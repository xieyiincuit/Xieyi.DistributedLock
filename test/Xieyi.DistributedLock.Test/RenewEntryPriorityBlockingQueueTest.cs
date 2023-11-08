using System.Diagnostics;
using System.Reflection;
using Xieyi.DistributedLock.Interfaces;
using Xieyi.DistributedLock.Renew;

namespace Xieyi.DistributedLock.Test;

public class RenewEntryPriorityBlockingQueueTest
{
    private const string _lockKey = "distributedLockTest";
    private static readonly long _defaultPTtl = 30000;
    private static readonly long _defaultWaitTime = _defaultPTtl / 3;

    private readonly RenewEntryPriorityBlockingQueue<RenewEntry> _priorityQueue = new();
    private readonly IDistributedLockFactory _lockFactory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);

    [Fact]
    public void Offer()
    {
        var distributedLock = new DistributedLock(_lockKey, _lockFactory);

        var renewEntry1 = new RenewEntry(distributedLock, TimeSpan.Zero, _defaultPTtl * 2);
        var renewEntry2 = new RenewEntry(distributedLock, TimeSpan.Zero, _defaultPTtl);

        _priorityQueue.Offer(renewEntry1);
        _priorityQueue.Offer(renewEntry2);

        var capacity = GetCapacity(_priorityQueue);
        Assert.Equal(10, capacity);

        var size = GetSize(_priorityQueue);
        Assert.Equal(2, size);

        var heap = GetHeap(_priorityQueue);
        Assert.Equal(11, heap.Length);

        Assert.True(ReferenceEquals(renewEntry2, heap[1]));
        Assert.True(ReferenceEquals(renewEntry1, heap[2]));

        _priorityQueue.Poll();
        _priorityQueue.Poll();
    }

    [Fact]
    public void Poll()
    {
        var num = 8;
        var random = new Random();
        for (int i = 0; i < num; i++)
        {
            var distributedLock = new DistributedLock(_lockKey, _lockFactory);
            var renewEntry = new RenewEntry(distributedLock, TimeSpan.Zero, random.Next(1000, 30000));
            _priorityQueue.Offer(renewEntry);
        }

        var poll = _priorityQueue.Poll();
        for (int i = num - 1; i > 0; i--)
        {
            var size = GetSize(_priorityQueue);
            Assert.Equal(i, size);
            var next = _priorityQueue.Poll();
            Assert.True(NotMoreThan(poll, next));
            poll = next;
        }

        var capacity = GetCapacity(_priorityQueue);
        Assert.Equal(10, capacity);

        var heap = GetHeap(_priorityQueue);
        Assert.Equal(11, heap.Length);
    }

    [Fact]
    public void Poll_Blocking()
    {
        var distributedLock = new DistributedLock(_lockKey, _lockFactory);
        var renewEntry = new RenewEntry(distributedLock, TimeSpan.Zero, _defaultPTtl);
        _priorityQueue.Offer(renewEntry);

        var stopwatch = Stopwatch.StartNew();
        _priorityQueue.Poll();
        var time = stopwatch.ElapsedMilliseconds;
        Assert.True(time >= _defaultWaitTime);
    }

    [Fact]
    public void GrowIfNecessary()
    {
        var num = 21;
        var random = new Random();
        for (int i = 0; i < num; i++)
        {
            var distributedLock = new DistributedLock(_lockKey, _lockFactory);
            var renewEntry = new RenewEntry(distributedLock, TimeSpan.Zero, random.Next(1000, 30000));
            _priorityQueue.Offer(renewEntry);
        }

        var capacity = GetCapacity(_priorityQueue);
        Assert.Equal(40, capacity);

        var size = GetSize(_priorityQueue);
        Assert.Equal(21, size);

        var heap = GetHeap(_priorityQueue);
        Assert.Equal(41, heap.Length);

        var poll = _priorityQueue.Poll();
        for (int i = num - 1; i > 0; i--)
        {
            size = GetSize(_priorityQueue);
            Assert.Equal(i, size);
            var next = _priorityQueue.Poll();
            Assert.True(NotMoreThan(poll, next));
            poll = next;
        }
    }

    private static int GetCapacity(object instance)
    {
        var type = instance.GetType();
        var fieldInfo = type.GetField("_capacity", BindingFlags.NonPublic | BindingFlags.Instance);
        return (int)fieldInfo?.GetValue(instance)!;
    }

    private static int GetSize(object instance)
    {
        var type = instance.GetType();
        var fieldInfo = type.GetField("_size", BindingFlags.NonPublic | BindingFlags.Instance);
        return (int)fieldInfo?.GetValue(instance)!;
    }

    private static RenewEntry[] GetHeap(object instance)
    {
        var type = instance.GetType();
        var fieldInfo = type.GetField("_heap", BindingFlags.NonPublic | BindingFlags.Instance);
        return (RenewEntry[])fieldInfo?.GetValue(instance);
    }

    private static bool NotMoreThan(RenewEntry v, RenewEntry w)
    {
        return v.CompareTo(w) <= 0;
    }
}