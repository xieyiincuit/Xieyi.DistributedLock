namespace Xieyi.DistributedLock.Connection
{
    /// <summary>
    /// A connection provider that uses existing Redis ConnectionMultiplexers
    /// </summary>
    internal class ExistDistributedLockProvider : DistributedLockProvider
    {
        public DistributedLockMultiplexer ExistedMultiplexer { get; set; }

        public ExistDistributedLockProvider(DistributedLockMultiplexer existedMultiplexer)
        {
            ExistedMultiplexer = existedMultiplexer;
        }

        internal override DistributedLockConnection CreateRedisConnection()
        {
            if (this.ExistedMultiplexer == null)
            {
                throw new ArgumentException("No multiplexers specified");
            }
            
            var redisConnection = new DistributedLockConnection()
            {
                ConnectionMultiplexer = ExistedMultiplexer.ConnectionMultiplexer,
                RedisDatabase = ExistedMultiplexer.RedisDatabase ?? DefaultRedisDatabase,
                RedisKeyFormat = string.IsNullOrEmpty(ExistedMultiplexer.RedisKeyFormat)
                    ? DefaultRedisKeyFormat
                    : ExistedMultiplexer.RedisKeyFormat
            };
            
            return redisConnection;
        }

        internal override void DisposeConnection()
        {
            ExistedMultiplexer.ConnectionMultiplexer.Dispose();
        }
    }
}