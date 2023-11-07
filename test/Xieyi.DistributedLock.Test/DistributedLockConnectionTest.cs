namespace Xieyi.DistributedLock.Test;

public class DistributedLockConnectionTest
{
    // make sure redis is running on these
    private static readonly EndPoint ActiveServer = new DnsEndPoint("localhost", 6379);

    // make sure redis is running here with the specified password
    private static readonly DistributedLockEndPoint PasswordedServer = new DistributedLockEndPoint
    {
        EndPoint = ActiveServer,
        Password = "password"
    };

    private static readonly DistributedLockEndPoint NonDefaultDatabaseServer = new DistributedLockEndPoint
    {
        EndPoint = ActiveServer,
        Password = "password",
        RedisDatabase = 1
    };

    private static readonly DistributedLockEndPoint UseCustomRedisKeyFormatServer = new DistributedLockEndPoint
    {
        EndPoint = ActiveServer,
        Password = "password",
        RedisKeyFormat = "{0}-xieyiLock"
    };

    [Fact]
    public void TestSingleLock()
    {
        using (var factory = DistributedLockFactory.Create(PasswordedServer))
        {
            const string lockKey = "singleLock";
            var field = Guid.NewGuid().ToString();
            var expire = TimeSpan.FromSeconds(15);

            var pttl = factory.CreateLock(lockKey, field, expire);
            Assert.Equal(pttl, expire.TotalMilliseconds);

            var res = factory.Unlock(lockKey, field);
            Assert.True(res);
        }
    }

    [Fact]
    public void TestSingleLock_UseDataBase()
    {
        using (var factory = DistributedLockFactory.Create(NonDefaultDatabaseServer))
        {
            const string lockKey = "singleLock";
            var field = Guid.NewGuid().ToString();
            var expire = TimeSpan.FromSeconds(15);

            var pttl = factory.CreateLock(lockKey, field, expire);
            Assert.Equal(pttl, expire.TotalMilliseconds);

            var res = factory.Unlock(lockKey, field);
            Assert.True(res);
        }
    }

    [Fact]
    public void TestSingleLock_UseRedisKeyFormat()
    {
        using (var factory = DistributedLockFactory.Create(UseCustomRedisKeyFormatServer))
        {
            const string lockKey = "singleLock";
            var field = Guid.NewGuid().ToString();
            var expire = TimeSpan.FromSeconds(15);

            var pttl = factory.CreateLock(lockKey, field, expire);
            Assert.Equal(pttl, expire.TotalMilliseconds);

            var res = factory.Unlock(lockKey, field);
            Assert.True(res);
        }
    }
}