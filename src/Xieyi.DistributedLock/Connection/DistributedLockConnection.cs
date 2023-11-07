using StackExchange.Redis;

namespace Xieyi.DistributedLock.Connection
{
    internal class DistributedLockConnection
    {
        public IConnectionMultiplexer ConnectionMultiplexer { get; init; }
        public int RedisDatabase { get; init; }
        public string RedisKeyFormat { get; init; }
    }
}