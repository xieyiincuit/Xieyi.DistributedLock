using System.Reflection;
using System.Text;
using StackExchange.Redis;

namespace Xieyi.DistributedLock.Test;

internal class TestHelper
{
    // make sure redis is running on these
    public static readonly EndPoint ActiveServer = new DnsEndPoint("localhost", 6379);

    // make sure redis is running here with the specified password
    public static readonly DistributedLockEndPoint PasswordedServer = new DistributedLockEndPoint
    {
        EndPoint = ActiveServer,
        Password = "password"
    };

    public static readonly DistributedLockEndPoint NonDefaultDatabaseServer = new DistributedLockEndPoint
    {
        EndPoint = ActiveServer,
        Password = "password",
        RedisDatabase = 1
    };

    public static readonly DistributedLockEndPoint UseCustomRedisKeyFormatServer = new DistributedLockEndPoint
    {
        EndPoint = ActiveServer,
        Password = "password",
        RedisKeyFormat = "{0}-xieyiLock"
    };
    
    public static readonly DistributedLockEndPoint RemoveRedisKeyFormatServer = new DistributedLockEndPoint
    {
        EndPoint = ActiveServer,
        Password = "password",
        RedisKeyFormat = "{0}"
    };

    public static DistributedLockFactory GetRedisConnectionFactory(DistributedLockEndPoint lockEndPoint)
    {
        return DistributedLockFactory.Create(lockEndPoint);
    }

    public static IConnectionMultiplexer GetRedisClient()
    {
        var lockEndPoint = PasswordedServer;

        var redisConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            Ssl = lockEndPoint.Ssl,
            SslProtocols = lockEndPoint.SslProtocols,
            Password = lockEndPoint.Password,
        };

        redisConfig.EndPoints.Add(lockEndPoint.EndPoint);

        var connection = ConnectionMultiplexer.Connect(redisConfig);
        return connection;
    }

    public static byte[] StringToBytes(string value)
    {
        if (value == null)
            return null;

        return Encoding.UTF8.GetBytes(value);
    }

    public static bool ArrayEquals(byte[] array1, byte[] array2)
    {
        if (ReferenceEquals(array1, array2))
            return true;

        if (array1 == null || array2 == null)
            return false;

        if (array1.Length != array2.Length)
            return false;

        for (int i = 0; i < array1.Length; ++i)
        {
            if (array1[i] != array2[i])
                return false;
        }

        return true;
    }

    public static object GetPropertyValue(object instance, string name)
    {
        var type = instance.GetType();
        var propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return propertyInfo?.GetValue(instance);
    }

    public static object GetFieldValue(object instance, string name)
    {
        var type = instance.GetType();
        var fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return fieldInfo?.GetValue(instance);
    }
}