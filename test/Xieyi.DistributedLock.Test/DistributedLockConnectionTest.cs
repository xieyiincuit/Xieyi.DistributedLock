namespace Xieyi.DistributedLock.Test;

public class DistributedLockConnectionTest
{
    
    [Fact]
    public void TestSingleLock()
    {
        using (var factory = TestHelper.GetRedisConnectionFactory(TestHelper.PasswordedServer))
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
        using (var factory = TestHelper.GetRedisConnectionFactory(TestHelper.NonDefaultDatabaseServer))
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
        using (var factory = TestHelper.GetRedisConnectionFactory(TestHelper.UseCustomRedisKeyFormatServer))
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