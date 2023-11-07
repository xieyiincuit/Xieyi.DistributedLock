local function RenewLock(key, field, leaseTime)
    if (redis.call('HEXISTS', key, field) == 1) then
        redis.call('PEXPIRE', key, leaseTime)
        return redis.call('PTTL', key)
    end

    return -2
end

return RenewLock(KEYS[1], ARGV[1], ARGV[2])
