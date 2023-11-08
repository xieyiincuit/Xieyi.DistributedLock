using StackExchange.Redis;
using Xieyi.DistributedLock.Interfaces;
using Xieyi.DistributedLock.Renew;

namespace Xieyi.DistributedLock.Test;

public class RenewManagementTest : IDisposable
{
    private const string _lockKey = "distributedLockTest";
    
    private readonly RenewManager _manager = RenewManager.Instance;
    private readonly RenewEntryPriorityBlockingQueue<RenewEntry> _priorityQueue;

    private static readonly int _numbers = 100;
    private static readonly int _totalRenewTime = 5 * 60 * 1000;

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _redis;
    private readonly IDistributedLockFactory _lockFactory;

    public RenewManagementTest()
    {
        _priorityQueue = (RenewEntryPriorityBlockingQueue<RenewEntry>)TestHelper.GetFieldValue(_manager, "_priorityQueue");
        
        _connectionMultiplexer = TestHelper.GetRedisClient();
        _redis = _connectionMultiplexer.GetDatabase();
        _lockFactory = TestHelper.GetRedisConnectionFactory(TestHelper.RemoveRedisKeyFormatServer);
    }

    [Fact]
    public void Renew()
    {
        var random = new Random();
        for (int i = 0; i < _numbers; i++)
        {
            var distributedLock = new DistributedLock(_lockKey, _lockFactory);
            distributedLock.Lock(TimeSpan.FromMilliseconds(random.Next(10000, 20000)));
            Thread.Sleep(100);
        }

        Thread.Sleep(_totalRenewTime);

        for (int i = 0; i < _numbers; i++)
        {
            var poll = _priorityQueue.Poll();

            var lockBase = (LockBase)TestHelper.GetFieldValue(poll, "_lockBase");
            Assert.False(lockBase.RenewFailed());

            var clientId = $"{lockBase.Id}:{Thread.CurrentThread.ManagedThreadId}";
            Assert.True(_redis.HashExists(lockBase.LockName, TestHelper.StringToBytes(clientId)));

            Thread.Sleep(100);
        }
    }


    public void Dispose()
    {
        _connectionMultiplexer?.Dispose();
        _lockFactory?.Dispose();
    }
}