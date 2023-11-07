local function TryLock(key, field, leaseTime)
    if (redis.call('EXISTS', key) == 0) or (redis.call('HEXISTS', key, field) == 1) then
        redis.call('HINCRBY', key, field, 1)
        redis.call('PEXPIRE', key, leaseTime)
        return redis.call('PTTL', key)
    end
    return -2
end

return TryLock(KEYS[1], ARGV[1], ARGV[2])