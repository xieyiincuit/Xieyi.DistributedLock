# Xieyi.DistributedLock
简单有效的分布式锁设计，支持请求量优化、锁重入、锁续约、锁失效等特征

## 特征
- 基于 StackExchange.Redis
- 可重入锁设计
- 请求量优化设计
- 正确的加锁，销毁锁流程（使用Lua脚本），保证主动销毁锁流程只能被锁的拥有者执行
- 利用阻塞优先队列的分布式锁续约设计
  
### 锁的可重入设计
Redis服务器使用Hash对象保存锁信息。分布式锁的key由(keyPrefix, lockName)组成。
客户端创建的每个锁实例，都会生成一个GUID作为锁的唯一id。
field: {id}:{threadId}，表示当前锁的持有者为拥有锁ID为{id}的锁对象的{threadId}线程。
value: 记录锁的重入次数。

### 请求量优化设计
每一个应用进程为每一把锁创建请求入口 Entry，使用 ConcurrentDictionary<lock_key, Entry> 管理应用进程下的所有 Entry。Entry的维护过程是线程安全的。
当线程创建锁对象时，若 Entry字典没有该 lock_key 的 Entry，创建 Entry 并加入到字典中，线程尝试获取锁，向服务器发起第一次请求。
若第一次请求未获取到锁，则尝试获取 Entry 的信号量，阻塞线程，将该线程加入到等待队列，只有拿到信号量的线程才会再次向服务器请求锁，重试间隔2s，直到获取锁。
获取锁后，将线程从等待队列移除，执行业务代码，解锁时，判断等待队列线程数量，为0则从字典中删除Entry。
在单应用多线程争抢同一把锁的场景下，使用内部锁保证该应用在同一时刻只有一个线程重试争抢锁(Moniter.Enter())。
内部锁生命周期通过引用计数进行管理，当计数器降低为0时，申请锁的入口会安全关闭。

### 加锁、解锁、续约的LUA脚本设计
```
local function TryLock(key, field, leaseTime)
    if (redis.call('EXISTS', key) == 0) or (redis.call('HEXISTS', key, field) == 1) then
        redis.call('HINCRBY', key, field, 1)
        redis.call('PEXPIRE', key, leaseTime)
        return redis.call('PTTL', key)
    end
    return -2
end

return TryLock(KEYS[1], ARGV[1], ARGV[2])
```

```
local function Unlock(key, field)
    if (redis.call('HEXISTS', key, field) == 0) then
        return 0
    end

    local counter = redis.call('HINCRBY', key, field, -1)

    if (counter < 1) then
        redis.call('DEL', key)
    end

    return 1
end

return Unlock(KEYS[1], ARGV[1])
```

```
local function RenewLock(key, field, leaseTime)
    if (redis.call('HEXISTS', key, field) == 1) then
        redis.call('PEXPIRE', key, leaseTime)
        return redis.call('PTTL', key)
    end

    return -2
end

return RenewLock(KEYS[1], ARGV[1], ARGV[2])
```

### 分布式锁续约设计
利用后台线程完成续约。
分布式锁默认30s过期，每10秒续约一次。
成功获取分布式锁后，会生成一个保存续约信息的RenewEntry对象，对象会以下次续约时间的时间戳从小到大排序，放入阻塞优先队列中，准备进行续约。
续约线程会从阻塞优先队列获取锁对象尝试续约。
如果分布式锁已经解锁，则该锁停止续约。
如果分布式锁在续约过程中发生异常，RenewEntry对象会放回阻塞优先队列中，等待再次续约，下次续约的时间戳为：CurrentTimeStamp + _leaseTime / 3。
如果分布式锁续约成功，RenewEntry对象会放回阻塞优先队列中，下次续约的时间戳为：CurrentTimeStamp + pttl / 3。
如果分布式锁续约失败，且锁未Unlock，即在续约时锁不存在，会判定锁已经失效，停止续约，并调用CancellationTokenSource的Cancel方法，用户可以调用 RenewFailed 方法判断续约是否失效。
RenewFailed、RenewFailedToken方法只有在当前线程持有分布式锁时才能调用，否则会抛出InvalidStateException异常。

### 使用示例
在初始化应用时，设置Redis链接节点以及其他设置 (如 password, SSL, connection timeout, redis database, key format)等详细信息，具体详见 ```DistributedLockEndPoint``` 类:
```
var distributedLockFactory = DistributedLockFactory.Create(new DistributedLockEndPoint()
{
    EndPoint = new DnsEndPoint("localhost", 6379),
    Password = "password",
    RedisKeyFormat = "{0}",
}, new DistributedLockRetryConfiguration(retryCount: 3, retryDelay: TimeSpan.FromMilliseconds(100)));

builder.Services.AddSingleton<IDistributedLockFactory>(distributedLockFactory);
```
我们将 ```DistributedLockFactory``` 创建成功后，可将其注入到DI容器中，在创建分布式锁时，需要注入 ```DistributedLockFactory``` 服务。
接下来我们可以选择两种不同的方式来使用分布式锁：
使用 ```DistributedLockProvider```，在业务逻辑完成后，会自动解锁：
```
if (DistributedLockProvider.TryLock(_lockFactory, _lockKey, out var Lock))
{
    using (Lock)
    {
       //do u things
    }
}
else
{
  //do loggings
}
```
or
```
using (var distributedLock = DistributedLockProvider.Lock(_lockFactory, _lockKey))
{
    //do u things
}
```
使用 ```DistributedLock```：
```
var distributedLock = new DistributedLock(_lockKey, factory);
try
{
    distributedLock.Lock();
    //do u things
}
catch (Exception ex)
{
    //do loggings
}
finally
{
    distributedLock.Unlock();
}
```
or
```
var distributedLock = new DistributedLock(_lockKey, factory);
if (distributedLock.TryLock())
{
    try
    {
        distributedLock.Lock();
        //do u things
    }
    catch (Exception ex)
    {
        //do loggings
    }
    finally
    {
        distributedLock.Unlock();
    }
}
else
{
  //do loggings
}
```
