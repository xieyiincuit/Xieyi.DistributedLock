using System.Collections.Concurrent;
using System.Diagnostics;
using Xieyi.DistributedLock.Exceptions;
using Xieyi.DistributedLock.LockLimit;

namespace Xieyi.DistributedLock.Test;

public class LimitLockManagerTest
{
    private readonly LocalLockManager _lockEntryManager = LocalLockManager.Instance;
    private readonly ConcurrentDictionary<string, LockEntry> _lockEntries;

    private static readonly string _localLockName = Guid.NewGuid().ToString();

    public LimitLockManagerTest()
    {
        _lockEntries = (ConcurrentDictionary<string, LockEntry>)TestHelper.GetFieldValue(_lockEntryManager, "_lockEntries");
    }
    
    [Fact]
    public void Lock()
    {
        _lockEntryManager.Lock(_localLockName);
        _lockEntries.TryGetValue(_localLockName, out var lockEntry);
        
        Assert.NotNull(lockEntry);
        Assert.Equal(lockEntry.Name, _localLockName);
        Assert.Equal(1, lockEntry.RefCount);
        Assert.True(lockEntry.IsEntered());

        _lockEntryManager.Unlock(_localLockName);
        
        Assert.Equal(0, lockEntry.RefCount);
    }
    
    [Fact]
    public void Lock_OneThread_Reentrant()
    {
        _lockEntryManager.Lock(_localLockName);
        _lockEntryManager.Lock(_localLockName);
        _lockEntryManager.Lock(_localLockName);

        _lockEntries.TryGetValue(_localLockName, out var lockEntry);
        Assert.NotNull(lockEntry);
        Assert.Equal(3, lockEntry.RefCount);
        Assert.True(lockEntry.IsEntered());

        _lockEntryManager.Unlock(_localLockName);
        Assert.Equal(2, lockEntry.RefCount);

        _lockEntryManager.Unlock(_localLockName);
        Assert.Equal(1, lockEntry.RefCount);

        _lockEntryManager.Unlock(_localLockName);
        
        Assert.Equal(0, lockEntry.RefCount);
    }
    
    [Fact]
    public void Lock_MultiThread_Blocking()
    {
        const int occupiedTime = 5000;
        EntryOccupiedInMilliTime(occupiedTime, _localLockName);
        
        //make sure the thread hold the lock
        Thread.Sleep(200);

        var stopwatch = Stopwatch.StartNew();
        _lockEntryManager.Lock(_localLockName);
        
        var time = stopwatch.ElapsedMilliseconds;
        Assert.True(time >= occupiedTime - 500);

        _lockEntryManager.Unlock(_localLockName);
    }
    
    [Fact]
    public void TryLock_MustHaveWaitTime()
    {
        Assert.False(_lockEntryManager.TryLock(_localLockName, 0));
    }
    
    [Fact]
    public void TryLock_Timeout()
    {
        var occupiedTime = 10000;
        EntryOccupiedInMilliTime(occupiedTime, _localLockName);
        Thread.Sleep(100);

        Assert.False(_lockEntryManager.TryLock(_localLockName, 5000));
    }
    
    [Fact]
    public void ErrorUnlock_ThrowLockInInvalidStateEx()
    {
        _lockEntryManager.Lock(_localLockName);

        Assert.Throws<LockInInvalidStateException>(() =>
        {
            _lockEntryManager.Unlock(_localLockName);
            _lockEntryManager.Unlock(_localLockName);
        });
    }
    
    [Fact]
    public void Unlock()
    {
        _lockEntryManager.Lock(_localLockName);
        _lockEntries.TryGetValue(_localLockName, out var lockEntry);
        _lockEntryManager.Unlock(_localLockName);

        Assert.NotNull(lockEntry);
        Assert.Equal(0, lockEntry.RefCount);
        Assert.False(lockEntry.IsEntered());
        Assert.False(_lockEntries.TryGetValue(_localLockName, out _));
    }
    
    private void EntryOccupiedInMilliTime(int time, string localLockName)
    {
        new Thread(() =>
        {
            _lockEntryManager.Lock(localLockName);
            Thread.Sleep(time);
            _lockEntryManager.Unlock(localLockName);
        }).Start();
    }
}