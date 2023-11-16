using StackExchange.Redis;

namespace Xieyi.DistributedLock.Connection
{
    internal class DistributedLockConnection
    {
        public IConnectionMultiplexer ConnectionMultiplexer { get; set; }
        public int RedisDatabase { get; set; }
        public string RedisKeyFormat { get; set; }
    }
}