using StackExchange.Redis;

namespace Xieyi.DistributedLock.Connection
{
    public class DistributedLockMultiplexer
    {
        public IConnectionMultiplexer ConnectionMultiplexer { get; }

        public DistributedLockMultiplexer(IConnectionMultiplexer connectionMultiplexer)
        {
            this.ConnectionMultiplexer = connectionMultiplexer;
        }

        public static implicit operator DistributedLockMultiplexer(ConnectionMultiplexer connectionMultiplexer)
        {
            return new DistributedLockMultiplexer(connectionMultiplexer);
        }

        public int? RedisDatabase { get; set; }
        public string RedisKeyFormat { get; set; }
    }
}